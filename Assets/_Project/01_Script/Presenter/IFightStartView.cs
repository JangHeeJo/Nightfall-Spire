using System;

// 밤 방어 시작 버튼 View가 Presenter에 제공해야 하는 계약입니다.
public interface IFightStartView
{
    event Action Clicked; // 사용자가 전투 시작 버튼을 눌렀을 때 발생하는 이벤트

    // 중복 클릭이나 전환 중 입력을 막기 위해 버튼 입력 가능 여부를 바꿉니다.
    void SetInteractable(bool isInteractable);
}
