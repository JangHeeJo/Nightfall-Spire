using System.Collections.Generic;

// 재화 테이블 한 줄입니다.
// 골드, 젬, 재료처럼 수량으로 관리되는 자원의 기본 정보를 담습니다.
public sealed class CurrencyDataRow : ITableRow
{
    public int Id => CurrencyId; // DataTable 기본 키
    public int CurrencyId { get; private set; } // 재화 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public CurrencyType CurrencyType { get; private set; } // 재화 분류
    public bool IsPremium { get; private set; } // 유료/희소 재화 여부
    public long MaxAmount { get; private set; } // 최대 보유량, 0이면 제한 없음
    public string IconKey { get; private set; } // 아이콘 리소스 키

    // TSV 한 줄에서 재화 기본 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        CurrencyId = row.GetInt("CurrencyId");
        NameKey = row.GetString("NameKey");
        CurrencyType = row.GetEnum<CurrencyType>("CurrencyType");
        IsPremium = row.GetBool("IsPremium");
        MaxAmount = row.GetLong("MaxAmount");
        IconKey = row.GetString("IconKey");
    }
}

// 영웅 테이블 한 줄입니다.
// 전투 슬롯에 배치될 영웅의 기본 공격, 스킬, 태그를 담습니다.
public sealed class HeroDataRow : ITableRow
{
    public int Id => HeroId; // DataTable 기본 키
    public int HeroId { get; private set; } // 영웅 ID
    public string CharacterName { get; private set; } // 프리팹 이름과 맞춘 캐릭터 이름
    public HeroRole HeroRole { get; private set; } // 영웅 역할
    public ElementType ElementType { get; private set; } // 속성
    public Rarity Rarity { get; private set; } // 카드 프레임에 표시할 캐릭터 등급
    public List<string> HeroTagList { get; private set; } = new(); // 시너지와 필터에 쓰는 영웅 태그
    public int BaseHealth { get; private set; } // 기본 체력
    public int BaseDefense { get; private set; } // 기본 방어력
    public int BaseAttack { get; private set; } // 기본 공격력
    public float BaseAttackSpeed { get; private set; } // 기본 공격 속도
    public float BaseAttackRange { get; private set; } // 기본 공격 사거리
    public int AttackPatternId { get; private set; } // 기본 공격 패턴 ID
    public int SkillId { get; private set; } // 스킬 ID
    public TargetingType TargetingType { get; private set; } // 타겟 선택 규칙
    public UnlockConditionType UnlockConditionType { get; private set; } // 영웅 해금 조건 타입
    public int UnlockValue { get; private set; } // 해금 조건 값
    public bool IsDefaultUnlocked { get; private set; } // 시작부터 해금되는 영웅인지 여부
    public string PrefabKey { get; private set; } // 프리팹 리소스 키
    public string IconKey { get; private set; } // 아이콘 리소스 키
    public int StartLevel { get; private set; } // 최초 획득 레벨

    // TSV 한 줄에서 영웅 전투 기본값을 읽어옵니다.
    public void Load(TsvRow row)
    {
        HeroId = row.GetInt("HeroId");
        CharacterName = row.GetString("CharacterName");
        HeroRole = row.GetEnum<HeroRole>("HeroRole");
        ElementType = row.GetEnum<ElementType>("ElementType");
        Rarity = row.GetEnum<Rarity>("Rarity");
        HeroTagList = row.GetStringList("HeroTagList");
        BaseHealth = row.GetInt("BaseHealth");
        BaseDefense = row.GetInt("BaseDefense");
        BaseAttack = row.GetInt("BaseAttack");
        BaseAttackSpeed = row.GetFloat("BaseAttackSpeed");
        BaseAttackRange = row.GetFloat("BaseAttackRange");
        AttackPatternId = row.GetInt("AttackPatternId");
        SkillId = row.GetInt("SkillId");
        TargetingType = row.GetEnum<TargetingType>("TargetingType");
        UnlockConditionType = row.GetEnum<UnlockConditionType>("UnlockConditionType");
        UnlockValue = row.GetInt("UnlockValue");
        IsDefaultUnlocked = row.GetBool("IsDefaultUnlocked");
        PrefabKey = row.GetString("PrefabKey");
        IconKey = row.GetString("IconKey");
        StartLevel = row.GetInt("StartLevel");
    }
}

// Static UI 버튼이나 기능 단위 해금 조건을 담습니다.
public sealed class FeatureUnlockDataRow : ITableRow
{
    public int Id => FeatureId; // DataTable 기본 키
    public int FeatureId { get; private set; } // 기능 해금 ID
    public string FeatureKey { get; private set; } // 버튼 이름이나 기능 키
    public string DisplayName { get; private set; } // 기획 확인용 표시 이름
    public UnlockConditionType UnlockConditionType { get; private set; } // 해금 조건 타입
    public int UnlockValue { get; private set; } // 해금 조건 값
    public bool IsDefaultUnlocked { get; private set; } // 시작부터 해금되는지 여부

