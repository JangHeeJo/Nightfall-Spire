using System.Collections.Generic;
using NUnit.Framework;

// 핵심 컨텐츠 서비스가 Progress와 테이블 계약을 올바르게 연결하는지 검증합니다.
public sealed class GameContentServiceTests
{
    // 밤 방어 서비스는 세션/웨이브 데이터를 검증하고 Progress에 현재 세션과 웨이브를 반영해야 합니다.
    [Test]
    public void NightDefenseService_StartsSessionAndAdvancesWave()
    {
        FakeContentDataSource dataSource = FakeContentDataSource.CreateDefault();
        GameContext context = new GameContext(SaveData.CreateDefault());
        context.GameProgress.SetCurrentDefenseSession(101);
        NightDefenseSessionService service = new NightDefenseSessionService(dataSource, context);

        NightDefenseStartResult startResult = service.TryStartCurrentSession();
        NightDefenseWaveResult waveResult = service.TryAdvanceNextWave();

        Assert.That(startResult.IsSuccess, Is.True);
        Assert.That(context.NightDefenseProgress.IsDefenseActive.Value, Is.True);
        Assert.That(context.NightDefenseProgress.CurrentDefenseSessionId.Value, Is.EqualTo(101));
        Assert.That(waveResult.IsSuccess, Is.True);
        Assert.That(waveResult.WaveIndex, Is.EqualTo(1));
        Assert.That(waveResult.WavePlan.TotalSpawnCount, Is.EqualTo(12));
        Assert.That(waveResult.WavePlan.SpawnEvents[0].EnemyId, Is.EqualTo(1101));
        Assert.That(waveResult.WavePlan.SpawnEvents[0].SpawnTimeSec, Is.EqualTo(0f));
        Assert.That(waveResult.WavePlan.LastSpawnTimeSec, Is.EqualTo(6.6f).Within(0.001f));
        Assert.That(context.NightDefenseProgress.CurrentWaveIndex.Value, Is.EqualTo(1));
    }

