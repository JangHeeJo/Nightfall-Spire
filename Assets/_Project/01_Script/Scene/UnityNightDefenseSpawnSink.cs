using System;
using System.Collections.Generic;
using UnityEngine;

// 전투 계산 결과를 Unity 씬 오브젝트로 반영하는 표시 어댑터입니다.
// 전투 런타임은 데이터와 계산만 담당하고, 이 클래스는 몬스터 프리팹 생성과 위치 갱신만 담당합니다.
public sealed class UnityNightDefenseSpawnSink : MonoBehaviour, IBattleCombatViewSink
{
    private static readonly string[] DefeatedAnimationNames = { "Dead1" }; // 사망 연출로 인정할 클립 이름입니다.
    private static readonly string[] ReachedGoalAnimationNames = { "Attack" }; // 성채 도착 정리 때 보여줄 클립 이름입니다.
    private const float AnimationReturnBufferSeconds = 0.05f; // 클립 마지막 프레임이 보이도록 풀 반납 전에 더하는 짧은 여유 시간입니다.

    [SerializeField] private Transform heroLayer; // 전투 중 생성되는 영웅 오브젝트를 모아둘 부모입니다.
    [SerializeField] private HeroPrefabBinding[] heroPrefabs = Array.Empty<HeroPrefabBinding>(); // HeroData.PrefabKey와 실제 영웅 프리팹의 연결 목록입니다.
    [SerializeField] private Transform enemyLayer; // 전투 중 생성되는 몬스터 오브젝트를 모아둘 부모입니다.
    [SerializeField] private EnemyPrefabBinding[] enemyPrefabs = Array.Empty<EnemyPrefabBinding>(); // EnemyData.PrefabKey와 실제 몬스터 프리팹의 연결 목록입니다.
    [SerializeField] private BattleLanePath[] lanePaths = Array.Empty<BattleLanePath>(); // LaneId별 몬스터 시작 위치와 성채 도착 위치입니다.
    [SerializeField] private int preloadCountPerPrefab = 3; // 프리팹별로 미리 만들어둘 풀 오브젝트 수입니다.
    [SerializeField] private float despawnDelaySeconds = 0.55f; // 제거 클립 길이를 찾지 못했을 때 사용할 기본 풀 반납 대기 시간입니다.
    [SerializeField] private float animationCrossFadeSeconds = 0.06f; // 공통 Idle/Walk/Attack/Dead1 상태 전환에 사용할 블렌딩 시간입니다.
    [SerializeField] private float sameLaneVisualSpacing = 0.04f; // 같은 라인 몬스터가 완전히 겹치지 않도록 진행도에서 뒤로 밀어낼 간격입니다.
    [SerializeField] private float sameLaneVisualYOffset = 0.12f; // 같은 라인 몬스터가 겹쳐 보이지 않도록 위아래로 벌릴 간격입니다.
    [SerializeField] private bool logRuntimeEvents = true; // 전투 표시 흐름을 확인하기 위한 로그 출력 여부입니다.

    private readonly Dictionary<string, GameObject> heroPrefabByKey = new(); // PrefabKey로 영웅 프리팹을 빠르게 찾기 위한 캐시입니다.
    private readonly Dictionary<string, GameObject> prefabByKey = new(); // PrefabKey로 실제 프리팹을 빠르게 찾기 위한 캐시입니다.
    private readonly Dictionary<int, BattleLanePath> laneById = new(); // LaneId로 이동 경로를 빠르게 찾기 위한 캐시입니다.
    private readonly Dictionary<int, ActiveHeroView> activeHeroesBySlotIndex = new(); // SlotIndex별 현재 배치된 영웅 표시 오브젝트입니다.
    private readonly Dictionary<string, Stack<PooledEnemyView>> pooledEnemiesByKey = new(); // PrefabKey별 재사용 대기 중인 몬스터 표시 오브젝트입니다.
    private readonly Dictionary<int, ActiveEnemyView> activeEnemiesByRuntimeId = new(); // RuntimeId별 현재 전장에 배치된 몬스터 표시 오브젝트입니다.
    private readonly List<PendingPoolReturn> pendingPoolReturns = new(); // 제거 연출 후 풀로 반납할 몬스터 표시 오브젝트입니다.
    private bool isBattleViewPaused; // 드래프트 중 표시 계층 애니메이션과 지연 반납 타이머를 멈췄는지 여부입니다.