    // TSV 한 줄에서 기능 해금 조건을 읽어옵니다.
    public void Load(TsvRow row)
    {
        FeatureId = row.GetInt("FeatureId");
        FeatureKey = row.GetString("FeatureKey");
        DisplayName = row.GetString("DisplayName");
        UnlockConditionType = row.GetEnum<UnlockConditionType>("UnlockConditionType");
        UnlockValue = row.GetInt("UnlockValue");
        IsDefaultUnlocked = row.GetBool("IsDefaultUnlocked");
    }
}

// 전투 슬롯 테이블 한 줄입니다.
// 슬롯 위치, 허용 영웅 역할, 성장 그룹을 담습니다.
public sealed class CombatSlotDataRow : ITableRow
{
    public int Id => SlotId; // DataTable 기본 키
    public int SlotId { get; private set; } // 슬롯 ID
    public int SlotIndex { get; private set; } // 화면/전투 배치 순서
    public CombatSlotType SlotType { get; private set; } // 슬롯 타입
    public int UnlockFloorId { get; private set; } // 해금에 필요한 층 ID
    public List<HeroRole> AllowedHeroRoleList { get; private set; } = new(); // 배치 가능한 영웅 역할
    public int UpgradeGroupId { get; private set; } // 슬롯 성장 그룹 ID
    public int DefaultHeroId { get; private set; } // 기본 배치 영웅 ID
    public string PositionKey { get; private set; } // 배치 위치 리소스 키
    public bool IsDefaultUnlocked { get; private set; } // 기본 해금 여부

    // TSV 한 줄에서 전투 슬롯 규칙을 읽어옵니다.
    public void Load(TsvRow row)
    {
        SlotId = row.GetInt("SlotId");
        SlotIndex = row.GetInt("SlotIndex");
        SlotType = row.GetEnum<CombatSlotType>("SlotType");
        UnlockFloorId = row.GetInt("UnlockFloorId");
        AllowedHeroRoleList = row.GetEnumList<HeroRole>("AllowedHeroRoleList");
        UpgradeGroupId = row.GetInt("UpgradeGroupId");
        DefaultHeroId = row.GetInt("DefaultHeroId");
        PositionKey = row.GetString("PositionKey");
        IsDefaultUnlocked = row.GetBool("IsDefaultUnlocked");
    }
}

// 전투 슬롯 업그레이드 테이블 한 줄입니다.
// 같은 UpgradeGroupId 안에서 Level별 성장 수치와 비용을 정의합니다.
public sealed class CombatSlotUpgradeDataRow : ITableRow
{
    public int Id => UpgradeId; // DataTable 기본 키
    public int UpgradeId { get; private set; } // 업그레이드 Row ID
    public int UpgradeGroupId { get; private set; } // 슬롯 성장 그룹 ID
    public int Level { get; private set; } // 성장 레벨
    public int CostCurrencyId { get; private set; } // 비용 재화 ID
    public long CostAmount { get; private set; } // 비용 수량
    public float AttackBonusPct { get; private set; } // 공격력 보너스 %
    public float AttackSpeedBonusPct { get; private set; } // 공격속도 보너스 %
    public float RangeBonusPct { get; private set; } // 사거리 보너스 %
    public float SkillChargeBonusPct { get; private set; } // 스킬 충전 보너스 %
    public int UnlockModuleSocket { get; private set; } // 해금되는 모듈 소켓 수

    // TSV 한 줄에서 슬롯 업그레이드 비용과 성장값을 읽어옵니다.
    public void Load(TsvRow row)
    {
        UpgradeId = row.GetInt("UpgradeId");
        UpgradeGroupId = row.GetInt("UpgradeGroupId");
        Level = row.GetInt("Level");
        CostCurrencyId = row.GetInt("CostCurrencyId");
        CostAmount = row.GetLong("CostAmount");
        AttackBonusPct = row.GetFloat("AttackBonusPct");
        AttackSpeedBonusPct = row.GetFloat("AttackSpeedBonusPct");
        RangeBonusPct = row.GetFloat("RangeBonusPct");
        SkillChargeBonusPct = row.GetFloat("SkillChargeBonusPct");
        UnlockModuleSocket = row.GetInt("UnlockModuleSocket");
    }
}

// 밤 방어 세션 테이블 한 줄입니다.
// 하루의 밤 전투 하나가 어떤 웨이브, 드래프트, 보상을 쓰는지 연결합니다.
public sealed class DefenseSessionDataRow : ITableRow
{
    public int Id => SessionId; // DataTable 기본 키
    public int SessionId { get; private set; } // 방어 세션 ID
    public int DayIndex { get; private set; } // 낮 진행 번호
    public int NightIndex { get; private set; } // 밤 진행 번호
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public int WaveGroupId { get; private set; } // 웨이브 그룹 ID
    public int DraftPoolId { get; private set; } // 드래프트 풀 ID
    public int RewardGroupId { get; private set; } // 보상 그룹 ID
    public int RequiredFloorId { get; private set; } // 입장에 필요한 성채 층
    public ClearConditionType ClearConditionType { get; private set; } // 클리어 조건
    public float TimeLimitSec { get; private set; } // 제한 시간, 0이면 없음
    public int BossEnemyId { get; private set; } // 보스 적 ID
    public List<string> EnvironmentTagList { get; private set; } = new(); // 환경 태그

