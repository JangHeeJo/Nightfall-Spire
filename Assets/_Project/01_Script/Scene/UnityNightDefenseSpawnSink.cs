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
    [SerializeField] private bool logRuntimeEvents = true; // 전투 표시 흐름을 확인하기 위한 로그 출력 여부입니다.

    private readonly Dictionary<string, GameObject> prefabByKey = new(); // PrefabKey로 실제 프리팹을 빠르게 찾기 위한 캐시입니다.
    private readonly Dictionary<int, BattleLanePath> laneById = new(); // LaneId로 이동 경로를 빠르게 찾기 위한 캐시입니다.
    private readonly Dictionary<int, ActiveEnemyView> activeEnemies = new(); // RuntimeId별 현재 생성된 몬스터 표시 오브젝트입니다.

    // 인스펙터에 연결된 프리팹과 라인 정보를 전투 중 조회하기 쉬운 형태로 준비합니다.
    private void Awake()
    {
        BuildPrefabCache();
        BuildLaneCache();
    }

    // 새 전투가 시작될 때 기존 전투 표시 오브젝트를 모두 정리합니다.
    public void ClearBattleViews()
    {
        foreach (ActiveEnemyView activeEnemy in activeEnemies.Values)
        {
            if (activeEnemy.Instance != null)
                Destroy(activeEnemy.Instance);
        }

        activeEnemies.Clear();

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

        activeEnemies[enemy.RuntimeId] = activeEnemy;
        SyncEnemyTransform(activeEnemy, enemy);

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

            if (enemy == null || !activeEnemies.TryGetValue(enemy.RuntimeId, out ActiveEnemyView activeEnemy))
                continue;

            SyncEnemyTransform(activeEnemy, enemy);
        }
    }

    // 데미지 결과를 표시합니다. 현재는 전투 흐름 검증용 로그만 남기고, 이후 데미지 숫자와 피격 연출이 여기 붙습니다.
    public void ShowDamageResults(IReadOnlyList<CombatDamageResult> damageResults)
    {
        if (!logRuntimeEvents || damageResults == null || damageResults.Count == 0)
            return;

        Debug.Log($"[BattleViewSink] Damage Count:{damageResults.Count}");
    }

    // 사망하거나 성채에 도착한 몬스터 표시 오브젝트를 제거합니다.
    public void DespawnEnemyView(CombatEnemyRuntimeState enemy, CombatEnemyDespawnReason reason)
    {
        if (enemy == null)
            return;

        if (activeEnemies.TryGetValue(enemy.RuntimeId, out ActiveEnemyView activeEnemy))
        {
            activeEnemies.Remove(enemy.RuntimeId);

            if (activeEnemy.Instance != null)
                Destroy(activeEnemy.Instance);
        }

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

        GameObject instance = Instantiate(prefab, enemyLayer);
        instance.name = $"{prefab.name}_Runtime_{enemy.RuntimeId}";
        SetSortingOrder(instance, 100 + request.LaneId * 10 + request.SpawnOrder);
        activeEnemy = new ActiveEnemyView(instance, lanePath);
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

    // 생성된 몬스터 프리팹 안의 SpriteRenderer 정렬값을 라인과 생성 순서 기준으로 보정합니다.
    private static void SetSortingOrder(GameObject instance, int baseOrder)
    {
        if (instance == null)
            return;

        SpriteRenderer[] renderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder += baseOrder;
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

    private readonly struct ActiveEnemyView
    {
        public GameObject Instance { get; } // 생성된 몬스터 프리팹 인스턴스입니다.
        public BattleLanePath LanePath { get; } // 이 몬스터가 따라가는 전투 라인입니다.

        public ActiveEnemyView(GameObject instance, BattleLanePath lanePath)
        {
            Instance = instance;
            LanePath = lanePath;
        }
    }
}