    public bool HasPendingEnemyRemoval => pendingPoolReturns.Count > 0; // 죽는 연출이 끝나지 않은 몬스터가 있는지 여부입니다.

    // 인스펙터에 연결된 프리팹과 라인 정보를 전투 중 조회하기 쉬운 형태로 준비합니다.
    private void Awake()
    {
        BuildHeroPrefabCache();
        BuildPrefabCache();
        BuildLaneCache();
        PreloadEnemyPools();
    }

    // 제거 상태를 잠깐 보여준 몬스터를 시간이 지난 뒤 풀로 반납합니다.
    private void Update()
    {
        if (isBattleViewPaused)
            return;

        ProcessPendingPoolReturns(Time.deltaTime);
    }

    // 새 전투가 시작될 때 기존 전투 표시 오브젝트를 풀로 되돌립니다.
    public void ClearBattleViews()
    {
        SetBattleViewPaused(false);
        ClearHeroViews();

        for (int i = pendingPoolReturns.Count - 1; i >= 0; i--)
            ReturnToPoolImmediately(pendingPoolReturns[i].ActiveEnemy);

        pendingPoolReturns.Clear();

        foreach (ActiveEnemyView activeEnemy in activeEnemiesByRuntimeId.Values)
            ReturnToPoolImmediately(activeEnemy);

        activeEnemiesByRuntimeId.Clear();

        if (logRuntimeEvents)
            Debug.Log("[BattleViewSink] 전투 표시 오브젝트를 초기화했습니다.");
    }

    // 현재 전투에 배치된 영웅 슬롯 목록을 실제 씬 프리팹으로 생성합니다.
    public void ShowHeroSlots(IReadOnlyList<CombatHeroSlotRuntimeState> heroSlots)
    {
        ClearHeroViews();

        if (heroSlots == null)
            return;

        for (int i = 0; i < heroSlots.Count; i++)
        {
            CombatHeroSlotRuntimeState slot = heroSlots[i];

            if (!TryCreateHeroView(slot, out ActiveHeroView activeHero))
                continue;

            activeHeroesBySlotIndex[slot.SlotIndex] = activeHero;

            if (logRuntimeEvents)
                Debug.Log($"[BattleViewSink] Hero Slot:{slot.SlotIndex} Hero:{slot.HeroId} Floor:{slot.FloorId} FloorSlot:{slot.FloorSlotIndex} Pos:({slot.LocalPositionX:0.00},{slot.LocalPositionY:0.00}) Target:{slot.MinTargetProgress:0.00}-{slot.MaxTargetProgress:0.00}");
        }
    }

    // 전투 런타임에서 몬스터 생성이 확정되면 대응되는 Unity 프리팹을 생성합니다.
    public void SpawnEnemyView(CombatEnemyRuntimeState enemy, NightDefenseSpawnRequest request)
    {
        if (enemy == null)
            return;

        if (!TryCreateEnemyView(enemy, request, out ActiveEnemyView activeEnemy))
            return;

        activeEnemy.ChangeState(EnemyViewState.Spawned, enemy.MoveSpeed);
        activeEnemiesByRuntimeId[enemy.RuntimeId] = activeEnemy;
        SyncEnemyTransform(activeEnemy, enemy);
        activeEnemy.ChangeState(EnemyViewState.Moving, enemy.MoveSpeed);

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

            EnemyViewState nextState = enemy.IsAttackingCastle ? EnemyViewState.ReachedGoal : EnemyViewState.Moving;

            if (activeEnemy.State != nextState)
                activeEnemy.ChangeState(nextState, enemy.MoveSpeed);
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

            if (activeHeroesBySlotIndex.TryGetValue(damageResult.AttackerSlotIndex, out ActiveHeroView activeHero))
                activeHero.ChangeState(HeroViewState.Attacking);

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

        activeEnemiesByRuntimeId.Remove(enemy.RuntimeId);
        ScheduleReturnToPool(activeEnemy, reason);

        if (logRuntimeEvents)
            Debug.Log($"[BattleViewSink] Enemy Despawn Runtime:{enemy.RuntimeId} Reason:{reason}");
    }

