// Nightfall Spire MVP 데이터 테이블에서 사용하는 enum 모음입니다.
// 초기에는 파일 수를 줄이기 위해 하나의 파일로 관리하고, 커지면 enum별 파일로 분리합니다.

public enum CurrencyType { Soft, Premium, Material, Meta }
public enum HeroRole { Dealer, Support, Tank }
public enum ElementType { None, Fire, Ice, Lightning, Dark, Physical }
public enum CombatSlotType { Front, Back, Tower, Support }
public enum BuildingModuleType { DefenseTower, Library, Mining, Workshop, Utility }
public enum UnlockFeatureType { None, CombatSlot, BuildingModule, DraftPool, MiningNode, CraftRecipe }
public enum UpgradeCategory { Slot, Module, Library, Mining, Crafting, Global }
public enum ClearConditionType { ClearAllWaves, SurviveTime, DefeatBoss }
public enum EnemyRank { Normal, Elite, Boss }
public enum CardGrade { Common, Rare, Epic }
public enum EffectType { AttackPercent, AttackSpeedPercent, RangePercent, ProjectileCountAdd, PierceCountAdd, ChainLightningCount, OnHitExplosionDamage, BurnDamagePercent, DraftRareWeight, MiningYieldPercent, CraftSpeedPercent, MaxHpPercent, SkillChargePercent }
public enum TargetScope { AllSlots, SlotByType, HeroByRole, HeroByTag, Enemy, DraftPool, Mining, Crafting, Account }
public enum ValueType { Flat, Percent }
public enum DurationType { Instant, Battle, Permanent }
public enum StackRule { None, Additive, Multiplicative, Replace }
public enum TriggerType { OnPick, OnHit, OnKill, OnWaveStart, OnWaveClear, OnCooldown }
public enum CastTriggerType { OnCooldown, OnHit, OnKill, OnWaveStart, OnWaveClear }
public enum TargetingType { Nearest, Farthest, LowestHp, HighestHp, Random }
public enum AttackPatternType { Single, Spread, Pierce, Chain, Area }
public enum ProjectileType { Straight, Homing, Instant, Arc }
public enum MaterialType { Ore, Crystal, Component, Essence }
public enum GearType { Weapon, Armor, Ring, Charm, Tool }
public enum EquipScope { Hero, CombatSlot, Account }
public enum RewardItemType { Currency, Material, Gear, Hero, Module }
public enum Rarity { Common, Rare, Epic, Legendary }
public enum SkillType { Active, Passive }