    // TSV 한 줄에서 밤 방어 세션 연결 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        SessionId = row.GetInt("SessionId");
        DayIndex = row.GetInt("DayIndex");
        NightIndex = row.GetInt("NightIndex");
        NameKey = row.GetString("NameKey");
        WaveGroupId = row.GetInt("WaveGroupId");
        DraftPoolId = row.GetInt("DraftPoolId");
        RewardGroupId = row.GetInt("RewardGroupId");
        RequiredFloorId = row.GetInt("RequiredFloorId");
        ClearConditionType = row.GetEnum<ClearConditionType>("ClearConditionType");
        TimeLimitSec = row.GetFloat("TimeLimitSec");
        BossEnemyId = row.GetInt("BossEnemyId");
        EnvironmentTagList = row.GetStringList("EnvironmentTagList");
    }
}

// 웨이브 그룹 테이블 한 줄입니다.
// 한 방어 세션의 전투 시간, 웨이브 수, 보스 위치를 정의합니다.
public sealed class WaveGroupDataRow : ITableRow
{
    public int Id => WaveGroupId; // DataTable 기본 키
    public int WaveGroupId { get; private set; } // 웨이브 그룹 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public int MaxWaveIndex { get; private set; } // 마지막 웨이브 번호
    public float BattleDurationSec { get; private set; } // 카드 선택 시간을 제외한 순수 전투 시간
    public int BossWaveIndex { get; private set; } // 보스 웨이브 번호
    public int BaseSpawnBudget { get; private set; } // 스폰 예산 기준값
    public int ScalingGroupId { get; private set; } // 난이도 스케일링 그룹 ID

    // TSV 한 줄에서 웨이브 그룹 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        WaveGroupId = row.GetInt("WaveGroupId");
        NameKey = row.GetString("NameKey");
        MaxWaveIndex = row.GetInt("MaxWaveIndex");
        BattleDurationSec = row.GetFloat("BattleDurationSec");
        BossWaveIndex = row.GetInt("BossWaveIndex");
        BaseSpawnBudget = row.GetInt("BaseSpawnBudget");
        ScalingGroupId = row.GetInt("ScalingGroupId");
    }
}

// 웨이브 테이블 한 줄입니다.
// 특정 웨이브에서 어떤 적을 언제 몇 마리 생성할지 정의합니다.
public sealed class WaveDataRow : ITableRow
{
    public int Id => WaveRowId; // DataTable 기본 키
    public int WaveRowId { get; private set; } // 웨이브 Row ID
    public int WaveGroupId { get; private set; } // 웨이브 그룹 ID
    public int WaveIndex { get; private set; } // 웨이브 번호
    public int EnemyId { get; private set; } // 스폰할 적 ID
    public int Count { get; private set; } // 스폰 수
    public float SpawnStartSec { get; private set; } // 웨이브 시작 후 첫 스폰 시간
    public float SpawnIntervalSec { get; private set; } // 반복 스폰 간격
    public int LaneId { get; private set; } // 라인 ID
    public bool IsBossWave { get; private set; } // 보스 웨이브 여부

    // TSV 한 줄에서 웨이브 스폰 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        WaveRowId = row.GetInt("WaveRowId");
        WaveGroupId = row.GetInt("WaveGroupId");
        WaveIndex = row.GetInt("WaveIndex");
        EnemyId = row.GetInt("EnemyId");
        Count = row.GetInt("Count");
        SpawnStartSec = row.GetFloat("SpawnStartSec");
        SpawnIntervalSec = row.GetFloat("SpawnIntervalSec");
        LaneId = row.GetInt("LaneId");
        IsBossWave = row.GetBool("IsBossWave");
    }
}

// 적 테이블 한 줄입니다.
// 적의 기본 체력, 이동, 공격, 태그와 프리팹 키를 담습니다.
public sealed class EnemyDataRow : ITableRow
{
    public int Id => EnemyId; // DataTable 기본 키
    public int EnemyId { get; private set; } // 적 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public EnemyRank EnemyRank { get; private set; } // 적 등급
    public ElementType ElementType { get; private set; } // 속성
    public int MaxHp { get; private set; } // 최대 체력
    public float MoveSpeed { get; private set; } // 이동 속도
    public int AttackPower { get; private set; } // 공격력
    public int Armor { get; private set; } // 방어력
    public List<string> AbilityTagList { get; private set; } = new(); // 적 특성 태그
    public int RewardScore { get; private set; } // 보상/점수 계산용 값
    public string PrefabKey { get; private set; } // 프리팹 리소스 키

