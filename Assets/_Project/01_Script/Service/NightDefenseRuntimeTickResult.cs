// 밤 방어 런타임이 한 번의 Tick에서 처리한 결과입니다.
public readonly struct NightDefenseRuntimeTickResult
{
    public int SpawnedCount { get; } // 이번 Tick에서 발생한 스폰 요청 수
    public bool IsWaveSpawnComplete { get; } // 현재 웨이브의 스폰 요청이 모두 발생했는지 여부
    public bool ShouldOpenDraft { get; } // 웨이브 스폰 완료 후 드래프트를 열어야 하는지 여부
    public bool IsLastWave { get; } // 현재 웨이브가 마지막 웨이브인지 여부
    public float SessionElapsedSec { get; } // 세션 전체 경과 시간
    public float WaveElapsedSec { get; } // 현재 웨이브 경과 시간

    // Tick 결과 값을 만듭니다.
    public NightDefenseRuntimeTickResult(int spawnedCount, bool isWaveSpawnComplete, bool shouldOpenDraft, bool isLastWave, float sessionElapsedSec, float waveElapsedSec)
    {
        SpawnedCount = spawnedCount;
        IsWaveSpawnComplete = isWaveSpawnComplete;
        ShouldOpenDraft = shouldOpenDraft;
        IsLastWave = isLastWave;
        SessionElapsedSec = sessionElapsedSec;
        WaveElapsedSec = waveElapsedSec;
    }
}
