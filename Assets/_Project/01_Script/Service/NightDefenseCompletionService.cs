using System;

// 밤 방어전 종료 시 세션 데이터 기준 보상 계산을 담당합니다.
// GameFlowController가 테이블 세부 구조를 직접 알지 않게 만드는 애플리케이션 서비스입니다.
public sealed class NightDefenseCompletionService
{
    private readonly INightDefenseDataSource dataSource; // 방어 세션 테이블 조회 계약
    private readonly RewardService rewardService; // 보상 그룹 계산 서비스
    private readonly GameProgress gameProgress; // 최초 클리어 여부 판단에 필요한 진행 모델

    // 종료 보상 계산에 필요한 세션 데이터, 보상 서비스, 진행 모델을 받습니다.
    public NightDefenseCompletionService(INightDefenseDataSource dataSource, RewardService rewardService, GameProgress gameProgress)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        this.rewardService = rewardService ?? throw new ArgumentNullException(nameof(rewardService));
        this.gameProgress = gameProgress ?? throw new ArgumentNullException(nameof(gameProgress));
    }

    // 방어 결과와 세션 ID를 기준으로 수령 대기 재화 보상을 계산합니다.
    public NightDefenseCompletionRewardResult BuildRewardForCompletion(int defenseSessionId, DefenseOutcome outcome)
    {
        if (outcome != DefenseOutcome.Victory)
            return NightDefenseCompletionRewardResult.Success(0, 0);

        if (!dataSource.TryGetDefenseSession(defenseSessionId, out DefenseSessionDataRow sessionRow))
            return NightDefenseCompletionRewardResult.Fail(NightDefenseCompletionFailureReason.SessionNotFound);

        bool isFirstClear = gameProgress.HighestClearedDefenseSessionId.Value < defenseSessionId;
        RewardGrantResult rewardResult = rewardService.BuildReward(sessionRow.RewardGroupId, isFirstClear);

        if (!rewardResult.IsSuccess)
            return NightDefenseCompletionRewardResult.Fail(NightDefenseCompletionFailureReason.RewardBuildFailed, rewardResult.FailureReason);

        return NightDefenseCompletionRewardResult.Success(rewardResult.Gold, rewardResult.Gem);
    }
}
