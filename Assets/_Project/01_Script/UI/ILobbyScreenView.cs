using System;

// 로비 화면 전체에서 발생하는 주요 사용자 입력을 Controller로 전달하는 View 계약입니다.
public interface ILobbyScreenView
{
    event Action<string> CommandRequested; // 로비 버튼 이름을 기준으로 한 화면 명령 요청

    // 로비 화면 View가 최소 표시와 입력 참조를 갖췄는지 알려줍니다.
    bool IsReady();

    // 특정 명령 버튼의 입력 가능 여부를 바꿉니다.
    void SetCommandInteractable(string commandKey, bool isInteractable);

    // 특정 명령 버튼의 해금 여부를 바꿉니다.
    void SetCommandUnlocked(string commandKey, bool isUnlocked);

    // 하단 탭 중 현재 선택된 버튼만 Focus 오브젝트를 켭니다.
    void SetFocusedCommand(string commandKey);

    // 하단 탭에 표시할 빨간 알림 점을 켜거나 끕니다.
    void SetCommandAlertVisible(string commandKey, bool isVisible);
}
