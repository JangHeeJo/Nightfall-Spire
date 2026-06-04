// 전투 런타임 진행 상태를 관리하는 모델입니다.
// 실제 전투 시스템이 붙기 전까지는 전투 진입 여부만 보관합니다.
public sealed class BattleProgress
{
    public bool IsBattleActive { get; private set; } // 전투 진행 중 여부

    public void BeginBattle()
    {
        IsBattleActive = true;
    }

    public void EndBattle()
    {
        IsBattleActive = false;
    }
}
