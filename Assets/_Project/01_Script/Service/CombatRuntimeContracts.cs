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

// 전투 슬롯 하나가 런타임에서 공격자로 쓰기 위해 필요한 상태입니다.
public sealed class CombatHeroSlotRuntimeState
{
    public int SlotIndex { get; } // 전투 슬롯 번호
    public int HeroId { get; } // 슬롯에 배치된 영웅 ID
    public CombatSlotType SlotType { get; } // 전투 슬롯 타입
    public HeroRole HeroRole { get; } // 영웅 역할
    public ElementType ElementType { get; } // 공격 속성
    public TargetingType TargetingType { get; } // 타겟 선택 규칙
    public int MaxHealth { get; } // 영웅 기본 체력
    public int Defense { get; } // 영웅 기본 방어력
    public int AttackPower { get; } // 슬롯 성장 보정이 반영된 공격력
    public float AttackSpeed { get; } // 슬롯 성장 보정이 반영된 초당 공격 횟수
    public float AttackTimer { get; private set; } // 다음 공격까지 누적된 시간

    public bool CanAttack => AttackPower > 0 && AttackSpeed > 0f; // 공격 가능한 슬롯인지 여부

    // 영웅 슬롯의 전투 계산용 값을 보관합니다.
    public CombatHeroSlotRuntimeState(int slotIndex, int heroId, CombatSlotType slotType, HeroRole heroRole, ElementType elementType, TargetingType targetingType, int maxHealth, int defense, int attackPower, float attackSpeed)
    {
        SlotIndex = slotIndex;
        HeroId = heroId;
        SlotType = slotType;
        HeroRole = heroRole;
        ElementType = elementType;
        TargetingType = targetingType;
        MaxHealth = maxHealth;
        Defense = defense;
        AttackPower = attackPower;
        AttackSpeed = attackSpeed;
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
    public float MoveSpeed { get; } // 이동 속도
    public float PathProgress { get; private set; } // 방어 목표 지점까지의 진행도
    public bool IsAlive => CurrentHp > 0; // 적 생존 여부

    // 적 런타임 상태를 만듭니다.
    public CombatEnemyRuntimeState(int runtimeId, int enemyId, EnemyRank enemyRank, ElementType elementType, int laneId, int spawnOrder, int maxHp, int armor, float moveSpeed)
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
        MoveSpeed = moveSpeed;
    }

    // 적 이동 진행도를 올립니다.
    public void Advance(float deltaSeconds, float progressPerSpeed)
    {
        if (deltaSeconds <= 0f || progressPerSpeed <= 0f || !IsAlive)
            return;

        PathProgress += MoveSpeed * progressPerSpeed * deltaSeconds;

        if (PathProgress > 1f)
            PathProgress = 1f;
    }

    // 피해를 적용하고 실제 감소한 체력을 반환합니다.
    public int ApplyDamage(int damage)
    {
        if (damage <= 0 || !IsAlive)
            return 0;

        int applied = damage > CurrentHp ? CurrentHp : damage;
        CurrentHp -= applied;
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

// 전투 런타임 한 Tick 결과입니다.
public readonly struct CombatRuntimeTickResult
{
    public int AttackCount { get; } // 이번 Tick에서 발생한 공격 횟수
    public int DefeatedEnemyCount { get; } // 이번 Tick에서 처치된 적 수
    public int AliveEnemyCount { get; } // Tick 종료 후 살아 있는 적 수
    public int ActiveHeroSlotCount { get; } // 공격 가능한 영웅 슬롯 수

    // Tick 결과 요약 값을 보관합니다.
    public CombatRuntimeTickResult(int attackCount, int defeatedEnemyCount, int aliveEnemyCount, int activeHeroSlotCount)
    {
        AttackCount = attackCount;
        DefeatedEnemyCount = defeatedEnemyCount;
        AliveEnemyCount = aliveEnemyCount;
        ActiveHeroSlotCount = activeHeroSlotCount;
    }
}
