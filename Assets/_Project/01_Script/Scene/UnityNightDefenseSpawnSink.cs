using System;
using System.Collections.Generic;
using UnityEngine;

// 전투 계산 결과를 Unity 씬 오브젝트로 반영하는 표시 어댑터입니다.
// 전투 런타임은 데이터와 계산만 담당하고, 이 클래스는 몬스터 프리팹 생성과 위치 갱신만 담당합니다.
public sealed class UnityNightDefenseSpawnSink : MonoBehaviour, IBattleCombatViewSink
{
    [SerializeField] private Transform enemyLayer; // 전투 중 생성되는 몬스터 오브젝트를 모아둘 부모입니다.
    [SerializeField] private EnemyPrefabBinding[] enemyPrefabs = Array.Empty<EnemyPrefabBinding>(); // EnemyData.PrefabKey와 실제 몬스터 프리팹의 연결 목록입니다.
    [SerializeField] private BattleLanePath[] lanePaths = Array.Empty<BattleLanePath>(); // LaneId별 몬스터 시작 위치와 성채 도착 위치입니다.
    [SerializeField] private int preloadCountPerPrefab = 3; // 프리팹별로 미리 만들어둘 풀 오브젝트 수입니다.
    [SerializeField] private float despawnDelaySeconds = 0.25f; // 처치나 성채 도착 상태를 짧게 보여준 뒤 풀로 반납하기까지의 시간입니다.
    [SerializeField] private bool logRuntimeEvents = true; // 전투 표시 흐름을 확인하기 위한 로그 출력 여부입니다.

    private readonly Dictionary<string, GameObject> prefabByKey = new(); // PrefabKey로 실제 프리팹을 빠르게 찾기 위한 캐시입니다.
    private readonly Dictionary<int, BattleLanePath> laneById = new(); // LaneId로 이동 경로를 빠르게 찾기 위한 캐시입니다.
    private readonly Dictionary<string, Stack<PooledEnemyView>> pooledEnemiesByKey = new(); // PrefabKey별 재사용 대기 중인 몬스터 표시 오브젝트입니다.
    private readonly Dictionary<int, ActiveEnemyView> activeEnemiesByRuntimeId = new(); // RuntimeId별 현재 전장에 배치된 몬스터 표시 오브젝트입니다.
    private readonly List<PendingPoolReturn> pendingPoolReturns = new(); // 제거 연출 후 풀로 반납할 몬스터 표시 오브젝트입니다.

    // 인스펙터에 연결된 프리팹과 라인 정보를 전투 중 조회하기 쉬운 형태로 준비합니다.
    private void Awake()
    {
        BuildPrefabCache();
        BuildLaneCache();
        PreloadEnemyPools();
    }

    // 제거 상태를 잠깐 보여준 몬스터를 시간이 지난 뒤 풀로 반납합니다.
    private void Update()
    {
        ProcessPendingPoolReturns(Time.deltaTime);
    }

    // 새 전투가 시작될 때 기존 전투 표시 오브젝트를 풀로 되돌립니다.
    public void ClearBattleViews()
    {
        for (int i = pendingPoolReturns.Count - 1; i >= 0; i--)
            ReturnToPoolImmediately(pendingPoolReturns[i].ActiveEnemy);

        pendingPoolReturns.Clear();

        foreach (ActiveEnemyView activeEnemy in activeEnemiesByRuntimeId.Values)
            ReturnToPoolImmediately(activeEnemy);

        activeEnemiesByRuntimeId.Clear();

        if (logRuntimeEvents)
            Debug.Log("[BattleViewSink] 전투 표시 오브젝트를 초기화했습니다.");
    }

