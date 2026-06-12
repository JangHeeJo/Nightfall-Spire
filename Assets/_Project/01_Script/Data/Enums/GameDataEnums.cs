// Nightfall Spire 데이터 테이블에서 사용하는 enum 모음입니다.
// 작은 컨텐츠 enum은 파일을 늘리지 않고 이 파일에 모아두며, 각 값의 테이블 의미를 주석으로 고정합니다.

// 재화/자원 분류입니다.
public enum CurrencyType
{
    Soft, // 플레이 중 반복 획득하는 기본 재화
    Premium, // 유료/희귀 재화
    Material, // 제작과 업그레이드에 쓰는 재료성 재화
    Meta // 계정 성장이나 장기 진행에 쓰는 메타 재화
}

// 영웅의 전투 역할입니다.
public enum HeroRole
{
    Melee, // 성채 가까이에서 싸우는 근접 영웅
    Ranged // 원거리 공격으로 후방에서 싸우는 영웅
}

// 공격, 스킬, 적 약점에 쓰는 속성입니다.
public enum ElementType
{
    None, // 속성이 없거나 속성 계산을 하지 않는 값
    Fire, // 화염 속성
    Ice, // 빙결 속성
    Lightning, // 번개 속성
    Dark, // 어둠 속성
    Physical // 물리 속성
}

// 전투 슬롯의 배치/기능 타입입니다.
public enum CombatSlotType
{
    Front, // 전방 배치 슬롯
    Back, // 후방 배치 슬롯
    Tower, // 타워형 방어 슬롯
    Support // 보조 기능 슬롯
}

// 성채 또는 스파이어에 설치되는 성장 모듈 타입입니다.
public enum BuildingModuleType
{
    DefenseTower, // 방어 타워 모듈
    Library, // 드래프트와 마법 성장에 관여하는 도서관 모듈
    Mining, // 방치/채굴 보상 모듈
    Workshop, // 제작과 장비 성장 모듈
    Utility // 기타 편의 또는 전역 보정 모듈
}

// 해금되는 기능의 종류입니다.
public enum UnlockFeatureType
{
    None, // 해금 기능이 없는 값
    CombatSlot, // 전투 슬롯 해금
    BuildingModule, // 건물 모듈 해금
    DraftPool, // 드래프트 카드 풀 해금
    MiningNode, // 채굴 노드 해금
    CraftRecipe // 제작 레시피 해금
}

// 컨텐츠 해금 조건을 판정하는 기준입니다.
public enum UnlockConditionType
{
    Default, // 시작부터 해금
    CitadelFloor, // 성채 층 수 기준 해금
    ClearedNight, // 클리어한 밤 세션 ID 기준 해금
    SpireLevel, // 스파이어 레벨 기준 해금
    MagicLibrary // 마법 도서관 해금 여부 기준 해금
}

// 업그레이드 테이블의 성장 분류입니다.
public enum UpgradeCategory
{
    Slot, // 전투 슬롯 업그레이드
    Module, // 건물 모듈 업그레이드
    Library, // 도서관/드래프트 관련 업그레이드
    Mining, // 채굴 관련 업그레이드
    Crafting, // 제작 관련 업그레이드
    Global // 계정 또는 전체 전투 전역 업그레이드
}

// 밤 방어 세션 클리어 조건입니다.
public enum ClearConditionType
{
    ClearAllWaves, // 모든 웨이브 처치
    SurviveTime, // 제한 시간 생존
    DefeatBoss // 보스 처치
}

// 적 등급입니다.
public enum EnemyRank
{
    Normal, // 일반 적
    Elite, // 강화 적
    Boss // 보스 적
}

// 드래프트 카드 등급입니다.
public enum CardGrade
{
    Common, // 일반 카드
    Rare, // 희귀 카드
    Epic // 에픽 카드
}

