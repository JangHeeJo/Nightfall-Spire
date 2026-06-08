// 재화 HUD View가 Presenter에게 제공해야 하는 표시 기능입니다.
// Unity 컴포넌트 구현과 Presenter 사이를 분리해 MVP 경계를 명확히 둡니다.
public interface ICurrencyHudView
{
    // Presenter가 만든 표시 상태를 화면에 반영합니다.
    void Render(CurrencyHudViewState viewState);

    // View에 필요한 Inspector 참조가 연결되어 있는지 알려줍니다.
    bool IsReady();
}
