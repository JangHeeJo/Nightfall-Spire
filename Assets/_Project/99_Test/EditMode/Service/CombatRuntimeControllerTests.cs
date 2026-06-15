using System.Collections.Generic;
using NUnit.Framework;

// 전투 런타임 컨트롤러가 슬롯 공격, 타겟 선택, 피해 적용을 처리하는지 검증합니다.
public sealed class CombatRuntimeControllerTests
{
    // 해금된 슬롯과 배치 영웅이 있으면 전투 공격자 상태가 구성되어야 합니다.
    [Test]
    public void RebuildHeroSlots_CreatesUnlockedHeroSlots()
    {
        SaveData saveData = SaveData.CreateDefault();
        CombatRuntimeController controller = new CombatRuntimeController(
            FakeCombatDataSource.CreateDefault(),
            new CombatSlotProgress(saveData));

        CombatRuntimeFailureReason result = controller.RebuildHeroSlots();

        Assert.That(result, Is.EqualTo(CombatRuntimeFailureReason.None));
        Assert.That(controller.HeroSlots.Count, Is.EqualTo(1));
        Assert.That(controller.HeroSlots[0].SlotIndex, Is.EqualTo(0));
        Assert.That(controller.HeroSlots[0].HeroId, Is.EqualTo(1001));
        Assert.That(controller.HeroSlots[0].MaxHealth, Is.EqualTo(150));
        Assert.That(controller.HeroSlots[0].Defense, Is.EqualTo(10));
        Assert.That(controller.HeroSlots[0].AttackPower, Is.EqualTo(20));
    }

    // 시간이 공격 주기만큼 흐르면 슬롯이 적을 공격하고 체력을 감소시켜야 합니다.
    [Test]
    public void Tick_AppliesSlotAttackDamage()
    {
        SaveData saveData = SaveData.CreateDefault();
        CombatRuntimeController controller = CreateReadyController(saveData);

        controller.SpawnEnemy(CreateSpawnRequest(1101), out CombatEnemyRuntimeState enemy);
        CombatRuntimeTickResult result = controller.Tick(1f);

        Assert.That(result.AttackCount, Is.EqualTo(1));
        Assert.That(result.DefeatedEnemyCount, Is.EqualTo(0));
        Assert.That(result.AliveEnemyCount, Is.EqualTo(1));
        Assert.That(enemy.CurrentHp, Is.EqualTo(10));
        Assert.That(controller.LastDamageResults.Count, Is.EqualTo(1));
        Assert.That(controller.LastDamageResults[0].FinalDamage, Is.EqualTo(15));
    }

    // 적 체력이 0이 되면 처치 수가 올라가고 런타임 적 목록에서 제거되어야 합니다.
    [Test]
    public void Tick_RemovesDefeatedEnemy()
    {
        SaveData saveData = SaveData.CreateDefault();
        CombatRuntimeController controller = CreateReadyController(saveData);

        controller.SpawnEnemy(CreateSpawnRequest(1101), out _);
        controller.Tick(1f);
        CombatRuntimeTickResult result = controller.Tick(1f);

        Assert.That(result.AttackCount, Is.EqualTo(1));
        Assert.That(result.DefeatedEnemyCount, Is.EqualTo(1));
        Assert.That(result.AliveEnemyCount, Is.EqualTo(0));
        Assert.That(controller.Enemies.Count, Is.EqualTo(0));
        Assert.That(controller.LastDespawnResults.Count, Is.EqualTo(1));
        Assert.That(controller.LastDespawnResults[0].Reason, Is.EqualTo(CombatEnemyDespawnReason.Defeated));
    }

