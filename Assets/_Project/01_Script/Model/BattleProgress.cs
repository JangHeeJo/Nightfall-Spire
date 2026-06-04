// 이전 Battle 중심 구조에서 사용하던 호환용 모델입니다.
// 새 구조에서는 NightDefenseProgress가 밤 방어 세션의 중심 상태를 담당합니다.
public sealed class BattleProgress
{
    public bool IsBattleActive { get; private set; } // 이전 전투 진행 중 여부

    // 이전 구조에서 전투 시작을 표시하던 호환 메서드입니다.
    // 새 코드에서는 NightDefenseProgress.BeginDefenseSession을 사용합니다.
    public void BeginBattle()
    {
        IsBattleActive = true;
    }

    // 이전 구조에서 전투 종료를 표시하던 호환 메서드입니다.
    // 새 코드에서는 NightDefenseProgress.EndDefenseSession을 사용합니다.
    public void EndBattle()
    {
        IsBattleActive = false;
    }
}
