// 스파이어 메인 화면 팝업의 최상위 View입니다.
// 로비 진입 시 기본으로 열리는 Content 팝업이며, 성장/층/전투 시작 표시는 이후 전용 Controller에서 연결합니다.
public sealed class SpirePopup : BasePopup
{
    // 현재 단계에서는 BasePopup의 생성, 열림, 닫힘 생명주기만 사용합니다.
    // 스파이어 데이터 바인딩은 UI 참조가 확정된 뒤 이 클래스에 필요한 필드만 추가합니다.
}
