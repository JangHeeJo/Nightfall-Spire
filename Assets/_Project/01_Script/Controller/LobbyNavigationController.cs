using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 로비 버튼 명령을 실제 화면 이동과 팝업 동작으로 변환하는 Controller입니다.
// LobbyStaticUIRoot는 씬 오브젝트를 조립만 하고, 로비 명령 실행 정책은 이 클래스가 소유합니다.
public sealed class LobbyNavigationController : IDisposable
{
    private readonly IReadOnlyList<ILobbyCommandRoute> commandRoutes; // 인스펙터에서 조립된 로비 버튼 라우트 목록
    private readonly BasePopup defaultSpirePopup; // 로비 Content 슬롯이 비었을 때 자동으로 열 기본 팝업
    private readonly PopupManager popupManager; // 로비 팝업 생성과 슬롯 관리를 담당하는 관리자
    private readonly GameFlowController gameFlowController; // 로비에서 전투 씬으로 넘어가는 게임 흐름 Controller
    private readonly Dictionary<string, ILobbyCommandRoute> routeByCommandKey = new(); // 버튼 이름별 라우트 빠른 조회 캐시

    private bool isDisposed; // 파괴 이후 비동기 흐름을 막습니다.
    private bool isEnsuringDefaultSpirePopup; // 기본 Spire 팝업 중복 생성을 막습니다.

    // 로비 명령 실행에 필요한 라우트 목록, 기본 팝업, 실행 시스템을 받습니다.
    public LobbyNavigationController(
        IReadOnlyList<ILobbyCommandRoute> commandRoutes,
        BasePopup defaultSpirePopup,
        PopupManager popupManager,
        GameFlowController gameFlowController)
    {
        this.commandRoutes = commandRoutes ?? Array.Empty<ILobbyCommandRoute>();
        this.defaultSpirePopup = defaultSpirePopup;
        this.popupManager = popupManager ?? throw new ArgumentNullException(nameof(popupManager));
        this.gameFlowController = gameFlowController ?? throw new ArgumentNullException(nameof(gameFlowController));
    }

    // 명령 라우트 캐시를 만들고 Dynamic UI 레이어 준비 후 기본 Spire 팝업을 보장합니다.
    public void Initialize()
    {
        BuildCommandRouteCache();
        EnsureDefaultSpirePopupWhenReadyAsync().Forget();
    }

    // 로비 View에서 전달된 버튼 이름을 실제 로비 명령으로 실행합니다.
    public async UniTask<bool> ExecuteCommandAsync(string commandKey)
    {
        if (isDisposed)
            return false;

        ILobbyCommandRoute route = FindCommandRoute(commandKey);
        if (route == null)
        {
            Debug.LogError($"[LobbyNavigationController] 명령 라우트를 찾지 못했습니다. CommandKey: {commandKey}");
            return false;
        }

        return await ExecuteCommandRouteAsync(route);
    }

    // 로비 Content 슬롯이 비어 있으면 기본 Spire 팝업을 엽니다.
    public async UniTask EnsureDefaultSpirePopupAsync()
    {
        if (isDisposed || isEnsuringDefaultSpirePopup)
            return;

        if (!popupManager.HasActiveLayers)
            return;

        if (popupManager.HasOpenPopupInSlot(PopupLayerSlot.Content))
            return;

        if (defaultSpirePopup == null)
        {
            Debug.LogError("[LobbyNavigationController] Spire_Popup 참조가 비어 있습니다.");
            return;
        }

        isEnsuringDefaultSpirePopup = true;

        try
        {
            PopupRequest<BasePopup> request = new PopupRequest<BasePopup>(
                defaultSpirePopup.name,
                defaultSpirePopup,
                PopupOpenPolicy.SingleInstance,
                PopupLayerSlot.Content,
                PopupPriority.Normal,
                false);

            await popupManager.OpenAsync(request);
        }
        finally
        {
            isEnsuringDefaultSpirePopup = false;
        }
    }

