using System;
using System.Collections.Generic;

// 전투 슬롯 공격, 적 상태, 피해 적용을 진행하는 순수 C# 전투 런타임입니다.
// Unity 프리팹 생성과 애니메이션은 이 클래스 밖에서 처리하고, 이 클래스는 전투 규칙 계산만 담당합니다.
public sealed class CombatRuntimeController
{
    private const float EnemyProgressPerSpeed = 0.01f; // MoveSpeed를 PathProgress로 바꾸는 임시 기준값
    private const int MaxAttacksPerSlotPerTick = 8; // 큰 deltaSeconds가 들어와도 한 Tick에서 공격이 폭주하지 않게 막는 상한

    private readonly ICombatDataSource dataSource; // 영웅, 적, 슬롯 테이블 조회 계약
    private readonly CombatSlotProgress combatSlotProgress; // 현재 해금/배치된 전투 슬롯 상태
    private readonly CombatTargetingService targetingService; // 타겟 선택 규칙
    private readonly CombatDamageResolver damageResolver; // 피해 계산 규칙
    private readonly List<CombatHeroSlotRuntimeState> heroSlots = new(); // 공격 가능한 영웅 슬롯 목록
    private readonly List<CombatEnemyRuntimeState> enemies = new(); // 현재 살아 있거나 정리 대기 중인 적 목록
    private readonly List<CombatDamageResult> damageResults = new(); // 최근 Tick 피해 결과 목록

    private int nextEnemyRuntimeId = 1; // 전투 내 적 인스턴스 ID 발급값

    public IReadOnlyList<CombatHeroSlotRuntimeState> HeroSlots => heroSlots; // 외부 조회용 영웅 슬롯 목록
    public IReadOnlyList<CombatEnemyRuntimeState> Enemies => enemies; // 외부 조회용 적 목록
    public IReadOnlyList<CombatDamageResult> LastDamageResults => damageResults; // 마지막 Tick 피해 결과

