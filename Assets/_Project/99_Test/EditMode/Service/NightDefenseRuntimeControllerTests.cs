using System.Collections.Generic;
using NUnit.Framework;

// 밤 방어 런타임 컨트롤러가 웨이브 시간표를 실제 스폰 요청으로 실행하는지 검증합니다.
public sealed class NightDefenseRuntimeControllerTests
{
    // 시간이 흐르면 스폰 요청이 시간표 순서대로 발생해야 합니다.
    [Test]
    public void RuntimeController_SpawnsEventsByElapsedTime()
    {
        FakeNightDefenseDataSource dataSource = FakeNightDefenseDataSource.CreateDefault();
        GameContext context = CreateActiveContext(dataSource);
        RecordingSpawnSink spawnSink = new RecordingSpawnSink();
        NightDefenseRuntimeController runtimeController = new NightDefenseRuntimeController(
            new NightDefenseSessionService(dataSource, context),
            context.NightDefenseProgress,
            spawnSink);

        NightDefenseRuntimeStartResult startResult = runtimeController.StartNextWave();
        NightDefenseRuntimeTickResult firstTick = runtimeController.Tick(0.25f);
        NightDefenseRuntimeTickResult secondTick = runtimeController.Tick(0.75f);
        NightDefenseRuntimeTickResult thirdTick = runtimeController.Tick(0.5f);

        Assert.That(startResult.IsSuccess, Is.True);
        Assert.That(firstTick.SpawnedCount, Is.EqualTo(1));
        Assert.That(secondTick.SpawnedCount, Is.EqualTo(1));
        Assert.That(thirdTick.SpawnedCount, Is.EqualTo(1));
        Assert.That(thirdTick.IsWaveSpawnComplete, Is.True);
        Assert.That(thirdTick.ShouldOpenDraft, Is.True);
        Assert.That(spawnSink.Requests.Count, Is.EqualTo(3));
        Assert.That(spawnSink.Requests[0].EnemyId, Is.EqualTo(1102));
        Assert.That(spawnSink.Requests[1].EnemyId, Is.EqualTo(1101));
        Assert.That(spawnSink.Requests[2].EnemyId, Is.EqualTo(1101));
        Assert.That(context.NightDefenseProgress.ElapsedSeconds.Value, Is.EqualTo(1.5f).Within(0.001f));
    }

    // 활성화된 방어 세션이 없으면 다음 웨이브를 시작하지 않아야 합니다.
    [Test]
    public void RuntimeController_BlocksWaveStartWhenSessionIsInactive()
    {
        FakeNightDefenseDataSource dataSource = FakeNightDefenseDataSource.CreateDefault();
        GameContext context = new GameContext(SaveData.CreateDefault());
        RecordingSpawnSink spawnSink = new RecordingSpawnSink();
        NightDefenseRuntimeController runtimeController = new NightDefenseRuntimeController(
            new NightDefenseSessionService(dataSource, context),
            context.NightDefenseProgress,
            spawnSink);

        NightDefenseRuntimeStartResult result = runtimeController.StartNextWave();

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(NightDefenseFailureReason.SessionNotActive));
        Assert.That(spawnSink.Requests.Count, Is.EqualTo(0));
    }

    // 테스트용으로 밤 방어 세션이 시작된 Context를 만듭니다.
    private static GameContext CreateActiveContext(FakeNightDefenseDataSource dataSource)
    {
        GameContext context = new GameContext(SaveData.CreateDefault());
        context.GameProgress.SetCurrentDefenseSession(101);
        NightDefenseSessionService service = new NightDefenseSessionService(dataSource, context);
        NightDefenseStartResult startResult = service.TryStartCurrentSession();

        Assert.That(startResult.IsSuccess, Is.True);
        return context;
    }

    // 스폰 요청을 기록하는 테스트용 수신자입니다.
    private sealed class RecordingSpawnSink : INightDefenseSpawnSink
    {
        public List<NightDefenseSpawnRequest> Requests { get; } = new List<NightDefenseSpawnRequest>();

        // 실제 생성 대신 요청 목록에 기록합니다.
        public void Spawn(NightDefenseSpawnRequest request)
        {
            Requests.Add(request);
        }
    }

    // 테스트에 필요한 최소 밤 방어 테이블을 제공하는 데이터 소스입니다.
    private sealed class FakeNightDefenseDataSource : INightDefenseDataSource
    {
        private DataTable<DefenseSessionDataRow> defenseSessions;
        private DataTable<WaveGroupDataRow> waveGroups;
        private DataTable<WaveDataRow> waves;

        // 웨이브 스폰 순서 검증에 필요한 최소 데이터를 만듭니다.
        public static FakeNightDefenseDataSource CreateDefault()
        {
            return new FakeNightDefenseDataSource
            {
                defenseSessions = TsvParser.Parse<DefenseSessionDataRow>("DefenseSessionData", DefenseSessionTsv),
                waveGroups = TsvParser.Parse<WaveGroupDataRow>("WaveGroupData", WaveGroupTsv),
                waves = TsvParser.Parse<WaveDataRow>("WaveData", WaveTsv)
            };
        }

        public bool TryGetDefenseSession(int sessionId, out DefenseSessionDataRow row)
        {
            return defenseSessions.TryGet(sessionId, out row);
        }

        public bool TryGetFirstDefenseSession(out DefenseSessionDataRow row)
        {
            row = defenseSessions.Rows.Count > 0 ? defenseSessions.Rows[0] : null;
            return row != null;
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

        private const string DefenseSessionTsv =
            "SessionId\tDayIndex\tNightIndex\tNameKey\tWaveGroupId\tDraftPoolId\tRewardGroupId\tRequiredFloorId\tClearConditionType\tTimeLimitSec\tBossEnemyId\tEnvironmentTagList\n" +
            "101\t1\t1\tsession_night_01\t8001\t7001\t9001\t1\tClearAllWaves\t0\t0\tForest|Early\n";

        private const string WaveGroupTsv =
            "WaveGroupId\tNameKey\tMaxWaveIndex\tDraftIntervalWave\tBossWaveIndex\tBaseSpawnBudget\tScalingGroupId\n" +
            "8001\twave_group_early_01\t1\t1\t0\t100\t0\n";

        private const string WaveTsv =
            "WaveRowId\tWaveGroupId\tWaveIndex\tEnemyId\tCount\tSpawnStartSec\tSpawnIntervalSec\tLaneId\tIsBossWave\tDraftAfterWave\n" +
            "810001\t8001\t1\t1101\t2\t1.0\t0.5\t1\tFALSE\tTRUE\n" +
            "810002\t8001\t1\t1102\t1\t0.25\t0.0\t2\tFALSE\tTRUE\n";
    }
}
