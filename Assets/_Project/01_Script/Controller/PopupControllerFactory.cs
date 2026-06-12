using System;

// 팝업 View와 팝업 Controller를 연결하는 조립 전용 Factory입니다.
// PopupManager는 팝업 수명만 관리하고, 어떤 팝업에 어떤 Controller가 붙는지는 이 클래스에서 결정합니다.
public sealed class PopupControllerFactory : IPopupControllerFactory
{
    // 팝업 인스턴스 타입에 맞는 Controller를 생성하고 초기화합니다.
    // View는 Controller 타입을 모르고 이벤트만 제공하므로, MVC 의존 방향을 이 조립 지점에 모읍니다.
    public IDisposable CreateController(BasePopup popup, PopupControllerContext context)
    {
        return popup switch
        {
            HeroListPopup heroListPopup => CreateHeroListController(heroListPopup, context),
            HeroDetailPopup heroDetailPopup => CreateHeroDetailController(heroDetailPopup),
            _ => null
        };
    }

    // 영웅 목록 팝업은 현재 게임 데이터와 상세 팝업을 열 PopupManager가 필요합니다.
    private static IDisposable CreateHeroListController(HeroListPopup popup, PopupControllerContext context)
    {
        if (context.GameContext == null || context.PopupManager == null)
            return null;

        HeroListController controller = new HeroListController(popup, context.GameContext, context.PopupManager);
        controller.Initialize();
        return controller;
    }

    // 영웅 상세 팝업은 View 이벤트와 표시 데이터만 다루므로 별도 전역 의존성이 필요 없습니다.
    private static IDisposable CreateHeroDetailController(HeroDetailPopup popup)
    {
        HeroDetailController controller = new HeroDetailController(popup);
        controller.Initialize();
        return controller;
    }
}