    // TSV 한 줄에서 적 기본 전투값을 읽어옵니다.
    public void Load(TsvRow row)
    {
        EnemyId = row.GetInt("EnemyId");
        NameKey = row.GetString("NameKey");
        EnemyRank = row.GetEnum<EnemyRank>("EnemyRank");
        ElementType = row.GetEnum<ElementType>("ElementType");
        MaxHp = row.GetInt("MaxHp");
        MoveSpeed = row.GetFloat("MoveSpeed");
        AttackPower = row.GetInt("AttackPower");
        Armor = row.GetInt("Armor");
        AbilityTagList = row.GetStringList("AbilityTagList");
        RewardScore = row.GetInt("RewardScore");
        PrefabKey = row.GetString("PrefabKey");
    }
}

// 드래프트 풀 테이블 한 줄입니다.
// 카드 후보 추첨에 사용할 태그 필터, 등급 가중치, 리롤 비용을 담습니다.
public sealed class DraftPoolDataRow : ITableRow
{
    public int Id => DraftPoolId; // DataTable 기본 키
    public int DraftPoolId { get; private set; } // 드래프트 풀 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public List<string> IncludeTagList { get; private set; } = new(); // 포함 태그
    public List<string> ExcludeTagList { get; private set; } = new(); // 제외 태그
    public int GradeWeightCommon { get; private set; } // Common 가중치
    public int GradeWeightRare { get; private set; } // Rare 가중치
    public int GradeWeightEpic { get; private set; } // Epic 가중치
    public int PickCount { get; private set; } // 제시 카드 수
    public int RerollCostCurrencyId { get; private set; } // 리롤 비용 재화 ID
    public long RerollCostAmount { get; private set; } // 리롤 비용 수량

    // TSV 한 줄에서 드래프트 풀 추첨 규칙을 읽어옵니다.
    public void Load(TsvRow row)
    {
        DraftPoolId = row.GetInt("DraftPoolId");
        NameKey = row.GetString("NameKey");
        IncludeTagList = row.GetStringList("IncludeTagList");
        ExcludeTagList = row.GetStringList("ExcludeTagList");
        GradeWeightCommon = row.GetInt("GradeWeightCommon");
        GradeWeightRare = row.GetInt("GradeWeightRare");
        GradeWeightEpic = row.GetInt("GradeWeightEpic");
        PickCount = row.GetInt("PickCount");
        RerollCostCurrencyId = row.GetInt("RerollCostCurrencyId");
        RerollCostAmount = row.GetLong("RerollCostAmount");
    }
}

// 드래프트 카드 테이블 한 줄입니다.
// 전투 중 선택할 카드의 등급, 태그, 등장 가중치, 해금 조건을 담습니다.
public sealed class DraftCardDataRow : ITableRow
{
    public int Id => CardId; // DataTable 기본 키
    public int CardId { get; private set; } // 카드 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public string DescKey { get; private set; } // 로컬라이징 설명 키
    public CardGrade CardGrade { get; private set; } // 카드 등급
    public List<string> CardTagList { get; private set; } = new(); // 카드 태그
    public ElementType ElementType { get; private set; } // 속성
    public int MaxStack { get; private set; } // 최대 중첩 수
    public bool IsUnique { get; private set; } // 고유 카드 여부
    public int Weight { get; private set; } // 기본 등장 가중치
    public int RequiredUnlockId { get; private set; } // 해금 조건 ID
    public string IconKey { get; private set; } // 아이콘 리소스 키

    // TSV 한 줄에서 드래프트 카드 기본 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        CardId = row.GetInt("CardId");
        NameKey = row.GetString("NameKey");
        DescKey = row.GetString("DescKey");
        CardGrade = row.GetEnum<CardGrade>("CardGrade");
        CardTagList = row.GetStringList("CardTagList");
        ElementType = row.GetEnum<ElementType>("ElementType");
        MaxStack = row.GetInt("MaxStack");
        IsUnique = row.GetBool("IsUnique");
        Weight = row.GetInt("Weight");
        RequiredUnlockId = row.GetInt("RequiredUnlockId");
        IconKey = row.GetString("IconKey");
    }
}

// 드래프트 카드 효과 테이블 한 줄입니다.
// 카드가 선택되었거나 조건이 발동했을 때 적용할 효과 하나를 정의합니다.
public sealed class DraftCardEffectDataRow : ITableRow
{
    public int Id => EffectRowId; // DataTable 기본 키
    public int EffectRowId { get; private set; } // 효과 Row ID
    public int CardId { get; private set; } // 소속 카드 ID
    public EffectType EffectType { get; private set; } // 효과 타입
    public TargetScope TargetScope { get; private set; } // 적용 대상 범위
    public string TargetTagFilter { get; private set; } // 대상 태그 필터
    public ValueType ValueType { get; private set; } // 수치 타입
    public float Value { get; private set; } // 효과 값
    public DurationType DurationType { get; private set; } // 지속 타입
    public StackRule StackRule { get; private set; } // 중첩 규칙
    public TriggerType TriggerType { get; private set; } // 발동 조건

