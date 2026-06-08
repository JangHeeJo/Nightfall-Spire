// 밤 방어 런타임에서 새 웨이브 시작을 시도한 결과입니다.
public readonly struct NightDefenseRuntimeStartResult
{
    public bool IsSuccess { get; } // 웨이브 시작 성공 여부
    public NightDefenseFailureReason FailureReason { get; } // 세션 서비스 실패 이유
    public NightDefenseWavePlan WavePlan { get; } // 시작된 웨이브 스폰 계획

    private NightDefenseRuntimeStartResult(bool isSuccess, NightDefenseFailureReason failureReason, NightDefenseWavePlan wavePlan)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        WavePlan = wavePlan;
    }

    // 성공한 웨이브 시작 결과를 만듭니다.
    public static NightDefenseRuntimeStartResult Success(NightDefenseWavePlan wavePlan)
    {
        return new NightDefenseRuntimeStartResult(true, NightDefenseFailureReason.None, wavePlan);
    }

    // 실패한 웨이브 시작 결과를 만듭니다.
    public static NightDefenseRuntimeStartResult Fail(NightDefenseFailureReason failureReason)
    {
        return new NightDefenseRuntimeStartResult(false, failureReason, NightDefenseWavePlan.Empty());
    }
}
