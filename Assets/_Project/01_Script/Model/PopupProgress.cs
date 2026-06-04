// 현재 팝업 표시 상태를 관리하는 모델입니다.
// PopupManager가 실제 스택을 갖기 전까지 전역 상태 구독용으로 사용합니다.
public sealed class PopupProgress
{
    public int OpenPopupCount { get; private set; } // 열려 있는 팝업 수

    public void IncreaseOpenCount()
    {
        OpenPopupCount++;
    }

    public void DecreaseOpenCount()
    {
        if (OpenPopupCount <= 0)
            return;

        OpenPopupCount--;
    }
}
