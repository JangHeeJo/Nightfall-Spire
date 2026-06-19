// 전투 런타임에서 공유하는 작은 값 타입과 결과 계약입니다.
// 슬롯 공격, 적 상태, 피해 결과처럼 전투 규칙 엔진 안에서 함께 쓰는 값을 모아둡니다.

// 전투 런타임 계산 실패 이유입니다.
public enum CombatRuntimeFailureReason
{
    None, // 실패 없음
    HeroNotFound, // 슬롯에 배치된 영웅 데이터를 찾지 못함
    EnemyNotFound, // 스폰 요청의 적 데이터를 찾지 못함
    CombatSlotNotFound, // 슬롯 번호에 맞는 전투 슬롯 데이터를 찾지 못함
    NoActiveHeroSlots // 공격 가능한 해금 슬롯이 없음
}

// 전투 런타임에서 적이 제거된 이유입니다.
public enum CombatEnemyDespawnReason
{
    None, // 제거 사유 없음
    Defeated, // 영웅 공격으로 처치됨
    ReachedGoal, // 성채에 도달한 뒤 전투 표시 정리가 필요할 때 사용함
    Reset // 전투 종료나 씬 정리로 제거됨
}

// 전투 런타임 안에서 적 하나가 어떤 단계에 있는지 나타냅니다.
public enum CombatEnemyLifecycleState
{
    Spawned, // 전투 런타임에 막 등록됨
    Moving, // 성채를 향해 이동 중
    AttackingCastle, // 성채 앞에 도착해 공격 중
    Defeated // 체력이 0이 되어 처치됨
}

// 전투 슬롯 하나가 런타임에서 공격자로 쓰기 위해 필요한 상태입니다.
public sealed class CombatHeroSlotRuntimeState
{
    public int SlotIndex { get; } // 전투 슬롯 번호
    public int HeroId { get; } // 슬롯에 배치된 영웅 ID
    public CombatSlotType SlotType { get; } // 전투 슬롯 타입
    public int FloorId { get; } // 영웅이 배치된 성채 층 ID
    public int FloorSlotIndex { get; } // 같은 층 안에서의 슬롯 순서
    public HeroRole HeroRole { get; } // 영웅 역할
    public ElementType ElementType { get; } // 공격 속성
    public TargetingType TargetingType { get; } // 타겟 선택 규칙
    public int MaxHealth { get; } // 영웅 기본 체력
    public int Defense { get; } // 영웅 기본 방어력
    public int AttackPower { get; } // 슬롯 성장 보정이 반영된 공격력
    public float AttackSpeed { get; } // 슬롯 성장 보정이 반영된 초당 공격 횟수
    public float AttackRange { get; } // 영웅 기본 사거리에 슬롯 보정이 반영된 값
    public float LocalPositionX { get; } // 성채 루트 기준 슬롯 X 좌표
    public float LocalPositionY { get; } // 성채 루트 기준 슬롯 Y 좌표
    public float MinTargetProgress { get; } // 이 슬롯이 공격할 수 있는 몬스터 최소 접근 진행도
    public float MaxTargetProgress { get; } // 이 슬롯이 공격할 수 있는 몬스터 최대 접근 진행도
    public float AttackTimer { get; private set; } // 다음 공격까지 누적된 시간

    public bool CanAttack => AttackPower > 0 && AttackSpeed > 0f; // 공격 가능한 슬롯인지 여부

    // 영웅 슬롯의 전투 계산용 값을 보관합니다.
    public CombatHeroSlotRuntimeState(int slotIndex, int heroId, CombatSlotType slotType, int floorId, int floorSlotIndex, HeroRole heroRole, ElementType elementType, TargetingType targetingType, int maxHealth, int defense, int attackPower, float attackSpeed, float attackRange, float localPositionX, float localPositionY, float minTargetProgress, float maxTargetProgress)
    {
        SlotIndex = slotIndex;
        HeroId = heroId;
        SlotType = slotType;
        FloorId = floorId;
        FloorSlotIndex = floorSlotIndex;
        HeroRole = heroRole;
        ElementType = elementType;
        TargetingType = targetingType;
        MaxHealth = maxHealth;
        Defense = defense;
        AttackPower = attackPower;
        AttackSpeed = attackSpeed;
        AttackRange = attackRange;
        LocalPositionX = localPositionX;
        LocalPositionY = localPositionY;
        MinTargetProgress = minTargetProgress;
        MaxTargetProgress = maxTargetProgress;
    }

