using System.Collections.Generic;
using NUnit.Framework;

// 드래프트 카드 효과가 전투 런타임 보정값으로 변환되는지 검증합니다.
public sealed class DraftEffectResolverTests
{
    // 공격력 카드 효과를 적용한 뒤 전투 슬롯을 다시 구성하면 공격력에 반영되어야 합니다.
    [Test]
    public void ApplyCardEffects_AddsAttackModifierToCombatRuntime()
    {
        FakeDraftEffectDataSource effectDataSource = FakeDraftEffectDataSource.CreateDefault();
        CombatRuntimeModifierSet modifierSet = new CombatRuntimeModifierSet();
        DraftEffectResolver resolver = new DraftEffectResolver(effectDataSource, modifierSet);
        CombatRuntimeController combatController = new CombatRuntimeController(
            FakeCombatDataSource.CreateDefault(),
            new CombatSlotProgress(SaveData.CreateDefault()),
            modifierSet: modifierSet);

        DraftEffectApplyResult applyResult = resolver.ApplyCardEffects(3001);
        CombatRuntimeFailureReason rebuildResult = combatController.RebuildHeroSlots();

        Assert.That(applyResult.IsSuccess, Is.True);
        Assert.That(applyResult.AppliedEffectCount, Is.EqualTo(1));
        Assert.That(rebuildResult, Is.EqualTo(CombatRuntimeFailureReason.None));
        Assert.That(combatController.HeroSlots[0].AttackPower, Is.EqualTo(30));
    }