// 효과 테이블에서 적용할 수 있는 효과 종류입니다.
public enum EffectType
{
    AttackPercent, // 공격력 비율 증가
    AttackSpeedPercent, // 공격 속도 비율 증가
    RangePercent, // 사거리 비율 증가
    ProjectileCountAdd, // 투사체 수 추가
    PierceCountAdd, // 관통 횟수 추가
    ChainLightningCount, // 연쇄 번개 대상 수 증가
    OnHitExplosionDamage, // 적중 시 폭발 피해
    BurnDamagePercent, // 화상 피해 비율
    DraftRareWeight, // 희귀 카드 등장 가중치 보정
    MiningYieldPercent, // 채굴 보상 비율 증가
    CraftSpeedPercent, // 제작 속도 비율 증가
    MaxHpPercent, // 최대 체력 비율 증가
    SkillChargePercent // 스킬 충전 속도 비율 증가
}

// 효과가 적용되는 대상 범위입니다.
public enum TargetScope
{
    AllSlots, // 모든 전투 슬롯
    SlotByType, // 특정 슬롯 타입
    HeroByRole, // 특정 영웅 역할
    HeroByTag, // 특정 영웅 태그
    Enemy, // 적 대상
    DraftPool, // 드래프트 카드 풀
    Mining, // 채굴 시스템
    Crafting, // 제작 시스템
    Account // 계정 전역
}

// 효과 수치 해석 방식입니다.
public enum ValueType
{
    Flat, // 고정값
    Percent // 비율값
}

// 효과 지속 시간 분류입니다.
public enum DurationType
{
    Instant, // 즉시 적용
    Battle, // 현재 전투 동안 적용
    Permanent // 영구 적용
}

// 같은 효과가 중첩될 때의 계산 규칙입니다.
public enum StackRule
{
    None, // 중첩하지 않음
    Additive, // 합산 중첩
    Multiplicative, // 곱연산 중첩
    Replace // 기존 효과 교체
}

// 효과 발동 조건입니다.
public enum TriggerType
{
    OnPick, // 드래프트 카드 선택 시
    OnHit, // 공격 적중 시
    OnKill, // 적 처치 시
    OnWaveStart, // 웨이브 시작 시
    OnWaveClear, // 웨이브 클리어 시
    OnCooldown // 쿨타임마다
}

// 스킬 시전 발동 조건입니다.
public enum CastTriggerType
{
    OnCooldown, // 쿨타임마다 자동 시전
    OnHit, // 적중 시 시전
    OnKill, // 처치 시 시전
    OnWaveStart, // 웨이브 시작 시 시전
    OnWaveClear // 웨이브 클리어 시 시전
}

// 대상 선택 방식입니다.
public enum TargetingType
{
    Nearest, // 가장 가까운 대상
    Farthest, // 가장 먼 대상
    LowestHp, // 체력이 가장 낮은 대상
    HighestHp, // 체력이 가장 높은 대상
    Random // 무작위 대상
}

// 공격 패턴 종류입니다.
public enum AttackPatternType
{
    Single, // 단일 대상 공격
    Spread, // 부채꼴/분산 공격
    Pierce, // 관통 공격
    Chain, // 연쇄 공격
    Area // 범위 공격
}

// 투사체 이동 방식입니다.
public enum ProjectileType
{
    Straight, // 직선 투사체
    Homing, // 유도 투사체
    Instant, // 즉발 판정
    Arc // 포물선 투사체
}

// 재료 아이템 분류입니다.
public enum MaterialType
{
    Ore, // 광석
    Crystal, // 결정
    Component, // 부품
    Essence // 정수
}

// 장비 부위 타입입니다.
public enum GearType
{
    Weapon, // 무기
    Armor, // 방어구
    Ring, // 반지
    Charm, // 부적
    Tool // 도구
}

// 장비 또는 효과가 장착되는 범위입니다.
public enum EquipScope
{
    Hero, // 영웅 단위 장착
    CombatSlot, // 전투 슬롯 단위 장착
    Account // 계정 전역 장착
}

// 보상 항목 타입입니다.
public enum RewardItemType
{
    Currency, // 재화
    Material, // 재료
    Gear, // 장비
    Hero, // 영웅
    Module // 건물 모듈
}

// 일반 희귀도입니다.
public enum Rarity
{
    Common, // 일반
    Rare, // 희귀
    Epic, // 에픽
    Legendary // 전설
}

// 스킬 작동 방식입니다.
public enum SkillType
{
    Active, // 직접 또는 조건에 따라 발동되는 액티브 스킬
    Passive // 항상 적용되는 패시브 스킬
}
