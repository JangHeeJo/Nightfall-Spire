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