    // 인스펙터 라우트 목록을 버튼 이름 기준 Dictionary로 변환합니다.
    private void BuildCommandRouteCache()
    {
        routeByCommandKey.Clear();

        for (int i = 0; i < commandRoutes.Count; i++)
        {
            ILobbyCommandRoute route = commandRoutes[i];

            if (route == null || string.IsNullOrWhiteSpace(route.CommandKey))
                continue;

            if (routeByCommandKey.ContainsKey(route.CommandKey))
            {
                Debug.LogWarning($"[LobbyNavigationController] 중복 로비 명령 라우트를 무시합니다. CommandKey: {route.CommandKey}");
                continue;
            }

            routeByCommandKey.Add(route.CommandKey, route);
        }
    }

    // 명령 키에 맞는 라우트를 찾습니다.
    private ILobbyCommandRoute FindCommandRoute(string commandKey)
    {
        if (string.IsNullOrWhiteSpace(commandKey))
            return null;

        return routeByCommandKey.TryGetValue(commandKey, out ILobbyCommandRoute route) ? route : null;
    }

    // 라우트에 지정된 액션 타입에 맞는 로비 동작을 실행합니다.
    private async UniTask<bool> ExecuteCommandRouteAsync(ILobbyCommandRoute route)
    {
        switch (route.Action)
        {
            case LobbyCommandAction.OpenPopup:
                await OpenPopupAsync(route);
                return true;

            case LobbyCommandAction.CloseContent:
                await CloseLobbyContentAsync();
                await EnsureDefaultSpirePopupAsync();
                return true;

            case LobbyCommandAction.LoadNightDefense:
                await CloseLobbyPopupsAsync();
                return await gameFlowController.LoadNightDefenseAsync();

            case LobbyCommandAction.CloseAllPopups:
                await CloseLobbyPopupsAsync();
                return true;

            default:
                Debug.LogWarning($"[LobbyNavigationController] 처리하지 않는 로비 액션입니다. CommandKey: {route.CommandKey}, Action: {route.Action}");
                return false;
        }
    }

    // 라우트에 연결된 팝업 프리팹을 PopupManager에 요청합니다.
    private async UniTask OpenPopupAsync(ILobbyCommandRoute route)
    {
        BasePopup popupPrefab = route.PopupPrefab;
        if (popupPrefab == null)
        {
            Debug.LogError($"[LobbyNavigationController] {route.CommandKey} 팝업 프리팹 참조가 비어 있습니다.");
            return;
        }

        PopupRequest<BasePopup> request = new PopupRequest<BasePopup>(
            popupPrefab.name,
            popupPrefab,
            PopupOpenPolicy.SingleInstance,
            route.PopupLayerSlot,
            PopupPriority.Normal,
            route.UseDim);

        await popupManager.OpenAsync(request);
    }

    // 하단 탭으로 열린 Content 팝업만 닫습니다.
    private UniTask CloseLobbyContentAsync()
    {
        return popupManager.CloseContentAsync();
    }

    // 모든 로비 팝업을 닫습니다.
    private UniTask CloseLobbyPopupsAsync()
    {
        return popupManager.CloseAllAsync();
    }

    // Dynamic UI 레이어가 늦게 등록되는 씬 초기화 순서를 기다린 뒤 기본 Spire 팝업을 엽니다.
    private async UniTaskVoid EnsureDefaultSpirePopupWhenReadyAsync()
    {
        await UniTask.WaitUntil(() => isDisposed
                                    || popupManager.HasActiveLayers);

        if (!isDisposed)
            await EnsureDefaultSpirePopupAsync();
    }

    // 씬 종료 시 이후 비동기 콜백을 무시하도록 표시합니다.
    public void Dispose()
    {
        isDisposed = true;
    }
}

// 로비 버튼 하나가 어떤 동작을 실행할지 정의합니다.
public interface ILobbyCommandRoute
{
    string CommandKey { get; }
    LobbyCommandAction Action { get; }
    BasePopup PopupPrefab { get; }
    PopupLayerSlot PopupLayerSlot { get; }
    bool UseDim { get; }
}

// 로비 버튼 명령이 실행할 수 있는 액션 종류입니다.
public enum LobbyCommandAction
{
    OpenPopup,
    CloseContent,
    LoadNightDefense,
    CloseAllPopups
}
