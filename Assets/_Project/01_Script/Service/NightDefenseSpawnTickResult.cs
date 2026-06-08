// 스폰 컨트롤러가 한 번의 Tick에서 처리한 결과입니다.
public readonly struct NightDefenseSpawnTickResult
{
    public int SpawnedCount { get; } // 이번 Tick에서 발생한 스폰 요청 수
    public bool IsWaveSpawnComplete { get; } // 현재 웨이브의 모든 스폰 요청을 발생시켰는지 여부
    public float WaveElapsedSec { get; } // 현재 웨이브 기준 경과 시간

    // Tick 결과 값을 만듭니다.
    public NightDefenseSpawnTickResult(int spawnedCount, bool isWaveSpawnComplete, float waveElapsedSec)
    {
        SpawnedCount = spawnedCount;
        IsWaveSpawnComplete = isWaveSpawnComplete;
        WaveElapsedSec = waveElapsedSec;
    }
}
