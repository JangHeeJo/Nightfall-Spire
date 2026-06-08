// 같은 팝업 요청이 들어왔을 때 PopupManager가 처리할 방식을 정의합니다.
public enum PopupOpenPolicy
{
    Stack, // 기존 팝업 위에 새 팝업을 쌓습니다.
    SingleInstance, // 같은 Key의 팝업이 이미 열려 있으면 기존 팝업을 재사용합니다.
    ReplaceTop, // 가장 위 팝업을 닫고 새 팝업을 엽니다.
    ReplaceAll // 현재 열린 모든 팝업을 닫고 새 팝업을 엽니다.
}
