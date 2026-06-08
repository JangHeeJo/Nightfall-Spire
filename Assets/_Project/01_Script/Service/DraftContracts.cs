using System;
using System.Collections.Generic;

// 드래프트 서비스에서 사용하는 실패 이유와 결과 계약입니다.
public enum DraftFailureReason
{
    None,
    PoolNotFound,
    NoCandidateCards
}

// 드래프트 후보 생성 결과입니다.
public readonly struct DraftOfferResult
{
    public bool IsSuccess { get; } // 후보 생성 성공 여부
    public DraftFailureReason FailureReason { get; } // 실패 이유
    public IReadOnlyList<int> OfferedCardIds { get; } // 제시할 카드 ID 목록

    private DraftOfferResult(bool isSuccess, DraftFailureReason failureReason, IReadOnlyList<int> offeredCardIds)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        OfferedCardIds = offeredCardIds;
    }

    // 성공한 드래프트 후보 결과를 만듭니다.
    public static DraftOfferResult Success(IReadOnlyList<int> offeredCardIds)
    {
        return new DraftOfferResult(true, DraftFailureReason.None, offeredCardIds);
    }

    // 실패 이유를 포함한 드래프트 후보 결과를 만듭니다.
    public static DraftOfferResult Fail(DraftFailureReason failureReason)
    {
        return new DraftOfferResult(false, failureReason, Array.Empty<int>());
    }
}