    // 카드 드래프트 중에는 전투 표시 애니메이션도 멈추고, 선택 후 다시 재생합니다.
    public void SetBattleViewPaused(bool isPaused)
    {
        if (isBattleViewPaused == isPaused)
            return;

        isBattleViewPaused = isPaused;
        float animatorSpeed = isPaused ? 0f : 1f;

        foreach (ActiveHeroView activeHero in activeHeroesBySlotIndex.Values)
            SetAllAnimatorSpeeds(activeHero.Instance, animatorSpeed);

        foreach (ActiveEnemyView activeEnemy in activeEnemiesByRuntimeId.Values)
            SetAllAnimatorSpeeds(activeEnemy.Instance, animatorSpeed);

        for (int i = 0; i < pendingPoolReturns.Count; i++)
            SetAllAnimatorSpeeds(pendingPoolReturns[i].ActiveEnemy?.Instance, animatorSpeed);
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
        activeEnemy = new ActiveEnemyView(prefabKey, pooledEnemy, lanePath, enemy.EnemyRank == EnemyRank.Boss);

        if (isBattleViewPaused)
            SetAllAnimatorSpeeds(instance, 0f);

        return true;
    }

    // HeroData의 PrefabKey를 인스펙터에 연결한 Unity 프리팹으로 변환해 슬롯 위치에 생성합니다.
    private bool TryCreateHeroView(CombatHeroSlotRuntimeState slot, out ActiveHeroView activeHero)
    {
        activeHero = default;

        if (slot == null)
            return false;

        if (!TryGetHeroPrefabKey(slot.HeroId, out string prefabKey))
            return false;

        if (!heroPrefabByKey.TryGetValue(prefabKey, out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"[BattleViewSink] PrefabKey '{prefabKey}'에 연결된 영웅 프리팹이 없습니다. UnityNightDefenseSpawnSink 인스펙터의 heroPrefabs를 확인해야 합니다.");
            return false;
        }

        Transform parent = heroLayer != null ? heroLayer : enemyLayer;
        GameObject instance = Instantiate(prefab, parent);
        instance.name = $"{prefab.name}_Slot_{slot.SlotIndex}";
        instance.SetActive(true);
        instance.transform.localPosition = new Vector3(slot.LocalPositionX, slot.LocalPositionY, -0.35f);
        instance.transform.localRotation = Quaternion.identity;
        SetSortingOrder(instance, 200 + slot.FloorId * 10 + slot.FloorSlotIndex);

        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
        CombatUnitAnimationPlayer animationPlayer = new CombatUnitAnimationPlayer(animators, animationCrossFadeSeconds);
        activeHero = new ActiveHeroView(prefabKey, instance, animationPlayer, slot.AttackSpeed);
        activeHero.ChangeState(HeroViewState.Idle);

        if (isBattleViewPaused)
            SetAllAnimatorSpeeds(instance, 0f);

        return true;
    }

