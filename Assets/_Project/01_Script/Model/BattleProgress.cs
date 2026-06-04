// 이전 Battle 중심 구조에서 사용하던 호환용 모델입니다.
// 새 구조에서는 NightDefenseProgress가 밤 방어 세션의 중심 상태를 담당합니다.
public sealed class BattleProgress
{
    public bool IsBattleActive { get; private set; } // 이전 전투 진행 중 여부

    public void BeginBattle()
    {
        IsBattleActive = true;
    }

    public void EndBattle()
    {
        IsBattleActive = false;
    }
}