    // 전투 계산에 필요한 데이터 조회 계약과 현재 슬롯 진행 상태를 받습니다.
    public CombatRuntimeController(
        ICombatDataSource dataSource,
        CombatSlotProgress combatSlotProgress,
        CombatTargetingService targetingService = null,
        CombatDamageResolver damageResolver = null)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        this.combatSlotProgress = combatSlotProgress ?? throw new ArgumentNullException(nameof(combatSlotProgress));
        this.targetingService = targetingService ?? new CombatTargetingService();
        this.damageResolver = damageResolver ?? new CombatDamageResolver();
    }

    // 현재 CombatSlotProgress와 테이블을 기준으로 공격 가능한 영웅 슬롯을 다시 구성합니다.
    public CombatRuntimeFailureReason RebuildHeroSlots()
    {
        heroSlots.Clear();

        foreach (CombatSlotRuntimeState slot in combatSlotProgress.SlotsByIndex.Values)
        {
            if (!slot.IsUnlocked.Value || slot.EquippedHeroId.Value <= 0)
                continue;

            if (!dataSource.TryGetCombatSlotByIndex(slot.SlotIndex, out CombatSlotDataRow slotRow))
                return CombatRuntimeFailureReason.CombatSlotNotFound;

            if (!dataSource.TryGetHero(slot.EquippedHeroId.Value, out HeroDataRow heroRow))
                return CombatRuntimeFailureReason.HeroNotFound;

            CombatSlotUpgradeDataRow upgradeRow = null;
            dataSource.TryGetCombatSlotUpgrade(slotRow.UpgradeGroupId, slot.Level.Value, out upgradeRow);

            heroSlots.Add(CreateHeroSlot(slot, heroRow, upgradeRow));
        }

        return heroSlots.Count > 0
            ? CombatRuntimeFailureReason.None
            : CombatRuntimeFailureReason.NoActiveHeroSlots;
    }

    // 스폰 요청을 실제 전투 계산용 적 상태로 등록합니다.
    public CombatRuntimeFailureReason SpawnEnemy(NightDefenseSpawnRequest request, out CombatEnemyRuntimeState enemy)
    {
        enemy = null;

        if (!dataSource.TryGetEnemy(request.EnemyId, out EnemyDataRow enemyRow))
            return CombatRuntimeFailureReason.EnemyNotFound;

        enemy = new CombatEnemyRuntimeState(
            nextEnemyRuntimeId,
            enemyRow.EnemyId,
            enemyRow.EnemyRank,
            enemyRow.ElementType,
            request.LaneId,
            request.SpawnOrder,
            enemyRow.MaxHp,
            enemyRow.Armor,
            enemyRow.MoveSpeed);

        nextEnemyRuntimeId += 1;
        enemies.Add(enemy);
        return CombatRuntimeFailureReason.None;
    }

    // 전투 시간을 진행하고 슬롯 공격과 피해 적용을 처리합니다.
    public CombatRuntimeTickResult Tick(float deltaSeconds)
    {
        damageResults.Clear();

        if (deltaSeconds <= 0f)
            return CreateTickResult(0, 0);

        AdvanceEnemies(deltaSeconds);

        int attackCount = 0;
        int defeatedCount = 0;

        for (int i = 0; i < heroSlots.Count; i++)
        {
            CombatHeroSlotRuntimeState slot = heroSlots[i];

            if (!slot.CanAttack)
                continue;

            slot.AddAttackTime(deltaSeconds);
            float attackInterval = 1f / slot.AttackSpeed;
            int slotAttackCount = 0;

            while (slot.AttackTimer >= attackInterval && slotAttackCount < MaxAttacksPerSlotPerTick)
            {
                CombatEnemyRuntimeState target = targetingService.SelectTarget(slot.TargetingType, enemies);

                if (target == null)
                    break;

                CombatDamageResult damageResult = damageResolver.ApplyBasicAttack(slot, target);
                damageResults.Add(damageResult);
                attackCount += 1;
                slotAttackCount += 1;
                slot.ConsumeAttackTime(attackInterval);

                if (damageResult.IsTargetDefeated)
                    defeatedCount += 1;
            }
        }

        RemoveDefeatedEnemies();
        return CreateTickResult(attackCount, defeatedCount);
    }

    // 현재 전투 상태를 초기화합니다.
    public void Reset()
    {
        heroSlots.Clear();
        enemies.Clear();
        damageResults.Clear();
        nextEnemyRuntimeId = 1;
    }

    // HeroDataRow와 슬롯 성장값을 합쳐 공격자 상태를 만듭니다.
    private static CombatHeroSlotRuntimeState CreateHeroSlot(CombatSlotRuntimeState slot, HeroDataRow heroRow, CombatSlotUpgradeDataRow upgradeRow)
    {
        float attackBonus = upgradeRow == null ? 0f : upgradeRow.AttackBonusPct;
        float attackSpeedBonus = upgradeRow == null ? 0f : upgradeRow.AttackSpeedBonusPct;
        int attackPower = Math.Max(1, (int)Math.Ceiling(heroRow.BaseAttack * (1f + attackBonus / 100f)));
        float attackSpeed = Math.Max(0.01f, heroRow.BaseAttackSpeed * (1f + attackSpeedBonus / 100f));

        return new CombatHeroSlotRuntimeState(
            slot.SlotIndex,
            heroRow.HeroId,
            heroRow.HeroRole,
            heroRow.ElementType,
            heroRow.TargetingType,
            attackPower,
            attackSpeed);
    }

    // 살아 있는 적의 이동 진행도를 갱신합니다.
    private void AdvanceEnemies(float deltaSeconds)
    {
        for (int i = 0; i < enemies.Count; i++)
            enemies[i].Advance(deltaSeconds, EnemyProgressPerSpeed);
    }

    // 처치된 적을 런타임 목록에서 제거합니다.
    private void RemoveDefeatedEnemies()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (!enemies[i].IsAlive)
                enemies.RemoveAt(i);
        }
    }

    // 현재 상태를 Tick 결과로 요약합니다.
    private CombatRuntimeTickResult CreateTickResult(int attackCount, int defeatedCount)
    {
        return new CombatRuntimeTickResult(attackCount, defeatedCount, enemies.Count, heroSlots.Count);
    }
}