    // 슬롯이 맡은 성채 방어 구간 안에 들어온 몬스터인지 판단합니다.
    public bool CanTarget(CombatEnemyRuntimeState enemy)
    {
        if (enemy == null || !enemy.IsAlive)
            return false;

        return enemy.PathProgress >= MinTargetProgress && enemy.PathProgress <= MaxTargetProgress;
    }

    // Tick마다 흐른 시간을 공격 타이머에 더합니다.
    public void AddAttackTime(float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
            return;

        AttackTimer += deltaSeconds;
    }

    // 공격 1회가 실행된 뒤 공격 주기만큼 타이머를 소모합니다.
    public void ConsumeAttackTime(float attackIntervalSec)
    {
        if (attackIntervalSec <= 0f)
            return;

        AttackTimer -= attackIntervalSec;

        if (AttackTimer < 0f)
            AttackTimer = 0f;
    }
}

// 전투 런타임에 살아 있는 적 하나의 상태입니다.
public sealed class CombatEnemyRuntimeState
{
    public int RuntimeId { get; } // 한 전투 안에서 적 인스턴스를 구분하는 ID
    public int EnemyId { get; } // 원본 적 데이터 ID
    public EnemyRank EnemyRank { get; } // 적 등급
    public ElementType ElementType { get; } // 적 속성
    public int LaneId { get; } // 스폰된 라인 ID
    public int SpawnOrder { get; } // 웨이브 안에서의 스폰 순서
    public int MaxHp { get; } // 최대 체력
    public int CurrentHp { get; private set; } // 현재 체력
    public int Armor { get; } // 방어력
    public int AttackPower { get; } // 성채 공격 1회당 피해량
    public float MoveSpeed { get; } // 이동 속도
    public float PathProgress { get; private set; } // 방어 목표 지점까지의 진행도
    public float CastleAttackTimer { get; private set; } // 성채 공격 주기 누적 시간
    public CombatEnemyLifecycleState LifecycleState { get; private set; } // 현재 적 런타임 상태
    public bool IsAlive => CurrentHp > 0; // 적 생존 여부
    public bool IsAttackingCastle => LifecycleState == CombatEnemyLifecycleState.AttackingCastle; // 성채를 공격 중인지 여부

    // 적 런타임 상태를 만듭니다.
    public CombatEnemyRuntimeState(int runtimeId, int enemyId, EnemyRank enemyRank, ElementType elementType, int laneId, int spawnOrder, int maxHp, int armor, int attackPower, float moveSpeed)
    {
        RuntimeId = runtimeId;
        EnemyId = enemyId;
        EnemyRank = enemyRank;
        ElementType = elementType;
        LaneId = laneId;
        SpawnOrder = spawnOrder;
        MaxHp = maxHp;
        CurrentHp = maxHp;
        Armor = armor;
        AttackPower = attackPower;
        MoveSpeed = moveSpeed;
        LifecycleState = CombatEnemyLifecycleState.Spawned;
    }

    // 적 이동 진행도를 올립니다.
    public bool Advance(float deltaSeconds, float progressPerSpeed)
    {
        if (deltaSeconds <= 0f || progressPerSpeed <= 0f || !IsAlive)
            return false;

        if (LifecycleState == CombatEnemyLifecycleState.AttackingCastle)
            return false;

        PathProgress += MoveSpeed * progressPerSpeed * deltaSeconds;
        LifecycleState = CombatEnemyLifecycleState.Moving;

        if (PathProgress > 1f)
            PathProgress = 1f;

        if (PathProgress >= 1f)
        {
            LifecycleState = CombatEnemyLifecycleState.AttackingCastle;
            CastleAttackTimer = 0f;
            return true;
        }

        return false;
    }

