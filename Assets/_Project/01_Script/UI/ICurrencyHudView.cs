// 재화 HUD View가 Controller에게 제공해야 하는 표시 기능입니다.
// Unity 컴포넌트 구현과 Controller 사이의 입력/출력 경계를 명확히 둡니다.
public interface ICurrencyHudView
{
    // Controller가 만든 표시 상태를 화면에 반영합니다.
    void Render(CurrencyHudViewState viewState);

    // View에 필요한 Inspector 참조가 연결되어 있는지 알려줍니다.
    bool IsReady();
}
