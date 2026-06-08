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
