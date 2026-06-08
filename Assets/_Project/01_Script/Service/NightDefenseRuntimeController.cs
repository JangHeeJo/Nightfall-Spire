using System;

// 밤 방어 세션 서비스와 스폰 컨트롤러를 연결해 전투 런타임 흐름을 진행합니다.
public sealed class NightDefenseRuntimeController
{
    private readonly NightDefenseSessionService sessionService; // 웨이브 진행과 테이블 검증 담당
    private readonly NightDefenseProgress progress; // 밤 방어전 런타임 상태
    private readonly NightDefenseSpawnController spawnController; // 웨이브 스폰 시간표 실행기
    private readonly INightDefenseSpawnSink spawnSink; // 실제 스폰 요청 수신자

    private NightDefenseWavePlan currentWavePlan = NightDefenseWavePlan.Empty(); // 현재 실행 중인 웨이브 계획

    // 런타임 진행에 필요한 서비스, Progress, 스폰러 계약을 받습니다.
    public NightDefenseRuntimeController(
        NightDefenseSessionService sessionService,
        NightDefenseProgress progress,
        INightDefenseSpawnSink spawnSink,
        NightDefenseSpawnController spawnController = null)
    {
        this.sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
        this.spawnSink = spawnSink ?? throw new ArgumentNullException(nameof(spawnSink));
        this.spawnController = spawnController ?? new NightDefenseSpawnController();
    }

    // 다음 웨이브를 세션 서비스에서 받아와 스폰 컨트롤러에 등록합니다.
    public NightDefenseRuntimeStartResult StartNextWave()
    {
        NightDefenseWaveResult waveResult = sessionService.TryAdvanceNextWave();

        if (!waveResult.IsSuccess)
            return NightDefenseRuntimeStartResult.Fail(waveResult.FailureReason);

        bool began = spawnController.BeginWave(waveResult.WavePlan);

        if (!began)
            return NightDefenseRuntimeStartResult.Fail(NightDefenseFailureReason.InvalidWaveData);

        currentWavePlan = waveResult.WavePlan;
        return NightDefenseRuntimeStartResult.Success(currentWavePlan);
    }

    // 세션 시간을 진행하고 현재 웨이브의 스폰 요청을 발생시킵니다.
    public NightDefenseRuntimeTickResult Tick(float deltaSeconds)
    {
        if (!progress.IsDefenseActive.Value || deltaSeconds < 0f)
            return CreateEmptyTickResult();

        progress.SetElapsedSeconds(progress.ElapsedSeconds.Value + deltaSeconds);
        NightDefenseSpawnTickResult spawnResult = spawnController.Tick(deltaSeconds, spawnSink);

        return new NightDefenseRuntimeTickResult(
            spawnResult.SpawnedCount,
            spawnResult.IsWaveSpawnComplete,
            spawnResult.IsWaveSpawnComplete && currentWavePlan.DraftAfterWave,
            currentWavePlan.IsLastWave,
            progress.ElapsedSeconds.Value,
            spawnResult.WaveElapsedSec);
    }

    // 현재 웨이브 실행을 멈추고 스폰 컨트롤러 상태를 초기화합니다.
    public void Stop()
    {
        spawnController.Stop();
        currentWavePlan = NightDefenseWavePlan.Empty();
    }

    // 실행 중인 세션이나 웨이브가 없을 때 반환할 빈 Tick 결과를 만듭니다.
    private NightDefenseRuntimeTickResult CreateEmptyTickResult()
    {
        return new NightDefenseRuntimeTickResult(0, false, false, false, progress.ElapsedSeconds.Value, 0f);
    }
}
