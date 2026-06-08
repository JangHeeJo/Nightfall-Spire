using System;
using System.Collections.Generic;

// 보상 서비스에서 사용하는 실패 이유와 결과 계약입니다.
public enum RewardFailureReason
{
    None,
    RewardGroupNotFound,
    NoGrantableRewards
}

// 보상 한 줄입니다.
public readonly struct RewardLine
{
    public RewardItemType ItemType { get; } // 보상 항목 타입
    public int ItemId { get; } // 보상 항목 ID
    public int Amount { get; } // 지급 수량

    // 보상 항목 타입, ID, 수량을 보관합니다.
    public RewardLine(RewardItemType itemType, int itemId, int amount)
    {
        ItemType = itemType;
        ItemId = itemId;
        Amount = amount;
    }
}

// 보상 계산 결과입니다.
public readonly struct RewardGrantResult
{
    public bool IsSuccess { get; } // 보상 계산 성공 여부
    public RewardFailureReason FailureReason { get; } // 실패 이유
    public IReadOnlyList<RewardLine> RewardLines { get; } // 지급할 보상 목록
    public long Gold { get; } // RewardProgress에 반영 가능한 골드 합계
    public long Gem { get; } // RewardProgress에 반영 가능한 젬 합계

    private RewardGrantResult(bool isSuccess, RewardFailureReason failureReason, IReadOnlyList<RewardLine> rewardLines, long gold, long gem)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        RewardLines = rewardLines;
        Gold = gold;
        Gem = gem;
    }

    // 성공한 보상 계산 결과를 만듭니다.
    public static RewardGrantResult Success(IReadOnlyList<RewardLine> rewardLines, long gold, long gem)
    {
        return new RewardGrantResult(true, RewardFailureReason.None, rewardLines, gold, gem);
    }

    // 실패 이유를 포함한 보상 계산 결과를 만듭니다.
    public static RewardGrantResult Fail(RewardFailureReason failureReason)
    {
        return new RewardGrantResult(false, failureReason, Array.Empty<RewardLine>(), 0, 0);
    }
}
