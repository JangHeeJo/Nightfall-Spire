using System;
using System.Collections.Generic;

// 한 웨이브에서 실제 스폰러가 따라야 할 시간표입니다.
// 전투 프리팹 생성기는 WaveDataRow를 직접 해석하지 않고 이 계획만 보고 움직입니다.
public sealed class NightDefenseWavePlan
{
    private readonly List<NightDefenseSpawnEvent> spawnEvents; // 시간순으로 정렬된 스폰 이벤트 목록

    public int WaveIndex { get; } // 웨이브 번호
    public bool IsBossWave { get; } // 보스 웨이브 여부
    public bool DraftAfterWave { get; } // 웨이브 종료 뒤 드래프트를 열지 여부
    public bool IsLastWave { get; } // 세션의 마지막 웨이브 여부
    public int TotalSpawnCount { get; } // 이 웨이브에서 생성될 총 적 수
    public float LastSpawnTimeSec { get; } // 마지막 스폰 이벤트 시간
    public IReadOnlyList<NightDefenseSpawnEvent> SpawnEvents => spawnEvents; // 외부 읽기 전용 스폰 이벤트 목록

    // 계산이 끝난 스폰 이벤트 목록과 웨이브 메타 정보를 보관합니다.
    public NightDefenseWavePlan(int waveIndex, bool isBossWave, bool draftAfterWave, bool isLastWave, IReadOnlyList<NightDefenseSpawnEvent> spawnEvents)
    {
        WaveIndex = waveIndex;
        IsBossWave = isBossWave;
        DraftAfterWave = draftAfterWave;
        IsLastWave = isLastWave;
        this.spawnEvents = new List<NightDefenseSpawnEvent>(spawnEvents ?? Array.Empty<NightDefenseSpawnEvent>());

        TotalSpawnCount = this.spawnEvents.Count;
        LastSpawnTimeSec = this.spawnEvents.Count == 0 ? 0f : this.spawnEvents[this.spawnEvents.Count - 1].SpawnTimeSec;
    }

    // 실패 결과나 아직 웨이브가 없는 상태에서 사용할 빈 계획을 만듭니다.
    public static NightDefenseWavePlan Empty()
    {
        return new NightDefenseWavePlan(0, false, false, false, Array.Empty<NightDefenseSpawnEvent>());
    }
}

// 한 마리의 적을 언제 어느 라인에 만들지 나타내는 값입니다.
public readonly struct NightDefenseSpawnEvent
{
    public int WaveRowId { get; } // 이 스폰 이벤트를 만든 WaveData Row ID
    public int EnemyId { get; } // 생성할 적 ID
    public int LaneId { get; } // 생성할 라인 ID
    public int SpawnOrder { get; } // 웨이브 안에서의 생성 순서
    public float SpawnTimeSec { get; } // 웨이브 시작 후 생성 시간

    // 스폰러가 바로 사용할 수 있는 단위 이벤트 값을 만듭니다.
    public NightDefenseSpawnEvent(int waveRowId, int enemyId, int laneId, int spawnOrder, float spawnTimeSec)
    {
        WaveRowId = waveRowId;
        EnemyId = enemyId;
        LaneId = laneId;
        SpawnOrder = spawnOrder;
        SpawnTimeSec = spawnTimeSec;
    }
}

// WaveDataRow 목록을 실제 스폰 시간표로 변환합니다.
public sealed class NightDefenseWavePlanBuilder
{
    // 웨이브 Row 목록을 검증하고 시간순 스폰 계획을 만듭니다.
    public bool TryBuild(
        int waveIndex,
        IReadOnlyList<WaveDataRow> waveRows,
        bool isBossWave,
        bool draftAfterWave,
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
        plan = new NightDefenseWavePlan(waveIndex, isBossWave, draftAfterWave, isLastWave, spawnEvents);
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
}
