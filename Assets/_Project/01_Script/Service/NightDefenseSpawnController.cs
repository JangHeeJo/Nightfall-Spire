using System;

// NightDefenseWavePlan을 시간 흐름에 맞춰 스폰 요청으로 실행합니다.
public sealed class NightDefenseSpawnController
{
    private NightDefenseWavePlan currentPlan = NightDefenseWavePlan.Empty(); // 현재 실행 중인 웨이브 스폰 계획
    private int nextSpawnIndex; // 다음으로 발생시킬 스폰 이벤트 인덱스
    private float waveElapsedSec; // 현재 웨이브 기준 경과 시간

    public bool IsRunning { get; private set; } // 웨이브 스폰 시간표 실행 중 여부
    public bool IsWaveSpawnComplete => IsRunning && nextSpawnIndex >= currentPlan.SpawnEvents.Count; // 모든 스폰 이벤트 처리 여부
    public NightDefenseWavePlan CurrentPlan => currentPlan; // 현재 실행 중인 스폰 계획
    public float WaveElapsedSec => waveElapsedSec; // 현재 웨이브 기준 경과 시간

    // 새 웨이브 스폰 계획을 처음부터 실행합니다.
    public bool BeginWave(NightDefenseWavePlan plan)
    {
        if (plan == null || plan.TotalSpawnCount <= 0)
            return false;

        currentPlan = plan;
        nextSpawnIndex = 0;
        waveElapsedSec = 0f;
        IsRunning = true;
        return true;
    }

    // 현재 웨이브 스폰 실행을 중단하고 내부 상태를 초기화합니다.
    public void Stop()
    {
        currentPlan = NightDefenseWavePlan.Empty();
        nextSpawnIndex = 0;
        waveElapsedSec = 0f;
        IsRunning = false;
    }

    // deltaSeconds만큼 시간을 진행하고, 도달한 스폰 이벤트를 sink로 전달합니다.
    public NightDefenseSpawnTickResult Tick(float deltaSeconds, INightDefenseSpawnSink sink)
    {
        if (!IsRunning || deltaSeconds < 0f)
            return new NightDefenseSpawnTickResult(0, IsWaveSpawnComplete, waveElapsedSec);

        if (sink == null)
            throw new ArgumentNullException(nameof(sink));

        waveElapsedSec += deltaSeconds;
        int spawnedCount = 0;

        while (nextSpawnIndex < currentPlan.SpawnEvents.Count)
        {
            NightDefenseSpawnEvent spawnEvent = currentPlan.SpawnEvents[nextSpawnIndex];

            if (spawnEvent.SpawnTimeSec > waveElapsedSec)
                break;

            sink.Spawn(new NightDefenseSpawnRequest(currentPlan.WaveIndex, spawnEvent, waveElapsedSec));
            nextSpawnIndex += 1;
            spawnedCount += 1;
        }

        return new NightDefenseSpawnTickResult(spawnedCount, IsWaveSpawnComplete, waveElapsedSec);
    }
}
