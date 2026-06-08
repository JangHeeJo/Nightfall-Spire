// 낮 성장 서비스에서 사용하는 실패 이유와 결과 계약입니다.
// 낮 성장 관련 작은 값 타입은 서비스 구현과 분리해 이 파일에 모아둡니다.
public enum DayGrowthFailureReason
{
    None,
    FloorNotFound,
    RequiredSessionNotCleared,
    NotEnoughCurrency,
    SlotNotFound,
    SlotLocked,
    UpgradeNotFound
}

// 낮 성장 명령 결과입니다.
public readonly struct DayGrowthResult
{
    public bool IsSuccess { get; } // 성장 명령 성공 여부
    public DayGrowthFailureReason FailureReason { get; } // 실패 이유

    private DayGrowthResult(bool isSuccess, DayGrowthFailureReason failureReason)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
    }

    // 성공 결과를 만듭니다.
    public static DayGrowthResult Success()
    {
        return new DayGrowthResult(true, DayGrowthFailureReason.None);
    }

    // 실패 이유를 포함한 실패 결과를 만듭니다.
    public static DayGrowthResult Fail(DayGrowthFailureReason failureReason)
    {
        return new DayGrowthResult(false, failureReason);
    }
}