    // TSV 한 줄에서 카드 효과 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        EffectRowId = row.GetInt("EffectRowId");
        CardId = row.GetInt("CardId");
        EffectType = row.GetEnum<EffectType>("EffectType");
        TargetScope = row.GetEnum<TargetScope>("TargetScope");
        TargetTagFilter = row.GetString("TargetTagFilter");
        ValueType = row.GetEnum<ValueType>("ValueType");
        Value = row.GetFloat("Value");
        DurationType = row.GetEnum<DurationType>("DurationType");
        StackRule = row.GetEnum<StackRule>("StackRule");
        TriggerType = row.GetEnum<TriggerType>("TriggerType");
    }
}

// 시너지 테이블 한 줄입니다.
// 카드, 영웅, 모듈 태그 조합이 만족될 때 적용할 보너스를 정의합니다.
public sealed class SynergyDataRow : ITableRow
{
    public int Id => SynergyId; // DataTable 기본 키
    public int SynergyId { get; private set; } // 시너지 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public List<StringIntPair> RequiredCardTagList { get; private set; } = new(); // 필요한 카드 태그 조건
    public List<StringIntPair> RequiredHeroTagList { get; private set; } = new(); // 필요한 영웅 태그 조건
    public List<StringIntPair> RequiredModuleTagList { get; private set; } = new(); // 필요한 모듈 태그 조건
    public int SynergyLevel { get; private set; } // 시너지 단계
    public EffectType EffectType { get; private set; } // 효과 타입
    public float EffectValue { get; private set; } // 효과 값
    public TargetScope TargetScope { get; private set; } // 적용 대상 범위
    public string IconKey { get; private set; } // 아이콘 리소스 키

    // TSV 한 줄에서 시너지 조건과 보상을 읽어옵니다.
    public void Load(TsvRow row)
    {
        SynergyId = row.GetInt("SynergyId");
        NameKey = row.GetString("NameKey");
        RequiredCardTagList = row.GetStringIntPairList("RequiredCardTagList");
        RequiredHeroTagList = row.GetStringIntPairList("RequiredHeroTagList");
        RequiredModuleTagList = row.GetStringIntPairList("RequiredModuleTagList");
        SynergyLevel = row.GetInt("SynergyLevel");
        EffectType = row.GetEnum<EffectType>("EffectType");
        EffectValue = row.GetFloat("EffectValue");
        TargetScope = row.GetEnum<TargetScope>("TargetScope");
        IconKey = row.GetString("IconKey");
    }
}

// 스킬 테이블 한 줄입니다.
// 영웅 스킬의 발동 조건, 타겟팅, 공격 패턴, 배율을 담습니다.
public sealed class SkillDataRow : ITableRow
{
    public int Id => SkillId; // DataTable 기본 키
    public int SkillId { get; private set; } // 스킬 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public SkillType SkillType { get; private set; } // 액티브/패시브 타입
    public ElementType ElementType { get; private set; } // 속성
    public float CooldownSec { get; private set; } // 쿨다운
    public CastTriggerType CastTriggerType { get; private set; } // 발동 조건
    public TargetingType TargetingType { get; private set; } // 타겟 선택 규칙
    public int AttackPatternId { get; private set; } // 공격 패턴 ID
    public int ProjectileId { get; private set; } // 투사체 ID
    public float PowerMultiplier { get; private set; } // 위력 배율
    public string IconKey { get; private set; } // 아이콘 리소스 키

    // TSV 한 줄에서 스킬 기본 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        SkillId = row.GetInt("SkillId");
        NameKey = row.GetString("NameKey");
        SkillType = row.GetEnum<SkillType>("SkillType");
        ElementType = row.GetEnum<ElementType>("ElementType");
        CooldownSec = row.GetFloat("CooldownSec");
        CastTriggerType = row.GetEnum<CastTriggerType>("CastTriggerType");
        TargetingType = row.GetEnum<TargetingType>("TargetingType");
        AttackPatternId = row.GetInt("AttackPatternId");
        ProjectileId = row.GetInt("ProjectileId");
        PowerMultiplier = row.GetFloat("PowerMultiplier");
        IconKey = row.GetString("IconKey");
    }
}

// 공격 패턴 테이블 한 줄입니다.
// 단일, 관통, 체인, 범위 공격 같은 발사 규칙을 정의합니다.
public sealed class AttackPatternDataRow : ITableRow
{
    public int Id => AttackPatternId; // DataTable 기본 키
    public int AttackPatternId { get; private set; } // 공격 패턴 ID
    public AttackPatternType PatternType { get; private set; } // 공격 패턴 타입
    public int ProjectileId { get; private set; } // 기본 투사체 ID
    public int ProjectileCount { get; private set; } // 발사체 수
    public float SpreadAngle { get; private set; } // 산탄 각도
    public int ChainCount { get; private set; } // 체인 횟수
    public int PierceCount { get; private set; } // 관통 횟수
    public float AreaRadius { get; private set; } // 범위 반경

