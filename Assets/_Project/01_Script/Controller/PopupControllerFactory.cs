using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            SpirePopup spirePopup => CreateSpireController(spirePopup, context),
            HeroListPopup heroListPopup => CreateHeroListController(heroListPopup, context),
            HeroDetailPopup heroDetailPopup => CreateHeroDetailController(heroDetailPopup),
            _ => null
        };
    }

    // 스파이어 팝업은 중앙 FIGHT 버튼으로 밤 방어전 씬 전환을 요청합니다.
    private static IDisposable CreateSpireController(SpirePopup popup, PopupControllerContext context)
    {
        if (context.GameFlowController == null || context.PopupManager == null)
            return null;

        SpirePopupController controller = new SpirePopupController(popup, context.PopupManager, context.GameFlowController);
        controller.Initialize();
        return controller;
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

// 스파이어 팝업 내부 입력을 게임 흐름으로 연결하는 Controller입니다.
// View는 버튼 클릭만 알리고, 팝업 정리와 씬 전환 순서는 이 Controller가 담당합니다.
public sealed class SpirePopupController : IDisposable
{
    private readonly SpirePopup view; // 스파이어 팝업 View
    private readonly PopupManager popupManager; // 전투 진입 전 현재 로비 팝업을 정리하는 관리자
    private readonly GameFlowController gameFlowController; // 밤 방어전 씬으로 전환하는 게임 흐름 Controller

    private bool isInitialized; // 중복 초기화 방지
    private bool isDisposed; // 팝업 파괴 이후 입력 방지
    private bool isLoadingBattle; // 전투씬 로딩 중 중복 클릭 방지

    // 전투 시작에 필요한 View와 시스템을 받습니다.
    public SpirePopupController(SpirePopup view, PopupManager popupManager, GameFlowController gameFlowController)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.popupManager = popupManager ?? throw new ArgumentNullException(nameof(popupManager));
        this.gameFlowController = gameFlowController ?? throw new ArgumentNullException(nameof(gameFlowController));
    }

    // 스파이어 팝업의 전투 시작 요청을 구독합니다.
    public void Initialize()
    {
        if (isInitialized || isDisposed)
            return;

        view.FightRequested += OnFightRequested;
        isInitialized = true;
    }

    // 전투 진입은 한 번만 실행하고, 로비 팝업을 정리한 뒤 BattleScene 로딩을 요청합니다.
    private void OnFightRequested()
    {
        LoadBattleAsync().Forget();
    }

    // 비동기 전투 로딩 흐름입니다.
    private async UniTaskVoid LoadBattleAsync()
    {
        if (isDisposed || isLoadingBattle)
            return;

        isLoadingBattle = true;
        view.SetFightButtonInteractable(false);

        try
        {
            await popupManager.CloseAllAsync(PopupCloseReason.SceneChanged);
            await gameFlowController.LoadNightDefenseAsync();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SpirePopupController] 전투 씬 전환 중 예외 발생\n{exception}");

            if (!isDisposed)
                view.SetFightButtonInteractable(true);
        }
        finally
        {
            isLoadingBattle = false;
        }
    }

    // 팝업이 닫힐 때 이벤트 구독을 해제합니다.
    public void Dispose()
    {
        if (isDisposed)
            return;

        view.FightRequested -= OnFightRequested;
        isDisposed = true;
    }
}
