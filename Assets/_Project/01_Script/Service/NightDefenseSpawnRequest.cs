// 전투 씬 스폰러에 전달할 적 생성 요청입니다.
public readonly struct NightDefenseSpawnRequest
{
    public int WaveIndex { get; } // 요청이 발생한 웨이브 번호
    public int WaveRowId { get; } // 요청의 원본 WaveData Row ID
    public int EnemyId { get; } // 생성할 적 ID
    public int LaneId { get; } // 생성할 라인 ID
    public int SpawnOrder { get; } // 웨이브 안에서의 생성 순서
    public float SpawnTimeSec { get; } // 웨이브 시작 기준 스폰 예정 시간
    public float RuntimeElapsedSec { get; } // 실제 런타임에서 요청이 발생한 시간

    // 스폰 시간표 이벤트를 실제 생성 요청으로 변환합니다.
    public NightDefenseSpawnRequest(int waveIndex, NightDefenseSpawnEvent spawnEvent, float runtimeElapsedSec)
    {
        WaveIndex = waveIndex;
        WaveRowId = spawnEvent.WaveRowId;
        EnemyId = spawnEvent.EnemyId;
        LaneId = spawnEvent.LaneId;
        SpawnOrder = spawnEvent.SpawnOrder;
        SpawnTimeSec = spawnEvent.SpawnTimeSec;
        RuntimeElapsedSec = runtimeElapsedSec;
    }
}