    // TSV 한 줄에서 공격 패턴 수치를 읽어옵니다.
    public void Load(TsvRow row)
    {
        AttackPatternId = row.GetInt("AttackPatternId");
        PatternType = row.GetEnum<AttackPatternType>("PatternType");
        ProjectileId = row.GetInt("ProjectileId");
        ProjectileCount = row.GetInt("ProjectileCount");
        SpreadAngle = row.GetFloat("SpreadAngle");
        ChainCount = row.GetInt("ChainCount");
        PierceCount = row.GetInt("PierceCount");
        AreaRadius = row.GetFloat("AreaRadius");
    }
}

// 투사체 테이블 한 줄입니다.
// 전투에서 발사되는 투사체의 이동 방식과 시각 키를 담습니다.
public sealed class ProjectileDataRow : ITableRow
{
    public int Id => ProjectileId; // DataTable 기본 키
    public int ProjectileId { get; private set; } // 투사체 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public ProjectileType ProjectileType { get; private set; } // 투사체 타입
    public float Speed { get; private set; } // 이동 속도
    public float HitRadius { get; private set; } // 피격 판정 반경
    public float LifeTimeSec { get; private set; } // 수명
    public string EffectKey { get; private set; } // 피격 이펙트 키
    public string PrefabKey { get; private set; } // 프리팹 리소스 키

    // TSV 한 줄에서 투사체 기본값을 읽어옵니다.
    public void Load(TsvRow row)
    {
        ProjectileId = row.GetInt("ProjectileId");
        NameKey = row.GetString("NameKey");
        ProjectileType = row.GetEnum<ProjectileType>("ProjectileType");
        Speed = row.GetFloat("Speed");
        HitRadius = row.GetFloat("HitRadius");
        LifeTimeSec = row.GetFloat("LifeTimeSec");
        EffectKey = row.GetString("EffectKey");
        PrefabKey = row.GetString("PrefabKey");
    }
}

// 보상 테이블 한 줄입니다.
// 방어 세션 보상 그룹 안의 개별 보상 항목을 정의합니다.
public sealed class RewardDataRow : ITableRow
{
    public int Id => RewardRowId; // DataTable 기본 키
    public int RewardRowId { get; private set; } // 보상 Row ID
    public int RewardGroupId { get; private set; } // 보상 그룹 ID
    public RewardItemType RewardItemType { get; private set; } // 보상 항목 타입
    public int RewardItemId { get; private set; } // 보상 항목 ID
    public int Amount { get; private set; } // 지급 수량
    public int ChancePermille { get; private set; } // 천분율 확률
    public bool FirstClearOnly { get; private set; } // 최초 클리어 전용 여부
    public int PreviewOrder { get; private set; } // UI 미리보기 정렬 순서

    // TSV 한 줄에서 보상 항목 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        RewardRowId = row.GetInt("RewardRowId");
        RewardGroupId = row.GetInt("RewardGroupId");
        RewardItemType = row.GetEnum<RewardItemType>("RewardItemType");
        RewardItemId = row.GetInt("RewardItemId");
        Amount = row.GetInt("Amount");
        ChancePermille = row.GetInt("ChancePermille");
        FirstClearOnly = row.GetBool("FirstClearOnly");
        PreviewOrder = row.GetInt("PreviewOrder");
    }
}

// 재료 테이블 한 줄입니다.
// 채굴, 제작, 보상에서 쓰이는 재료의 기본 정보를 담습니다.
public sealed class MaterialDataRow : ITableRow
{
    public int Id => MaterialId; // DataTable 기본 키
    public int MaterialId { get; private set; } // 재료 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public MaterialType MaterialType { get; private set; } // 재료 타입
    public Rarity Rarity { get; private set; } // 희귀도
    public int MaxStack { get; private set; } // 최대 스택
    public string IconKey { get; private set; } // 아이콘 리소스 키

    // TSV 한 줄에서 재료 기본 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        MaterialId = row.GetInt("MaterialId");
        NameKey = row.GetString("NameKey");
        MaterialType = row.GetEnum<MaterialType>("MaterialType");
        Rarity = row.GetEnum<Rarity>("Rarity");
        MaxStack = row.GetInt("MaxStack");
        IconKey = row.GetString("IconKey");
    }
}

// 건물 모듈 테이블 한 줄입니다.
// 성채 층에 설치되는 시설과 그 효과를 정의합니다.
public sealed class BuildingModuleDataRow : ITableRow
{
    public int Id => ModuleId; // DataTable 기본 키
    public int ModuleId { get; private set; } // 모듈 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public BuildingModuleType ModuleType { get; private set; } // 모듈 타입
    public int FloorId { get; private set; } // 설치 층 ID
    public int RequiredFloorId { get; private set; } // 해금에 필요한 층 ID
    public EffectType EffectType { get; private set; } // 효과 타입
    public float EffectValue { get; private set; } // 효과 값
    public TargetScope TargetScope { get; private set; } // 적용 대상 범위
    public int UpgradeGroupId { get; private set; } // 성장 그룹 ID
    public List<string> ModuleTagList { get; private set; } = new(); // 모듈 태그
    public string PrefabKey { get; private set; } // 프리팹 리소스 키