    // 적이 성채 지점까지 도달하면 목록에서 제거되고 성채 피해로 변환되어야 합니다.
    [Test]
    public void Tick_ConvertsReachedEnemyToCastleDamage()
    {
        SaveData saveData = SaveData.CreateDefault();
        CombatRuntimeController controller = CreateReadyController(saveData);

        controller.SpawnEnemy(CreateSpawnRequest(1102), out _);
        CombatRuntimeTickResult result = controller.Tick(1f);

        Assert.That(result.ReachedGoalEnemyCount, Is.EqualTo(1));
        Assert.That(result.CastleDamage, Is.EqualTo(10));
        Assert.That(result.AliveEnemyCount, Is.EqualTo(0));
        Assert.That(controller.LastDespawnResults.Count, Is.EqualTo(1));
        Assert.That(controller.LastDespawnResults[0].Reason, Is.EqualTo(CombatEnemyDespawnReason.ReachedGoal));
    }

    // 슬롯 성장 보정이 있으면 공격력 계산에 반영되어야 합니다.
    [Test]
    public void RebuildHeroSlots_AppliesSlotUpgradeBonus()
    {
        SaveData saveData = SaveData.CreateDefault();
        saveData.CombatSlot.Slots[0].Level = 2;
        CombatRuntimeController controller = new CombatRuntimeController(
            FakeCombatDataSource.CreateDefault(),
            new CombatSlotProgress(saveData));

        CombatRuntimeFailureReason result = controller.RebuildHeroSlots();

        Assert.That(result, Is.EqualTo(CombatRuntimeFailureReason.None));
        Assert.That(controller.HeroSlots[0].AttackPower, Is.EqualTo(30));
    }

    // 테스트에 필요한 슬롯 구성을 끝낸 컨트롤러를 만듭니다.
    private static CombatRuntimeController CreateReadyController(SaveData saveData)
    {
        CombatRuntimeController controller = new CombatRuntimeController(
            FakeCombatDataSource.CreateDefault(),
            new CombatSlotProgress(saveData));

        CombatRuntimeFailureReason result = controller.RebuildHeroSlots();
        Assert.That(result, Is.EqualTo(CombatRuntimeFailureReason.None));
        return controller;
    }

    // 스폰 컨트롤러가 넘기는 요청과 같은 형태의 테스트용 적 스폰 요청을 만듭니다.
    private static NightDefenseSpawnRequest CreateSpawnRequest(int enemyId)
    {
        NightDefenseSpawnEvent spawnEvent = new NightDefenseSpawnEvent(810001, enemyId, 1, 1, 0f);
        return new NightDefenseSpawnRequest(1, spawnEvent, 0f);
    }

    // 전투 런타임 테스트에 필요한 최소 테이블을 제공하는 데이터 소스입니다.
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
            "1001\tHero_Guardian\tMelee\tPhysical\tCommon\tStarter|Slot\t150\t10\t20\t1.0\t2.0\t1\t0\tNearest\tDefault\t0\ttrue\tHero_Guardian\tIcon_Hero_Guardian\t1\n";

        private const string EnemyTsv =
            "EnemyId\tNameKey\tEnemyRank\tElementType\tMaxHp\tMoveSpeed\tAttackPower\tArmor\tAbilityTagList\tRewardScore\tPrefabKey\n" +
            "1101\tenemy_shadow\tNormal\tDark\t25\t1.0\t3\t5\tWalker\t10\tEnemy_Shadow\n" +
            "1102\tenemy_runner\tNormal\tDark\t999\t100.0\t10\t0\tWalker\t10\tEnemy_Runner\n";

        private const string CombatSlotTsv =
            "SlotId\tSlotIndex\tSlotType\tUnlockFloorId\tAllowedHeroRoleList\tUpgradeGroupId\tDefaultHeroId\tPositionKey\tIsDefaultUnlocked\n" +
            "1001\t0\tFront\t1\tMelee|Ranged\t2001\t1001\tSlot_Front_01\tTRUE\n";

        private const string CombatSlotUpgradeTsv =
            "UpgradeId\tUpgradeGroupId\tLevel\tCostCurrencyId\tCostAmount\tAttackBonusPct\tAttackSpeedBonusPct\tRangeBonusPct\tSkillChargeBonusPct\tUnlockModuleSocket\n" +
            "200101\t2001\t1\t1\t0\t0\t0\t0\t0\t0\n" +
            "200102\t2001\t2\t1\t10\t50\t0\t0\t0\t0\n";
    }
}
