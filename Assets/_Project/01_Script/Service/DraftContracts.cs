using System;
using System.Collections.Generic;

// 드래프트 서비스에서 사용하는 실패 이유와 결과 계약입니다.
public enum DraftFailureReason
{
    None, // 실패 없음
    PoolNotFound, // 드래프트 풀 데이터를 찾지 못함
    NoCandidateCards, // 조건을 만족하는 후보 카드가 없음
    CardEffectNotFound, // 선택한 카드에 연결된 효과 Row가 없음
    UnsupportedEffectType // 현재 전투 런타임이 아직 처리하지 않는 효과 타입
}

// 드래프트 후보 생성 결과입니다.
public readonly struct DraftOfferResult
{
    public bool IsSuccess { get; } // 후보 생성 성공 여부
    public DraftFailureReason FailureReason { get; } // 실패 이유
    public IReadOnlyList<int> OfferedCardIds { get; } // 제시할 카드 ID 목록

    // 성공 여부, 실패 이유, 제시 카드 목록을 보관합니다.
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

// 드래프트 카드 효과 적용 결과입니다.
public readonly struct DraftEffectApplyResult
{
    public bool IsSuccess { get; } // 효과 적용 성공 여부
    public DraftFailureReason FailureReason { get; } // 실패 이유
    public int AppliedEffectCount { get; } // 실제 적용된 효과 수

    // 성공 여부, 실패 이유, 적용된 효과 수를 보관합니다.
    private DraftEffectApplyResult(bool isSuccess, DraftFailureReason failureReason, int appliedEffectCount)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        AppliedEffectCount = appliedEffectCount;
    }

    // 성공 결과를 만듭니다.
    public static DraftEffectApplyResult Success(int appliedEffectCount)
    {
        return new DraftEffectApplyResult(true, DraftFailureReason.None, appliedEffectCount);
    }

    // 실패 결과를 만듭니다.
    public static DraftEffectApplyResult Fail(DraftFailureReason failureReason)
    {
        return new DraftEffectApplyResult(false, failureReason, 0);
    }
}
