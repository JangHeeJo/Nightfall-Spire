using System.Collections.Generic;

// WaveDataRow 목록을 실제 스폰 시간표로 변환합니다.
public sealed class NightDefenseWavePlanBuilder
{
    private const float SameLaneMinimumSpawnIntervalSec = 0.35f; // 같은 라인에서 몬스터가 한 점에 겹쳐 나오지 않게 보장하는 최소 스폰 간격

    // 웨이브 Row 목록을 검증하고 시간순 스폰 계획을 만듭니다.
    public bool TryBuild(
        int waveIndex,
        IReadOnlyList<WaveDataRow> waveRows,
        bool isBossWave,
        bool isLastWave,
        out NightDefenseWavePlan plan)
    {
        plan = NightDefenseWavePlan.Empty();

        if (waveIndex <= 0 || waveRows == null || waveRows.Count == 0)
            return false;

        List<NightDefenseSpawnEvent> spawnEvents = new List<NightDefenseSpawnEvent>();
        int spawnOrder = 0;

        for (int rowIndex = 0; rowIndex < waveRows.Count; rowIndex++)
        {
            WaveDataRow row = waveRows[rowIndex];

            if (!IsValidWaveRow(row, waveIndex))
                return false;

            for (int countIndex = 0; countIndex < row.Count; countIndex++)
            {
                float spawnTimeSec = row.SpawnStartSec + row.SpawnIntervalSec * countIndex;
                spawnEvents.Add(new NightDefenseSpawnEvent(row.WaveRowId, row.EnemyId, row.LaneId, spawnOrder, spawnTimeSec));
                spawnOrder += 1;
            }
        }

        spawnEvents.Sort(CompareSpawnEvent);
        ApplySameLaneSpawnSpacing(spawnEvents);
        plan = new NightDefenseWavePlan(waveIndex, isBossWave, isLastWave, spawnEvents);
        return plan.TotalSpawnCount > 0;
    }

    // 스폰 계획을 만들 수 있는 유효한 웨이브 Row인지 확인합니다.
    private static bool IsValidWaveRow(WaveDataRow row, int expectedWaveIndex)
    {
        return row != null &&
               row.WaveIndex == expectedWaveIndex &&
               row.WaveRowId > 0 &&
               row.EnemyId > 0 &&
               row.Count > 0 &&
               row.SpawnStartSec >= 0f &&
               row.SpawnIntervalSec >= 0f &&
               row.LaneId > 0;
    }

    // 스폰 시간을 우선으로 정렬하고, 시간이 같으면 원래 생성 순서를 유지합니다.
    private static int CompareSpawnEvent(NightDefenseSpawnEvent left, NightDefenseSpawnEvent right)
    {
        int timeCompare = left.SpawnTimeSec.CompareTo(right.SpawnTimeSec);

        if (timeCompare != 0)
            return timeCompare;

        return left.SpawnOrder.CompareTo(right.SpawnOrder);
    }

    // 테이블에서 같은 라인과 같은 시간에 여러 적이 잡혀도 실제 스폰 계획에서는 최소 간격을 벌립니다.
    private static void ApplySameLaneSpawnSpacing(List<NightDefenseSpawnEvent> spawnEvents)
    {
        Dictionary<int, float> lastSpawnTimeByLane = new Dictionary<int, float>();

        for (int i = 0; i < spawnEvents.Count; i++)
        {
            NightDefenseSpawnEvent spawnEvent = spawnEvents[i];

            if (!lastSpawnTimeByLane.TryGetValue(spawnEvent.LaneId, out float lastSpawnTime))
            {
                lastSpawnTimeByLane[spawnEvent.LaneId] = spawnEvent.SpawnTimeSec;
                continue;
            }

            float minAllowedTime = lastSpawnTime + SameLaneMinimumSpawnIntervalSec;

            if (spawnEvent.SpawnTimeSec >= minAllowedTime)
            {
                lastSpawnTimeByLane[spawnEvent.LaneId] = spawnEvent.SpawnTimeSec;
                continue;
            }

            spawnEvents[i] = new NightDefenseSpawnEvent(
                spawnEvent.WaveRowId,
                spawnEvent.EnemyId,
                spawnEvent.LaneId,
                spawnEvent.SpawnOrder,
                minAllowedTime);

            lastSpawnTimeByLane[spawnEvent.LaneId] = minAllowedTime;
        }

        spawnEvents.Sort(CompareSpawnEvent);
    }
}
