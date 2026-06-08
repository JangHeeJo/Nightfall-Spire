using System;
using System.Collections.Generic;

// 방어전 보상 계산과 수령 대기 보상 반영을 담당합니다.
public sealed class RewardService
{
    private readonly IRewardDataSource dataSource; // 보상 테이블 조회 계약
    private readonly RewardProgress rewardProgress; // 보상 진행 모델

    // 보상 규칙에 필요한 데이터와 보상 모델을 받습니다.
    public RewardService(IRewardDataSource dataSource, RewardProgress rewardProgress)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        this.rewardProgress = rewardProgress ?? throw new ArgumentNullException(nameof(rewardProgress));
    }

    // 보상 그룹 기준으로 지급 가능한 보상 목록을 계산합니다.
    public RewardGrantResult BuildReward(int rewardGroupId, bool isFirstClear)
    {
        IReadOnlyList<RewardDataRow> rewardRows = dataSource.GetRewardRows(rewardGroupId);

        if (rewardRows.Count == 0)
            return RewardGrantResult.Fail(RewardFailureReason.RewardGroupNotFound);

        List<RewardLine> rewardLines = new List<RewardLine>();
        long gold = 0;
        long gem = 0;

        for (int i = 0; i < rewardRows.Count; i++)
        {
            RewardDataRow row = rewardRows[i];

            if (row.FirstClearOnly && !isFirstClear)
                continue;

            if (row.ChancePermille <= 0)
                continue;

            RewardLine line = new RewardLine(row.RewardItemType, row.RewardItemId, row.Amount);
            rewardLines.Add(line);

            if (row.RewardItemType == RewardItemType.Currency && row.RewardItemId == 1)
                gold += row.Amount;

            if (row.RewardItemType == RewardItemType.Currency && row.RewardItemId == 2)
                gem += row.Amount;
        }

        if (rewardLines.Count == 0)
            return RewardGrantResult.Fail(RewardFailureReason.NoGrantableRewards);

        return RewardGrantResult.Success(rewardLines, gold, gem);
    }

    // 계산된 보상 중 현재 RewardProgress가 표현할 수 있는 재화 보상을 수령 대기 상태로 반영합니다.
    public void SetPendingCurrencyReward(RewardGrantResult result)
    {
        if (!result.IsSuccess)
        {
            rewardProgress.SetPendingReward(0, 0);
            return;
        }

        rewardProgress.SetPendingReward(result.Gold, result.Gem);
    }
}

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

    public static RewardGrantResult Success(IReadOnlyList<RewardLine> rewardLines, long gold, long gem)
    {
        return new RewardGrantResult(true, RewardFailureReason.None, rewardLines, gold, gem);
    }

    public static RewardGrantResult Fail(RewardFailureReason failureReason)
    {
        return new RewardGrantResult(false, failureReason, Array.Empty<RewardLine>(), 0, 0);
    }
}