    // 밤 방어 서비스는 필요한 성채 층을 만족하지 못하면 세션을 시작하지 않아야 합니다.
    [Test]
    public void NightDefenseService_BlocksLockedSession()
    {
        FakeContentDataSource dataSource = FakeContentDataSource.CreateDefault();
        SaveData saveData = SaveData.CreateDefault();
        saveData.Progress.CurrentDefenseSessionId = 103;
        saveData.DayCycle.CitadelFloorCount = 1;
        GameContext context = new GameContext(saveData);
        NightDefenseSessionService service = new NightDefenseSessionService(dataSource, context);

        NightDefenseStartResult result = service.TryStartCurrentSession();

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(NightDefenseFailureReason.RequiredFloorLocked));
        Assert.That(context.NightDefenseProgress.IsDefenseActive.Value, Is.False);
    }

    // 드래프트 서비스는 풀의 태그 조건과 PickCount에 맞춰 후보를 열어야 합니다.
    [Test]
    public void DraftService_OpensOfferFromPoolRules()
    {
        FakeContentDataSource dataSource = FakeContentDataSource.CreateDefault();
        DraftProgress draftProgress = new DraftProgress();
        DraftService service = new DraftService(dataSource, draftProgress);

        DraftOfferResult result = service.TryOpenOffer(7001);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.OfferedCardIds, Is.EquivalentTo(new[] { 7101, 7102, 7105 }));
        Assert.That(draftProgress.IsDraftOpen.Value, Is.True);
        Assert.That(draftProgress.OfferedCardIds, Is.EquivalentTo(result.OfferedCardIds));
    }

    // 보상 서비스는 보상 그룹에서 지급 가능한 재화 합계를 계산하고 RewardProgress에 반영해야 합니다.
    [Test]
    public void RewardService_BuildsAndStoresPendingCurrencyReward()
    {
        FakeContentDataSource dataSource = FakeContentDataSource.CreateDefault();
        RewardProgress rewardProgress = new RewardProgress();
        RewardService service = new RewardService(dataSource, rewardProgress);

        RewardGrantResult result = service.BuildReward(9001, true);
        service.SetPendingCurrencyReward(result);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Gold, Is.EqualTo(100));
        Assert.That(result.Gem, Is.EqualTo(0));
        Assert.That(result.RewardLines.Count, Is.EqualTo(2));
        Assert.That(rewardProgress.PendingGold, Is.EqualTo(100));
        Assert.That(rewardProgress.PendingGem, Is.EqualTo(0));
    }

    // 낮 성장 서비스는 클리어 조건과 비용을 검증한 뒤 다음 성채 층과 연결된 기능을 해금해야 합니다.
    [Test]
    public void DayGrowthService_UnlocksNextFloorAndFeature()
    {
        FakeContentDataSource dataSource = FakeContentDataSource.CreateDefault();
        SaveData saveData = SaveData.CreateDefault();
        saveData.Currency.Gold = 200;
        saveData.Progress.HighestClearedDefenseSessionId = 101;
        GameContext context = new GameContext(saveData);
        DayGrowthService service = new DayGrowthService(dataSource, context);

        DayGrowthResult result = service.TryUnlockNextCitadelFloor();

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(context.DayProgress.CitadelFloorCount.Value, Is.EqualTo(2));
        Assert.That(context.CurrencyProgress.Gold.Value, Is.EqualTo(50));
    }

    // 낮 성장 서비스는 슬롯 성장 비용을 지불하고 슬롯 레벨을 올려야 합니다.
    [Test]
    public void DayGrowthService_UpgradesCombatSlot()
    {
        FakeContentDataSource dataSource = FakeContentDataSource.CreateDefault();
        SaveData saveData = SaveData.CreateDefault();
        saveData.Currency.Gold = 200;
        GameContext context = new GameContext(saveData);
        DayGrowthService service = new DayGrowthService(dataSource, context);

        DayGrowthResult result = service.TryUpgradeCombatSlot(0, 2101);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(context.CombatSlotProgress.SlotsByIndex[0].Level.Value, Is.EqualTo(2));
        Assert.That(context.CurrencyProgress.Gold.Value, Is.EqualTo(100));
    }

    // 테스트용 데이터 소스입니다. TSV 파서를 사용해 실제 Row 타입을 만들어 서비스 계약을 검증합니다.
    private sealed class FakeContentDataSource : INightDefenseDataSource, IDraftDataSource, IRewardDataSource, IDayGrowthDataSource
    {
        private DataTable<DefenseSessionDataRow> defenseSessions;
        private DataTable<WaveGroupDataRow> waveGroups;
        private DataTable<WaveDataRow> waves;
        private DataTable<DraftPoolDataRow> draftPools;
        private DataTable<DraftCardDataRow> draftCards;
        private DataTable<RewardDataRow> rewards;
        private DataTable<CitadelFloorDataRow> citadelFloors;
        private DataTable<CombatSlotDataRow> combatSlots;
        private DataTable<CombatSlotUpgradeDataRow> combatSlotUpgrades;

        // 서비스 테스트에 필요한 최소 테이블을 구성합니다.
        public static FakeContentDataSource CreateDefault()
        {
            return new FakeContentDataSource
            {
                defenseSessions = TsvParser.Parse<DefenseSessionDataRow>("DefenseSessionData", DefenseSessionTsv),
                waveGroups = TsvParser.Parse<WaveGroupDataRow>("WaveGroupData", WaveGroupTsv),
                waves = TsvParser.Parse<WaveDataRow>("WaveData", WaveTsv),
                draftPools = TsvParser.Parse<DraftPoolDataRow>("DraftPoolData", DraftPoolTsv),
                draftCards = TsvParser.Parse<DraftCardDataRow>("DraftCardData", DraftCardTsv),
                rewards = TsvParser.Parse<RewardDataRow>("RewardData", RewardTsv),
                citadelFloors = TsvParser.Parse<CitadelFloorDataRow>("CitadelFloorData", CitadelFloorTsv),
                combatSlots = TsvParser.Parse<CombatSlotDataRow>("CombatSlotData", CombatSlotTsv),
                combatSlotUpgrades = TsvParser.Parse<CombatSlotUpgradeDataRow>("CombatSlotUpgradeData", CombatSlotUpgradeTsv)
            };
        }

        public bool TryGetDefenseSession(int sessionId, out DefenseSessionDataRow row)
        {
            return defenseSessions.TryGet(sessionId, out row);
        }

        public bool TryGetWaveGroup(int waveGroupId, out WaveGroupDataRow row)
        {
            return waveGroups.TryGet(waveGroupId, out row);
        }

        public IReadOnlyList<WaveDataRow> GetWaveRows(int waveGroupId, int waveIndex)
        {
            List<WaveDataRow> rows = new List<WaveDataRow>();

            for (int i = 0; i < waves.Rows.Count; i++)
            {
                WaveDataRow row = waves.Rows[i];

                if (row.WaveGroupId == waveGroupId && row.WaveIndex == waveIndex)
                    rows.Add(row);
            }

            return rows;
        }

        public bool TryGetDraftPool(int draftPoolId, out DraftPoolDataRow row)
        {
            return draftPools.TryGet(draftPoolId, out row);
        }

        public IReadOnlyList<DraftCardDataRow> GetDraftCards()
        {
            return draftCards.Rows;
        }

        public IReadOnlyList<RewardDataRow> GetRewardRows(int rewardGroupId)
        {
            List<RewardDataRow> rows = new List<RewardDataRow>();

            for (int i = 0; i < rewards.Rows.Count; i++)
            {
                RewardDataRow row = rewards.Rows[i];

                if (row.RewardGroupId == rewardGroupId)
                    rows.Add(row);
            }

            return rows;
        }

        public bool TryGetCitadelFloor(int floorId, out CitadelFloorDataRow row)
        {
            return citadelFloors.TryGet(floorId, out row);
        }

        public bool TryGetCombatSlot(int slotId, out CombatSlotDataRow row)
        {
            return combatSlots.TryGet(slotId, out row);
        }

        public bool TryGetCombatSlotUpgrade(int upgradeGroupId, int level, out CombatSlotUpgradeDataRow row)
        {
            for (int i = 0; i < combatSlotUpgrades.Rows.Count; i++)
            {
                row = combatSlotUpgrades.Rows[i];

                if (row.UpgradeGroupId == upgradeGroupId && row.Level == level)
                    return true;
            }

            row = null;
            return false;
        }

        private const string DefenseSessionTsv =
            "SessionId\tDayIndex\tNightIndex\tNameKey\tWaveGroupId\tDraftPoolId\tRewardGroupId\tRequiredFloorId\tClearConditionType\tTimeLimitSec\tBossEnemyId\tEnvironmentTagList\n" +
            "101\t1\t1\tsession_night_01\t8001\t7001\t9001\t1\tClearAllWaves\t0\t0\tForest|Early\n" +
            "103\t3\t3\tsession_night_03\t8003\t7002\t9003\t2\tDefeatBoss\t0\t1301\tForest|Boss\n";

        private const string WaveGroupTsv =
            "WaveGroupId\tNameKey\tMaxWaveIndex\tDraftIntervalWave\tBossWaveIndex\tBaseSpawnBudget\tScalingGroupId\n" +
            "8001\twave_group_early_01\t2\t2\t0\t100\t0\n" +
            "8003\twave_group_boss_01\t7\t2\t7\t180\t0\n";

        private const string WaveTsv =
            "WaveRowId\tWaveGroupId\tWaveIndex\tEnemyId\tCount\tSpawnStartSec\tSpawnIntervalSec\tLaneId\tIsBossWave\tDraftAfterWave\n" +
            "810001\t8001\t1\t1101\t12\t0.0\t0.6\t1\tFALSE\tFALSE\n" +
            "810002\t8001\t2\t1101\t16\t0.0\t0.5\t1\tFALSE\tTRUE\n" +
            "810201\t8003\t7\t1301\t1\t1.0\t0.0\t1\tTRUE\tFALSE\n";

        private const string DraftPoolTsv =
            "DraftPoolId\tNameKey\tIncludeTagList\tExcludeTagList\tGradeWeightCommon\tGradeWeightRare\tGradeWeightEpic\tPickCount\tRerollCostCurrencyId\tRerollCostAmount\n" +
            "7001\tdraft_pool_early\tAttack|Projectile|Element\t\t80\t18\t2\t3\t0\t0\n";

        private const string DraftCardTsv =
            "CardId\tNameKey\tDescKey\tCardGrade\tCardTagList\tElementType\tMaxStack\tIsUnique\tWeight\tRequiredUnlockId\tIconKey\n" +
            "7101\tcard_attack_up\tcard_attack_up_desc\tCommon\tAttack\tNone\t5\tFALSE\t100\t0\ticon_card_attack\n" +
            "7102\tcard_multi_shot\tcard_multi_shot_desc\tRare\tProjectile|Attack\tNone\t3\tFALSE\t40\t0\ticon_card_multishot\n" +
            "7105\tcard_boss_breaker\tcard_boss_breaker_desc\tRare\tBossKiller|Attack\tNone\t2\tFALSE\t20\t0\ticon_card_boss\n" +
            "7199\tcard_utility\tcard_utility_desc\tCommon\tUtility\tNone\t1\tFALSE\t999\t0\ticon_card_utility\n";

        private const string RewardTsv =
            "RewardRowId\tRewardGroupId\tRewardItemType\tRewardItemId\tAmount\tChancePermille\tFirstClearOnly\tPreviewOrder\n" +
            "900101\t9001\tCurrency\t1\t100\t1000\tFALSE\t1\n" +
            "900102\t9001\tMaterial\t20001\t5\t1000\tFALSE\t2\n";

        private const string CitadelFloorTsv =
            "FloorId\tNameKey\tRequiredSessionId\tUnlockCostCurrencyId\tUnlockCostAmount\tModuleSlotCount\tUnlockFeatureType\tUnlockFeatureId\tVisualKey\n" +
            "1\tfloor_gate\t0\t1\t0\t1\tCombatSlot\t2001\tfloor_gate_visual\n" +
            "2\tfloor_workshop\t101\t1\t150\t1\tBuildingModule\t5001\tfloor_workshop_visual\n";

        private const string CombatSlotTsv =
            "SlotId\tSlotIndex\tSlotType\tUnlockFloorId\tAllowedHeroRoleList\tUpgradeGroupId\tDefaultHeroId\tPositionKey\tIsDefaultUnlocked\n" +
            "2001\t0\tFront\t1\tDealer|Tank\t2101\t1001\tslot_front_01\tTRUE\n";

        private const string CombatSlotUpgradeTsv =
            "UpgradeId\tUpgradeGroupId\tLevel\tCostCurrencyId\tCostAmount\tAttackBonusPct\tAttackSpeedBonusPct\tRangeBonusPct\tSkillChargeBonusPct\tUnlockModuleSocket\n" +
            "210101\t2101\t1\t1\t0\t0\t0\t0\t0\t0\n" +
            "210102\t2101\t2\t1\t100\t10\t0\t0\t0\t0\n";
    }
}
