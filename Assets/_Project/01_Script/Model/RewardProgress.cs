// 밤 방어전 결과 보상을 임시로 보관하는 런타임 모델입니다.
// NightDefenseResult 화면이나 보상 팝업이 이 모델을 조회하는 흐름으로 확장합니다.
public sealed class RewardProgress
{
    public long PendingGold { get; private set; } // 아직 수령 처리되지 않은 골드 보상
    public long PendingGem { get; private set; } // 아직 수령 처리되지 않은 젬 보상

    // 수령 대기 보상을 설정합니다.
    public void SetPendingReward(long gold, long gem)
    {
        PendingGold = gold < 0 ? 0 : gold;
        PendingGem = gem < 0 ? 0 : gem;
    }

    // 수령 대기 보상을 비웁니다.
    public void ClearPendingReward()
    {
        PendingGold = 0;
        PendingGem = 0;
    }
}