    // 성채 앞에 붙은 적의 공격 타이머를 누적합니다.
    public void AddCastleAttackTime(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || !IsAlive || !IsAttackingCastle)
            return;

        CastleAttackTimer += deltaSeconds;
    }

    // 성채 공격 1회가 실행된 뒤 공격 주기만큼 타이머를 소모합니다.
    public void ConsumeCastleAttackTime(float attackIntervalSec)
    {
        if (attackIntervalSec <= 0f)
            return;

        CastleAttackTimer -= attackIntervalSec;

        if (CastleAttackTimer < 0f)
            CastleAttackTimer = 0f;
    }

    // 피해를 적용하고 실제 감소한 체력을 반환합니다.
    public int ApplyDamage(int damage)
    {
        if (damage <= 0 || !IsAlive)
            return 0;

        int applied = damage > CurrentHp ? CurrentHp : damage;
        CurrentHp -= applied;

        if (CurrentHp <= 0)
            LifecycleState = CombatEnemyLifecycleState.Defeated;

        return applied;
    }
}

// 피해 계산 결과입니다.
public readonly struct CombatDamageResult
{
    public int AttackerSlotIndex { get; } // 공격한 슬롯 번호
    public int TargetRuntimeId { get; } // 피해를 받은 적 런타임 ID
    public int RawDamage { get; } // 방어력 적용 전 피해
    public int FinalDamage { get; } // 방어력 적용 후 실제 피해
    public bool IsTargetDefeated { get; } // 이번 피해로 대상이 처치되었는지 여부

    // 피해 계산 결과를 보관합니다.
    public CombatDamageResult(int attackerSlotIndex, int targetRuntimeId, int rawDamage, int finalDamage, bool isTargetDefeated)
    {
        AttackerSlotIndex = attackerSlotIndex;
        TargetRuntimeId = targetRuntimeId;
        RawDamage = rawDamage;
        FinalDamage = finalDamage;
        IsTargetDefeated = isTargetDefeated;
    }
}

// 적 제거 결과입니다.
public readonly struct CombatEnemyDespawnResult
{
    public CombatEnemyRuntimeState Enemy { get; } // 제거된 적 런타임 상태
    public CombatEnemyDespawnReason Reason { get; } // 제거 사유

    // 제거된 적과 이유를 보관합니다.
    public CombatEnemyDespawnResult(CombatEnemyRuntimeState enemy, CombatEnemyDespawnReason reason)
    {
        Enemy = enemy;
        Reason = reason;
    }
}

// 전투 런타임 한 Tick 결과입니다.
public readonly struct CombatRuntimeTickResult
{
    public int AttackCount { get; } // 이번 Tick에서 발생한 공격 횟수
    public int DefeatedEnemyCount { get; } // 이번 Tick에서 처치된 적 수
    public int ReachedGoalEnemyCount { get; } // 이번 Tick에서 새로 성채 앞에 도착한 적 수
    public int CastleDamage { get; } // 이번 Tick에서 성채 공격으로 받은 총 피해량
    public int AliveEnemyCount { get; } // Tick 종료 후 살아 있는 적 수
    public int ActiveHeroSlotCount { get; } // 공격 가능한 영웅 슬롯 수

    // Tick 결과 요약 값을 보관합니다.
    public CombatRuntimeTickResult(int attackCount, int defeatedEnemyCount, int reachedGoalEnemyCount, int castleDamage, int aliveEnemyCount, int activeHeroSlotCount)
    {
        AttackCount = attackCount;
        DefeatedEnemyCount = defeatedEnemyCount;
        ReachedGoalEnemyCount = reachedGoalEnemyCount;
        CastleDamage = castleDamage;
        AliveEnemyCount = aliveEnemyCount;
        ActiveHeroSlotCount = activeHeroSlotCount;
    }
}
