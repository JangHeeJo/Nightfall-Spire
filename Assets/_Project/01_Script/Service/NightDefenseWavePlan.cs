using System;
using System.Collections.Generic;

// 한 웨이브에서 실제 스폰러가 따라야 할 시간표입니다.
// 전투 프리팹 생성기는 WaveDataRow를 직접 해석하지 않고 이 계획만 보고 움직입니다.
public sealed class NightDefenseWavePlan
{
    private readonly List<NightDefenseSpawnEvent> spawnEvents; // 시간순으로 정렬된 스폰 이벤트 목록

    public int WaveIndex { get; } // 웨이브 번호
    public bool IsBossWave { get; } // 보스 웨이브 여부
    public bool IsLastWave { get; } // 세션의 마지막 웨이브 여부
    public int TotalSpawnCount { get; } // 이 웨이브에서 생성될 총 적 수
    public float LastSpawnTimeSec { get; } // 마지막 스폰 이벤트 시간
    public IReadOnlyList<NightDefenseSpawnEvent> SpawnEvents => spawnEvents; // 외부 읽기 전용 스폰 이벤트 목록

    // 계산이 끝난 스폰 이벤트 목록과 웨이브 메타 정보를 보관합니다.
    public NightDefenseWavePlan(int waveIndex, bool isBossWave, bool isLastWave, IReadOnlyList<NightDefenseSpawnEvent> spawnEvents)
    {
        WaveIndex = waveIndex;
        IsBossWave = isBossWave;
        IsLastWave = isLastWave;
        this.spawnEvents = new List<NightDefenseSpawnEvent>(spawnEvents ?? Array.Empty<NightDefenseSpawnEvent>());

        TotalSpawnCount = this.spawnEvents.Count;
        LastSpawnTimeSec = this.spawnEvents.Count == 0 ? 0f : this.spawnEvents[this.spawnEvents.Count - 1].SpawnTimeSec;
    }

    // 실패 결과나 아직 웨이브가 없는 상태에서 사용할 빈 계획을 만듭니다.
    public static NightDefenseWavePlan Empty()
    {
        return new NightDefenseWavePlan(0, false, false, Array.Empty<NightDefenseSpawnEvent>());
    }
}