    // 카드에 연결된 효과가 없으면 실패 결과를 반환해야 합니다.
    [Test]
    public void ApplyCardEffects_FailsWhenCardHasNoEffects()
    {
        FakeDraftEffectDataSource effectDataSource = FakeDraftEffectDataSource.CreateDefault();
        DraftEffectResolver resolver = new DraftEffectResolver(effectDataSource, new CombatRuntimeModifierSet());

        DraftEffectApplyResult result = resolver.ApplyCardEffects(9999);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(DraftFailureReason.CardEffectNotFound));
    }

    // 드래프트 효과 테스트에 필요한 최소 카드 효과 데이터를 제공합니다.
    private sealed class FakeDraftEffectDataSource : IDraftEffectDataSource
    {
        private DataTable<DraftCardEffectDataRow> effects;

        // 카드 효과 테스트 데이터를 만듭니다.
        public static FakeDraftEffectDataSource CreateDefault()
        {
            return new FakeDraftEffectDataSource
            {
                effects = TsvParser.Parse<DraftCardEffectDataRow>("DraftCardEffectData", DraftCardEffectTsv)
            };
        }

        public IReadOnlyList<DraftCardEffectDataRow> GetDraftCardEffects(int cardId)
        {
            List<DraftCardEffectDataRow> rows = new List<DraftCardEffectDataRow>();

            for (int i = 0; i < effects.Rows.Count; i++)
            {
                DraftCardEffectDataRow row = effects.Rows[i];

                if (row.CardId == cardId)
                    rows.Add(row);
            }

            return rows;
        }

        private const string DraftCardEffectTsv =
            "EffectRowId\tCardId\tEffectType\tTargetScope\tTargetTagFilter\tValueType\tValue\tDurationType\tStackRule\tTriggerType\n" +
            "400001\t3001\tAttackPercent\tAllSlots\t\tPercent\t50\tBattle\tAdditive\tOnPick\n";
    }

    // 전투 런타임 테스트에 필요한 최소 전투 데이터를 제공합니다.
    private sealed class FakeCombatDataSource : ICombatDataSource
    {
        private DataTable<HeroDataRow> heroes;
        private DataTable<EnemyDataRow> enemies;
        private DataTable<CombatSlotDataRow> combatSlots;
        private DataTable<CombatSlotUpgradeDataRow> combatSlotUpgrades;

        // 영웅, 적, 슬롯, 슬롯 성장 테스트 데이터를 만듭니다.
        public static FakeCombatDataSource CreateDefault()
        {
            return new FakeCombatDataSource
            {
                heroes = TsvParser.Parse<HeroDataRow>("HeroData", HeroTsv),
                enemies = TsvParser.Parse<EnemyDataRow>("EnemyData", EnemyTsv),
                combatSlots = TsvParser.Parse<CombatSlotDataRow>("CombatSlotData", CombatSlotTsv),
                combatSlotUpgrades = TsvParser.Parse<CombatSlotUpgradeDataRow>("CombatSlotUpgradeData", CombatSlotUpgradeTsv)
            };
        }

        public bool TryGetHero(int heroId, out HeroDataRow row)
        {
            return heroes.TryGet(heroId, out row);
        }

        public bool TryGetEnemy(int enemyId, out EnemyDataRow row)
        {
            return enemies.TryGet(enemyId, out row);
        }

        public bool TryGetCombatSlotByIndex(int slotIndex, out CombatSlotDataRow row)
        {
            row = null;

            for (int i = 0; i < combatSlots.Rows.Count; i++)
            {
                CombatSlotDataRow candidate = combatSlots.Rows[i];

                if (candidate.SlotIndex != slotIndex)
                    continue;

                row = candidate;
                return true;
            }

            return false;
        }

        public bool TryGetCombatSlotUpgrade(int upgradeGroupId, int level, out CombatSlotUpgradeDataRow row)
        {
            row = null;

            for (int i = 0; i < combatSlotUpgrades.Rows.Count; i++)
            {
                CombatSlotUpgradeDataRow candidate = combatSlotUpgrades.Rows[i];

                if (candidate.UpgradeGroupId != upgradeGroupId || candidate.Level != level)
                    continue;

                row = candidate;
                return true;
            }

            return false;
        }

        private const string HeroTsv =
            "HeroId\tCharacterName\tHeroRole\tElementType\tRarity\tHeroTagList\tBaseHealth\tBaseDefense\tBaseAttack\tBaseAttackSpeed\tBaseAttackRange\tAttackPatternId\tSkillId\tTargetingType\tUnlockConditionType\tUnlockValue\tIsDefaultUnlocked\tPrefabKey\tIconKey\tStartLevel\n" +
            "1\tHero_Guardian\tMelee\tPhysical\tCommon\tStarter|Slot\t150\t10\t20\t1.0\t2.0\t1\t0\tNearest\tDefault\t0\ttrue\tHero_Guardian\tIcon_Hero_Guardian\t1\n";

        private const string EnemyTsv =
            "EnemyId\tNameKey\tEnemyRank\tElementType\tMaxHp\tMoveSpeed\tAttackPower\tArmor\tAbilityTagList\tRewardScore\tPrefabKey\n" +
            "1101\tenemy_shadow\tNormal\tDark\t25\t1.0\t3\t5\tWalker\t10\tEnemy_Shadow\n";

        private const string CombatSlotTsv =
            "SlotId\tSlotIndex\tSlotType\tFloorId\tFloorSlotIndex\tUnlockFloorId\tAllowedHeroRoleList\tUpgradeGroupId\tDefaultHeroId\tPositionKey\tLocalPositionX\tLocalPositionY\tMinTargetProgress\tMaxTargetProgress\tIsDefaultUnlocked\n" +
            "1001\t0\tFront\t1\t0\t1\tMelee|Ranged\t2001\t1\tSlot_Front_01\t-0.5\t-1.5\t0.00\t1.00\tTRUE\n";

        private const string CombatSlotUpgradeTsv =
            "UpgradeId\tUpgradeGroupId\tLevel\tCostCurrencyId\tCostAmount\tAttackBonusPct\tAttackSpeedBonusPct\tRangeBonusPct\tSkillChargeBonusPct\tUnlockModuleSocket\n" +
            "200101\t2001\t1\t1\t0\t0\t0\t0\t0\t0\n";
    }
}