    // GameContext의 콘텐츠 테이블에서 HeroId에 해당하는 프리팹 키를 가져옵니다.
    private bool TryGetHeroPrefabKey(int heroId, out string prefabKey)
    {
        prefabKey = string.Empty;
        GameContext context = GameRoot.Instance != null ? GameRoot.Instance.Context : null;

        if (context?.ContentDataSource == null)
        {
            Debug.LogError("[BattleViewSink] GameContext 또는 ContentDataSource가 없어 HeroData를 조회할 수 없습니다.");
            return false;
        }

        if (!context.ContentDataSource.TryGetHero(heroId, out HeroDataRow heroRow) || heroRow == null)
        {
            Debug.LogError($"[BattleViewSink] HeroId {heroId}에 해당하는 HeroData를 찾지 못했습니다.");
            return false;
        }

        prefabKey = heroRow.PrefabKey;
        return !string.IsNullOrWhiteSpace(prefabKey);
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
    private void SyncEnemyTransform(ActiveEnemyView activeEnemy, CombatEnemyRuntimeState enemy)
    {
        if (activeEnemy.Instance == null)
            return;

        float visualProgress = enemy.PathProgress;

        if (!enemy.IsAttackingCastle && sameLaneVisualSpacing > 0f)
            visualProgress = Mathf.Max(0f, visualProgress - enemy.SpawnOrder % 4 * sameLaneVisualSpacing);

        Vector3 position = Vector3.Lerp(activeEnemy.LanePath.StartPosition, activeEnemy.LanePath.GoalPosition, visualProgress);

        if (!enemy.IsAttackingCastle && sameLaneVisualYOffset > 0f)
            position.y += (enemy.SpawnOrder % 3 - 1) * sameLaneVisualYOffset;

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

    // 프리팹 안에 Animator가 여러 개 있어도 드래프트 일시정지가 전부 적용되게 합니다.
    private static void SetAllAnimatorSpeeds(GameObject instance, float speed)
    {
        if (instance == null)
            return;

        float normalizedSpeed = Mathf.Max(0f, speed);
        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
            animators[i].speed = normalizedSpeed;
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

    // HeroData.PrefabKey와 실제 Unity 프리팹 연결 목록을 캐시합니다.
    private void BuildHeroPrefabCache()
    {
        heroPrefabByKey.Clear();

        for (int i = 0; i < heroPrefabs.Length; i++)
        {
            HeroPrefabBinding binding = heroPrefabs[i];

            if (string.IsNullOrWhiteSpace(binding.PrefabKey) || binding.Prefab == null)
                continue;

            heroPrefabByKey[binding.PrefabKey] = binding.Prefab;
        }
    }

    // 슬롯 구성 변경이나 전투 초기화 때 현재 생성된 영웅 표시 오브젝트를 제거합니다.
    private void ClearHeroViews()
    {
        foreach (ActiveHeroView activeHero in activeHeroesBySlotIndex.Values)
        {
            if (activeHero.Instance != null)
                Destroy(activeHero.Instance);
        }

        activeHeroesBySlotIndex.Clear();
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
        pendingPoolReturns.Add(new PendingPoolReturn(activeEnemy, GetPoolReturnDelaySeconds(activeEnemy, reason)));
    }

    // 제거 사유에 맞는 애니메이션 클립 길이를 찾아서, 연출이 끝난 뒤 풀로 반납되게 합니다.
    private float GetPoolReturnDelaySeconds(ActiveEnemyView activeEnemy, CombatEnemyDespawnReason reason)
    {
        float fallbackDelaySeconds = Mathf.Max(0f, despawnDelaySeconds);

        if (activeEnemy?.Instance == null)
            return fallbackDelaySeconds;

        string[] clipNames = reason switch
        {
            CombatEnemyDespawnReason.Defeated => DefeatedAnimationNames,
            CombatEnemyDespawnReason.ReachedGoal => ReachedGoalAnimationNames,
            _ => null
        };

        float clipLengthSeconds = GetLongestAnimationClipSeconds(activeEnemy.Instance, clipNames);

        if (clipLengthSeconds <= 0f)
            return fallbackDelaySeconds;

        return clipLengthSeconds + AnimationReturnBufferSeconds;
    }

    // 프리팹 내부 모든 Animator에서 요청한 이름의 클립 중 가장 긴 길이를 반환합니다.
    private static float GetLongestAnimationClipSeconds(GameObject instance, IReadOnlyList<string> clipNames)
    {
        if (instance == null || clipNames == null || clipNames.Count == 0)
            return 0f;

        float longestSeconds = 0f;
        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            RuntimeAnimatorController controller = animators[i].runtimeAnimatorController;

            if (controller == null || controller.animationClips == null)
                continue;

            AnimationClip[] clips = controller.animationClips;

            for (int j = 0; j < clips.Length; j++)
            {
                AnimationClip clip = clips[j];

                if (clip == null || !IsNamedClip(clip.name, clipNames))
                    continue;

                if (clip.length > longestSeconds)
                    longestSeconds = clip.length;
            }
        }

        return longestSeconds;
    }

    // Animator Controller마다 클립명이 다를 수 있으니 허용 이름 목록으로 비교합니다.
    private static bool IsNamedClip(string clipName, IReadOnlyList<string> clipNames)
    {
        if (string.IsNullOrWhiteSpace(clipName))
            return false;

        for (int i = 0; i < clipNames.Count; i++)
        {
            if (string.Equals(clipName, clipNames[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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
        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
        CombatUnitAnimationPlayer animationPlayer = new CombatUnitAnimationPlayer(animators, animationCrossFadeSeconds);
        return new PooledEnemyView(prefabKey, instance, animationPlayer);
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
    private sealed class HeroPrefabBinding
    {
        [SerializeField] private string prefabKey; // HeroData.tsv의 PrefabKey입니다.
        [SerializeField] private GameObject prefab; // 실제 생성할 영웅 프리팹입니다.

        public string PrefabKey => prefabKey;
        public GameObject Prefab => prefab;
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
        public CombatUnitAnimationPlayer AnimationPlayer { get; } // 공통 전투 애니메이션 재생기입니다.

        public PooledEnemyView(string prefabKey, GameObject instance, CombatUnitAnimationPlayer animationPlayer)
        {
            PrefabKey = prefabKey;
            Instance = instance;
            AnimationPlayer = animationPlayer;
        }

        // 풀에서 다시 꺼낼 때 이전 피격/사망 애니메이션 상태가 남지 않게 초기화합니다.
        public void ResetForRent()
        {
            AnimationPlayer?.ResetPlayback();
        }
    }

    private sealed class ActiveEnemyView
    {
        public string PrefabKey { get; } // 이 몬스터 표시 오브젝트가 속한 풀 키입니다.
        public PooledEnemyView PooledEnemy { get; } // 풀에서 꺼낸 표시 오브젝트입니다.
        public GameObject Instance => PooledEnemy.Instance; // 생성된 몬스터 프리팹 인스턴스입니다.
        public BattleLanePath LanePath { get; } // 이 몬스터가 따라가는 전투 라인입니다.
        public bool CanUseSkill { get; } // 보스 몬스터만 Skill 애니메이션을 사용할 수 있습니다.
        public EnemyViewState State { get; private set; } // 현재 표시 상태입니다.

        public ActiveEnemyView(string prefabKey, PooledEnemyView pooledEnemy, BattleLanePath lanePath, bool canUseSkill)
        {
            PrefabKey = prefabKey;
            PooledEnemy = pooledEnemy;
            LanePath = lanePath;
            CanUseSkill = canUseSkill;
            State = EnemyViewState.Pooled;
        }

        // 표시 상태를 바꾸고, 프리팹에 Animator가 있으면 공통 파라미터를 조심스럽게 전달합니다.
        public void ChangeState(EnemyViewState nextState, float moveSpeed = 1f)
        {
            State = nextState;

            switch (nextState)
            {
                case EnemyViewState.Pooled:
                case EnemyViewState.Spawned:
                    PooledEnemy?.AnimationPlayer?.PlayIdle();
                    break;
                case EnemyViewState.Moving:
                    PooledEnemy?.AnimationPlayer?.PlayWalk(moveSpeed);
                    break;
                case EnemyViewState.Hit:
                    break;
                case EnemyViewState.Defeated:
                    PooledEnemy?.AnimationPlayer?.PlayDead1();
                    break;
                case EnemyViewState.ReachedGoal:
                    PooledEnemy?.AnimationPlayer?.PlayAttack();
                    break;
            }
        }

        // 보스 스킬 시스템이 붙을 때만 Skill 애니메이션을 재생합니다.
        public void PlaySkill(float skillSpeed = 1f)
        {
            if (!CanUseSkill)
                return;

            PooledEnemy?.AnimationPlayer?.PlaySkill(skillSpeed);
        }
    }

    private sealed class ActiveHeroView
    {
        public string PrefabKey { get; } // 이 영웅 표시 오브젝트가 어떤 HeroData.PrefabKey에서 생성됐는지 나타냅니다.
        public GameObject Instance { get; } // 실제 Unity 프리팹 인스턴스입니다.
        public CombatUnitAnimationPlayer AnimationPlayer { get; } // 공통 전투 애니메이션 재생기입니다.
        public float AttackSpeed { get; } // 공격 연출 배속에 사용할 슬롯 공격 속도입니다.
        public HeroViewState State { get; private set; } // 현재 표시 상태입니다.

        public ActiveHeroView(string prefabKey, GameObject instance, CombatUnitAnimationPlayer animationPlayer, float attackSpeed)
        {
            PrefabKey = prefabKey;
            Instance = instance;
            AnimationPlayer = animationPlayer;
            AttackSpeed = attackSpeed;
            State = HeroViewState.Idle;
        }

        // 표시 상태를 바꾸고, 프리팹에 Animator가 있으면 공통 파라미터를 조심스럽게 전달합니다.
        public void ChangeState(HeroViewState nextState)
        {
            State = nextState;

            switch (nextState)
            {
                case HeroViewState.Idle:
                    AnimationPlayer?.PlayIdle();
                    break;
                case HeroViewState.Attacking:
                    AnimationPlayer?.PlayAttack(AttackSpeed);
                    break;
            }
        }

        // 영웅 스킬 시스템이 붙을 때 사용할 Skill 애니메이션 진입점입니다.
        public void PlaySkill(float skillSpeed = 1f)
        {
            AnimationPlayer?.PlaySkill(skillSpeed);
        }
    }

    private enum HeroViewState
    {
        Idle, // 전투 슬롯에 배치되어 대기 중인 상태
        Attacking // 공격 판정이 발생해 공격 애니메이션을 재생하는 상태
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