    // TSV 한 줄에서 건물 모듈 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        ModuleId = row.GetInt("ModuleId");
        NameKey = row.GetString("NameKey");
        ModuleType = row.GetEnum<BuildingModuleType>("ModuleType");
        FloorId = row.GetInt("FloorId");
        RequiredFloorId = row.GetInt("RequiredFloorId");
        EffectType = row.GetEnum<EffectType>("EffectType");
        EffectValue = row.GetFloat("EffectValue");
        TargetScope = row.GetEnum<TargetScope>("TargetScope");
        UpgradeGroupId = row.GetInt("UpgradeGroupId");
        ModuleTagList = row.GetStringList("ModuleTagList");
        PrefabKey = row.GetString("PrefabKey");
    }
}

// 성채 층 테이블 한 줄입니다.
// 층 해금 비용, 슬롯 수, 새 기능 해금 연결을 담습니다.
public sealed class CitadelFloorDataRow : ITableRow
{
    public int Id => FloorId; // DataTable 기본 키
    public int FloorId { get; private set; } // 층 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public int RequiredSessionId { get; private set; } // 해금에 필요한 클리어 세션 ID
    public int UnlockCostCurrencyId { get; private set; } // 해금 비용 재화 ID
    public long UnlockCostAmount { get; private set; } // 해금 비용 수량
    public int ModuleSlotCount { get; private set; } // 모듈 슬롯 수
    public UnlockFeatureType UnlockFeatureType { get; private set; } // 함께 해금되는 기능 타입
    public int UnlockFeatureId { get; private set; } // 함께 해금되는 기능 ID
    public string VisualKey { get; private set; } // 시각 리소스 키

    // TSV 한 줄에서 성채 층 해금 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        FloorId = row.GetInt("FloorId");
        NameKey = row.GetString("NameKey");
        RequiredSessionId = row.GetInt("RequiredSessionId");
        UnlockCostCurrencyId = row.GetInt("UnlockCostCurrencyId");
        UnlockCostAmount = row.GetLong("UnlockCostAmount");
        ModuleSlotCount = row.GetInt("ModuleSlotCount");
        UnlockFeatureType = row.GetEnum<UnlockFeatureType>("UnlockFeatureType");
        UnlockFeatureId = row.GetInt("UnlockFeatureId");
        VisualKey = row.GetString("VisualKey");
    }
}

// Spire 팝업 안에서 보여줄 시설/방/컨텐츠의 해금 조건을 담습니다.
public sealed class SpireContentDataRow : ITableRow
{
    public int Id => ContentId; // DataTable 기본 키
    public int ContentId { get; private set; } // Spire 컨텐츠 ID
    public string ContentKey { get; private set; } // UI와 기능 연결에 사용할 컨텐츠 키
    public string DisplayName { get; private set; } // 기획 확인용 표시 이름
    public int FloorId { get; private set; } // 배치되거나 연결된 성채 층
    public UnlockConditionType UnlockConditionType { get; private set; } // 해금 조건 타입
    public int UnlockValue { get; private set; } // 해금 조건 값
    public bool IsDefaultUnlocked { get; private set; } // 시작부터 해금되는지 여부
    public string PopupKey { get; private set; } // 눌렀을 때 연결할 팝업 또는 화면 키
    public string IconKey { get; private set; } // 아이콘 리소스 키

    // TSV 한 줄에서 Spire 컨텐츠 해금 조건을 읽어옵니다.
    public void Load(TsvRow row)
    {
        ContentId = row.GetInt("ContentId");
        ContentKey = row.GetString("ContentKey");
        DisplayName = row.GetString("DisplayName");
        FloorId = row.GetInt("FloorId");
        UnlockConditionType = row.GetEnum<UnlockConditionType>("UnlockConditionType");
        UnlockValue = row.GetInt("UnlockValue");
        IsDefaultUnlocked = row.GetBool("IsDefaultUnlocked");
        PopupKey = row.GetString("PopupKey");
        IconKey = row.GetString("IconKey");
    }
}

// 스파이어 업그레이드 테이블 한 줄입니다.
// 시설, 도서관, 채굴, 제작 같은 성장 항목의 레벨별 비용과 효과를 담습니다.
public sealed class SpireUpgradeDataRow : ITableRow
{
    public int Id => UpgradeId; // DataTable 기본 키
    public int UpgradeId { get; private set; } // 업그레이드 ID
    public int UpgradeGroupId { get; private set; } // 업그레이드 그룹 ID
    public int Level { get; private set; } // 업그레이드 레벨
    public UpgradeCategory UpgradeCategory { get; private set; } // 업그레이드 분류
    public int RequiredFloorId { get; private set; } // 필요한 층 ID
    public int CostCurrencyId { get; private set; } // 비용 재화 ID
    public long CostAmount { get; private set; } // 비용 수량
    public EffectType EffectType { get; private set; } // 효과 타입
    public float EffectValue { get; private set; } // 효과 값
    public int DependencyUpgradeId { get; private set; } // 선행 업그레이드 ID

