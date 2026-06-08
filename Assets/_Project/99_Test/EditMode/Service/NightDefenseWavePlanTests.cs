using NUnit.Framework;

// WaveDataRow가 실제 스폰 시간표로 안전하게 변환되는지 검증합니다.
public sealed class NightDefenseWavePlanTests
{
    // 여러 Wave Row가 섞여도 스폰 이벤트는 시간순으로 정렬되어야 합니다.
    [Test]
    public void WavePlanBuilder_BuildsSortedSpawnSchedule()
    {
        NightDefenseWavePlanBuilder builder = new NightDefenseWavePlanBuilder();
        WaveDataRow[] rows =
        {
            CreateWaveRow(1, 1, 1101, 2, 1.0f, 0.5f, 1),
            CreateWaveRow(2, 1, 1102, 1, 0.25f, 0.0f, 2)
        };

        bool built = builder.TryBuild(1, rows, false, true, false, out NightDefenseWavePlan plan);

        Assert.That(built, Is.True);
        Assert.That(plan.TotalSpawnCount, Is.EqualTo(3));
        Assert.That(plan.SpawnEvents[0].EnemyId, Is.EqualTo(1102));
        Assert.That(plan.SpawnEvents[0].SpawnTimeSec, Is.EqualTo(0.25f));
        Assert.That(plan.SpawnEvents[1].EnemyId, Is.EqualTo(1101));
        Assert.That(plan.SpawnEvents[1].SpawnTimeSec, Is.EqualTo(1.0f));
        Assert.That(plan.SpawnEvents[2].SpawnTimeSec, Is.EqualTo(1.5f));
        Assert.That(plan.DraftAfterWave, Is.True);
        Assert.That(plan.IsLastWave, Is.False);
    }

    // 스폰 수가 0인 Row는 런타임으로 넘기지 않고 실패해야 합니다.
    [Test]
    public void WavePlanBuilder_RejectsInvalidWaveRow()
    {
        NightDefenseWavePlanBuilder builder = new NightDefenseWavePlanBuilder();
        WaveDataRow[] rows =
        {
            CreateWaveRow(1, 1, 1101, 0, 0f, 0.5f, 1)
        };

        bool built = builder.TryBuild(1, rows, false, false, false, out NightDefenseWavePlan plan);

        Assert.That(built, Is.False);
        Assert.That(plan.TotalSpawnCount, Is.EqualTo(0));
    }

    // 테스트용 WaveDataRow를 TSV 파서로 만들어 실제 Row Load 계약까지 함께 검증합니다.
    private static WaveDataRow CreateWaveRow(int rowId, int waveIndex, int enemyId, int count, float startSec, float intervalSec, int laneId)
    {
        string tsv =
            "WaveRowId\tWaveGroupId\tWaveIndex\tEnemyId\tCount\tSpawnStartSec\tSpawnIntervalSec\tLaneId\tIsBossWave\tDraftAfterWave\n" +
            $"{rowId}\t8001\t{waveIndex}\t{enemyId}\t{count}\t{startSec}\t{intervalSec}\t{laneId}\tFALSE\tFALSE\n";

        return TsvParser.Parse<WaveDataRow>("WaveData", tsv).Rows[0];
    }
}