    // 전투 런타임에서 몬스터 생성이 확정되면 대응되는 Unity 프리팹을 생성합니다.
    public void SpawnEnemyView(CombatEnemyRuntimeState enemy, NightDefenseSpawnRequest request)
    {
        if (enemy == null)
            return;

        if (!TryCreateEnemyView(enemy, request, out ActiveEnemyView activeEnemy))
            return;

        activeEnemy.ChangeState(EnemyViewState.Spawned);
        activeEnemiesByRuntimeId[enemy.RuntimeId] = activeEnemy;
        SyncEnemyTransform(activeEnemy, enemy);
        activeEnemy.ChangeState(EnemyViewState.Moving);

        if (logRuntimeEvents)
            Debug.Log($"[BattleViewSink] Enemy Spawn Runtime:{enemy.RuntimeId} Enemy:{enemy.EnemyId} Wave:{request.WaveIndex} Lane:{request.LaneId}");
    }

    // 살아 있는 몬스터들의 전투 진행률을 실제 씬 위치로 반영합니다.
    public void SyncEnemyViews(IReadOnlyList<CombatEnemyRuntimeState> enemies)
    {
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            CombatEnemyRuntimeState enemy = enemies[i];

            if (enemy == null || !activeEnemiesByRuntimeId.TryGetValue(enemy.RuntimeId, out ActiveEnemyView activeEnemy))
                continue;

            SyncEnemyTransform(activeEnemy, enemy);

            if (activeEnemy.State != EnemyViewState.Moving)
                activeEnemy.ChangeState(EnemyViewState.Moving);
        }
    }

    // 데미지 결과를 표시하고, 피해를 받은 몬스터 표시 상태를 갱신합니다.
    public void ShowDamageResults(IReadOnlyList<CombatDamageResult> damageResults)
    {
        if (damageResults == null || damageResults.Count == 0)
            return;

        for (int i = 0; i < damageResults.Count; i++)
        {
            CombatDamageResult damageResult = damageResults[i];

            if (!activeEnemiesByRuntimeId.TryGetValue(damageResult.TargetRuntimeId, out ActiveEnemyView activeEnemy))
                continue;

            activeEnemy.ChangeState(damageResult.IsTargetDefeated ? EnemyViewState.Defeated : EnemyViewState.Hit);
        }

        if (logRuntimeEvents)
            Debug.Log($"[BattleViewSink] Damage Count:{damageResults.Count}");
    }

    // 사망하거나 성채에 도착한 몬스터 표시 오브젝트를 풀로 반납합니다.
    public void DespawnEnemyView(CombatEnemyRuntimeState enemy, CombatEnemyDespawnReason reason)
    {
        if (enemy == null)
            return;

        if (!activeEnemiesByRuntimeId.TryGetValue(enemy.RuntimeId, out ActiveEnemyView activeEnemy))
            return;

        if (reason == CombatEnemyDespawnReason.ReachedGoal)
            SyncEnemyTransform(activeEnemy, enemy);

        activeEnemiesByRuntimeId.Remove(enemy.RuntimeId);
        ScheduleReturnToPool(activeEnemy, reason);

        if (logRuntimeEvents)
            Debug.Log($"[BattleViewSink] Enemy Despawn Runtime:{enemy.RuntimeId} Reason:{reason}");
    }

    // EnemyData의 PrefabKey를 인스펙터에 연결한 Unity 프리팹으로 변환해 전투 필드에 생성합니다.
    private bool TryCreateEnemyView(CombatEnemyRuntimeState enemy, NightDefenseSpawnRequest request, out ActiveEnemyView activeEnemy)
    {
        activeEnemy = default;

        if (!TryGetEnemyPrefabKey(enemy.EnemyId, out string prefabKey))
            return false;

        if (!prefabByKey.TryGetValue(prefabKey, out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[BattleViewSink] PrefabKey '{prefabKey}'에 연결된 몬스터 프리팹이 없습니다. UnityNightDefenseSpawnSink 인스펙터의 enemyPrefabs를 확인해야 합니다.");
            return false;
        }

        if (!laneById.TryGetValue(request.LaneId, out BattleLanePath lanePath))
        {
            Debug.LogError($"[BattleViewSink] LaneId {request.LaneId}에 연결된 전투 경로가 없습니다. UnityNightDefenseSpawnSink 인스펙터의 lanePaths를 확인해야 합니다.");
            return false;
        }

        PooledEnemyView pooledEnemy = RentFromPool(prefabKey, prefab);
        GameObject instance = pooledEnemy.Instance;
        instance.transform.SetParent(enemyLayer, false);
        instance.name = $"{prefab.name}_Runtime_{enemy.RuntimeId}";
        pooledEnemy.ResetForRent();
        instance.SetActive(true);
        SetSortingOrder(instance, 100 + request.LaneId * 10 + request.SpawnOrder);
        activeEnemy = new ActiveEnemyView(prefabKey, pooledEnemy, lanePath);
        return true;
    }

    // GameContext의 콘텐츠 테이블에서 EnemyId에 해당하는 프리팹 키를 가져옵니다.
    private bool TryGetEnemyPrefabKey(int enemyId, out string prefabKey)
    {
        prefabKey = string.Empty;
        GameContext context = GameRoot.Instance != null ? GameRoot.Instance.Context : null;

        if (context?.ContentDataSource == null)
        {
            Debug.LogError("[BattleViewSink] GameContext 또는 ContentDataSource가 없어 EnemyData를 조회할 수 없습니다.");
            return false;
        }

        if (!context.ContentDataSource.TryGetEnemy(enemyId, out EnemyDataRow enemyRow) || enemyRow == null)
        {
            Debug.LogError($"[BattleViewSink] EnemyId {enemyId}에 해당하는 EnemyData를 찾지 못했습니다.");
            return false;
        }

        prefabKey = enemyRow.PrefabKey;
        return !string.IsNullOrWhiteSpace(prefabKey);
    }

    // 전투 진행률 0~1을 라인의 시작 위치와 도착 위치 사이의 실제 위치로 변환합니다.
    private static void SyncEnemyTransform(ActiveEnemyView activeEnemy, CombatEnemyRuntimeState enemy)
    {
        if (activeEnemy.Instance == null)
            return;

        Vector3 position = Vector3.Lerp(activeEnemy.LanePath.StartPosition, activeEnemy.LanePath.GoalPosition, enemy.PathProgress);
        activeEnemy.Instance.transform.position = position;

        Vector3 scale = activeEnemy.Instance.transform.localScale;
        float direction = activeEnemy.LanePath.GoalPosition.x >= activeEnemy.LanePath.StartPosition.x ? 1f : -1f;
        activeEnemy.Instance.transform.localScale = new Vector3(Mathf.Abs(scale.x) * direction, scale.y, scale.z);
    }

    // 생성된 몬스터 프리팹 안의 SpriteRenderer 정렬값을 라인과 생성 순서 기준으로 절대값 세팅합니다.
    private static void SetSortingOrder(GameObject instance, int baseOrder)
    {
        if (instance == null)
            return;

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = baseOrder + i;
    }

    // EnemyData.PrefabKey와 실제 Unity 프리팹 연결 목록을 캐시합니다.
    private void BuildPrefabCache()
    {
        prefabByKey.Clear();

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            EnemyPrefabBinding binding = enemyPrefabs[i];

            if (string.IsNullOrWhiteSpace(binding.PrefabKey) || binding.Prefab == null)
                continue;

            prefabByKey[binding.PrefabKey] = binding.Prefab;
        }
    }

    // 전투 중 Instantiate가 몰리지 않도록 프리팹별 몬스터 표시 오브젝트를 미리 만들어 둡니다.
    private void PreloadEnemyPools()
    {
        pooledEnemiesByKey.Clear();

        foreach (KeyValuePair<string, GameObject> pair in prefabByKey)
        {
            Stack<PooledEnemyView> pool = GetOrCreatePool(pair.Key);
            int preloadCount = Mathf.Max(0, preloadCountPerPrefab);

            for (int i = 0; i < preloadCount; i++)
                pool.Push(CreatePooledEnemy(pair.Key, pair.Value));
        }
    }

    // LaneId와 전투 경로 연결 목록을 캐시합니다.
    private void BuildLaneCache()
    {
        laneById.Clear();

        for (int i = 0; i < lanePaths.Length; i++)
        {
            BattleLanePath lanePath = lanePaths[i];

            if (lanePath.LaneId <= 0)
                continue;

            laneById[lanePath.LaneId] = lanePath;
        }
    }

    // 풀에서 몬스터 표시 오브젝트를 꺼내고, 비어 있으면 같은 규칙으로 추가 생성합니다.
    private PooledEnemyView RentFromPool(string prefabKey, GameObject prefab)
    {
        Stack<PooledEnemyView> pool = GetOrCreatePool(prefabKey);

        while (pool.Count > 0)
        {
            PooledEnemyView pooledEnemy = pool.Pop();

            if (pooledEnemy.Instance != null)
                return pooledEnemy;
        }

        return CreatePooledEnemy(prefabKey, prefab);
    }

    // 제거 사유에 맞는 상태를 보여준 뒤 일정 시간이 지나면 풀로 반납하도록 예약합니다.
    private void ScheduleReturnToPool(ActiveEnemyView activeEnemy, CombatEnemyDespawnReason reason)
    {
        if (activeEnemy.Instance == null)
            return;

        EnemyViewState state = reason switch
        {
            CombatEnemyDespawnReason.Defeated => EnemyViewState.Defeated,
            CombatEnemyDespawnReason.ReachedGoal => EnemyViewState.ReachedGoal,
            _ => EnemyViewState.Pooled
        };

        activeEnemy.ChangeState(state);
        pendingPoolReturns.Add(new PendingPoolReturn(activeEnemy, Mathf.Max(0f, despawnDelaySeconds)));
    }

    // 풀 반납 대기열을 갱신합니다.
    private void ProcessPendingPoolReturns(float deltaSeconds)
    {
        if (pendingPoolReturns.Count == 0)
            return;

        for (int i = pendingPoolReturns.Count - 1; i >= 0; i--)
        {
            PendingPoolReturn pendingReturn = pendingPoolReturns[i];
            pendingReturn.RemainingSeconds -= deltaSeconds;

            if (pendingReturn.RemainingSeconds > 0f)
                continue;

            pendingPoolReturns.RemoveAt(i);
            ReturnToPoolImmediately(pendingReturn.ActiveEnemy);
        }
    }

    // 몬스터 표시 오브젝트를 즉시 풀에 넣기 전에 상태와 Transform을 기본값으로 되돌립니다.
    private void ReturnToPoolImmediately(ActiveEnemyView activeEnemy)
    {
        if (activeEnemy.Instance == null)
            return;

        activeEnemy.ChangeState(EnemyViewState.Pooled);
        activeEnemy.Instance.SetActive(false);
        activeEnemy.Instance.transform.SetParent(enemyLayer, false);
        activeEnemy.Instance.transform.localPosition = Vector3.zero;
        activeEnemy.Instance.transform.localRotation = Quaternion.identity;

        Stack<PooledEnemyView> pool = GetOrCreatePool(activeEnemy.PrefabKey);
        pool.Push(activeEnemy.PooledEnemy);
    }

    // 실제 Unity 프리팹 인스턴스를 만들고 풀 대기 상태로 초기화합니다.
    private PooledEnemyView CreatePooledEnemy(string prefabKey, GameObject prefab)
    {
        GameObject instance = Instantiate(prefab, enemyLayer);
        instance.name = $"{prefab.name}_Pooled";
        instance.SetActive(false);
        return new PooledEnemyView(prefabKey, instance, instance.GetComponentInChildren<Animator>(true));
    }

    // PrefabKey별 풀 Stack을 가져오거나 새로 만듭니다.
    private Stack<PooledEnemyView> GetOrCreatePool(string prefabKey)
    {
        if (pooledEnemiesByKey.TryGetValue(prefabKey, out Stack<PooledEnemyView> pool))
            return pool;

        pool = new Stack<PooledEnemyView>();
        pooledEnemiesByKey[prefabKey] = pool;
        return pool;
    }

    [Serializable]
    private sealed class EnemyPrefabBinding
    {
        [SerializeField] private string prefabKey; // EnemyData.tsv의 PrefabKey입니다.
        [SerializeField] private GameObject prefab; // 실제 생성할 몬스터 프리팹입니다.

        public string PrefabKey => prefabKey;
        public GameObject Prefab => prefab;
    }

    [Serializable]
    private sealed class BattleLanePath
    {
        [SerializeField] private int laneId; // WaveData.tsv의 LaneId입니다.
        [SerializeField] private Vector3 startPosition; // 몬스터가 생성되는 월드 위치입니다.
        [SerializeField] private Vector3 goalPosition; // 성채에 도착했다고 보는 월드 위치입니다.

        public int LaneId => laneId;
        public Vector3 StartPosition => startPosition;
        public Vector3 GoalPosition => goalPosition;
    }

    private sealed class PooledEnemyView
    {
        public string PrefabKey { get; } // 이 오브젝트가 어느 EnemyData.PrefabKey 풀에 속하는지 나타냅니다.
        public GameObject Instance { get; } // 실제 Unity 프리팹 인스턴스입니다.
        public Animator Animator { get; } // 프리팹 안에 연결된 애니메이터입니다.

        public PooledEnemyView(string prefabKey, GameObject instance, Animator animator)
        {
            PrefabKey = prefabKey;
            Instance = instance;
            Animator = animator;
        }

        // 풀에서 다시 꺼낼 때 이전 피격/사망 애니메이션 상태가 남지 않게 초기화합니다.
        public void ResetForRent()
        {
            if (Animator == null)
                return;

            Animator.Rebind();
            Animator.Update(0f);
        }
    }

    private sealed class ActiveEnemyView
    {
        public string PrefabKey { get; } // 이 몬스터 표시 오브젝트가 속한 풀 키입니다.
        public PooledEnemyView PooledEnemy { get; } // 풀에서 꺼낸 표시 오브젝트입니다.
        public GameObject Instance => PooledEnemy.Instance; // 생성된 몬스터 프리팹 인스턴스입니다.
        public BattleLanePath LanePath { get; } // 이 몬스터가 따라가는 전투 라인입니다.
        public EnemyViewState State { get; private set; } // 현재 표시 상태입니다.

        public ActiveEnemyView(string prefabKey, PooledEnemyView pooledEnemy, BattleLanePath lanePath)
        {
            PrefabKey = prefabKey;
            PooledEnemy = pooledEnemy;
            LanePath = lanePath;
            State = EnemyViewState.Pooled;
        }

        // 표시 상태를 바꾸고, 프리팹에 Animator가 있으면 공통 파라미터를 조심스럽게 전달합니다.
        public void ChangeState(EnemyViewState nextState)
        {
            State = nextState;

            if (PooledEnemy?.Animator == null)
                return;

            Animator animator = PooledEnemy.Animator;

            if (HasAnimatorParameter(animator, "IsMoving"))
                animator.SetBool("IsMoving", nextState == EnemyViewState.Moving);

            if (nextState == EnemyViewState.Hit && HasAnimatorParameter(animator, "Hit"))
                animator.SetTrigger("Hit");

            if (nextState == EnemyViewState.Defeated && HasAnimatorParameter(animator, "Die"))
                animator.SetTrigger("Die");
        }

        // 사용하는 프리팹마다 Animator 파라미터가 다를 수 있으므로 존재 여부를 확인한 뒤에만 값을 넣습니다.
        private static bool HasAnimatorParameter(Animator animator, string parameterName)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName)
                    return true;
            }

            return false;
        }
    }

    private enum EnemyViewState
    {
        Pooled, // 풀에 들어가 비활성화된 상태
        Spawned, // 전장에 막 생성된 상태
        Moving, // 성채를 향해 이동 중인 상태
        Hit, // 피해를 받은 상태
        Defeated, // 체력이 0이 되어 처치된 상태
        ReachedGoal // 성채에 도착해 피해를 준 상태
    }

    private sealed class PendingPoolReturn
    {
        public ActiveEnemyView ActiveEnemy { get; } // 제거 상태를 잠깐 보여준 뒤 풀로 돌아갈 몬스터입니다.
        public float RemainingSeconds { get; set; } // 풀 반납까지 남은 시간입니다.

        public PendingPoolReturn(ActiveEnemyView activeEnemy, float remainingSeconds)
        {
            ActiveEnemy = activeEnemy;
            RemainingSeconds = remainingSeconds;
        }
    }
}