    // TSV 한 줄에서 스파이어 성장 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        UpgradeId = row.GetInt("UpgradeId");
        UpgradeGroupId = row.GetInt("UpgradeGroupId");
        Level = row.GetInt("Level");
        UpgradeCategory = row.GetEnum<UpgradeCategory>("UpgradeCategory");
        RequiredFloorId = row.GetInt("RequiredFloorId");
        CostCurrencyId = row.GetInt("CostCurrencyId");
        CostAmount = row.GetLong("CostAmount");
        EffectType = row.GetEnum<EffectType>("EffectType");
        EffectValue = row.GetFloat("EffectValue");
        DependencyUpgradeId = row.GetInt("DependencyUpgradeId");
    }
}

// 채굴 노드 테이블 한 줄입니다.
// 낮 성장 단계에서 획득 가능한 자원 노드와 생산량을 정의합니다.
public sealed class MiningNodeDataRow : ITableRow
{
    public int Id => NodeId; // DataTable 기본 키
    public int NodeId { get; private set; } // 채굴 노드 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public int FloorId { get; private set; } // 배치 층 ID
    public int MaterialId { get; private set; } // 생산 재료 ID
    public int BaseYield { get; private set; } // 기본 생산량
    public float RespawnSec { get; private set; } // 재생성 시간
    public int RequiredUpgradeId { get; private set; } // 필요한 업그레이드 ID
    public int NodeTier { get; private set; } // 노드 티어
    public string PrefabKey { get; private set; } // 프리팹 리소스 키

    // TSV 한 줄에서 채굴 노드 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        NodeId = row.GetInt("NodeId");
        NameKey = row.GetString("NameKey");
        FloorId = row.GetInt("FloorId");
        MaterialId = row.GetInt("MaterialId");
        BaseYield = row.GetInt("BaseYield");
        RespawnSec = row.GetFloat("RespawnSec");
        RequiredUpgradeId = row.GetInt("RequiredUpgradeId");
        NodeTier = row.GetInt("NodeTier");
        PrefabKey = row.GetString("PrefabKey");
    }
}

// 제작 레시피 테이블 한 줄입니다.
// 재료를 소비해 재료나 장비를 만드는 규칙을 정의합니다.
public sealed class CraftRecipeDataRow : ITableRow
{
    public int Id => RecipeId; // DataTable 기본 키
    public int RecipeId { get; private set; } // 제작 레시피 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public RewardItemType OutputType { get; private set; } // 결과물 타입
    public int OutputId { get; private set; } // 결과물 ID
    public int OutputAmount { get; private set; } // 결과물 수량
    public List<IntPair> InputMaterialList { get; private set; } = new(); // 입력 재료 ID/수량 목록
    public float CraftTimeSec { get; private set; } // 제작 시간
    public int RequiredFloorId { get; private set; } // 필요한 층 ID
    public int RequiredModuleId { get; private set; } // 필요한 모듈 ID

    // TSV 한 줄에서 제작 레시피를 읽어옵니다.
    public void Load(TsvRow row)
    {
        RecipeId = row.GetInt("RecipeId");
        NameKey = row.GetString("NameKey");
        OutputType = row.GetEnum<RewardItemType>("OutputType");
        OutputId = row.GetInt("OutputId");
        OutputAmount = row.GetInt("OutputAmount");
        InputMaterialList = row.GetIntPairList("InputMaterialList");
        CraftTimeSec = row.GetFloat("CraftTimeSec");
        RequiredFloorId = row.GetInt("RequiredFloorId");
        RequiredModuleId = row.GetInt("RequiredModuleId");
    }
}

// 장비 테이블 한 줄입니다.
// 영웅, 슬롯, 계정에 붙는 장비 효과를 정의합니다.
public sealed class GearDataRow : ITableRow
{
    public int Id => GearId; // DataTable 기본 키
    public int GearId { get; private set; } // 장비 ID
    public string NameKey { get; private set; } // 로컬라이징 이름 키
    public GearType GearType { get; private set; } // 장비 타입
    public EquipScope EquipScope { get; private set; } // 장착 범위
    public Rarity Rarity { get; private set; } // 희귀도
    public EffectType EffectType { get; private set; } // 효과 타입
    public float EffectValue { get; private set; } // 효과 값
    public int CraftRecipeId { get; private set; } // 제작 레시피 ID
    public string IconKey { get; private set; } // 아이콘 리소스 키

    // TSV 한 줄에서 장비 기본 정보를 읽어옵니다.
    public void Load(TsvRow row)
    {
        GearId = row.GetInt("GearId");
        NameKey = row.GetString("NameKey");
        GearType = row.GetEnum<GearType>("GearType");
        EquipScope = row.GetEnum<EquipScope>("EquipScope");
        Rarity = row.GetEnum<Rarity>("Rarity");
        EffectType = row.GetEnum<EffectType>("EffectType");
        EffectValue = row.GetFloat("EffectValue");
        CraftRecipeId = row.GetInt("CraftRecipeId");
        IconKey = row.GetString("IconKey");
    }
}
