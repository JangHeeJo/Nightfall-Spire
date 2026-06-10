using System;
using UnityEngine;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;

// LobbyScene의 Static UI 초기화 지점입니다.
// 위치가 고정된 로비 UI들은 이 Root를 통해 GameContext를 전달받습니다.
public sealed class LobbyStaticUIRoot : MonoBehaviour
{
    [FormerlySerializedAs("currencyHud")]
    [SerializeField] private CurrencyHudView currencyHudView; // 로비 상단 재화 HUD View
    [SerializeField] private LobbyScreen lobbyScreen; // 로비 화면 단위 View
    [SerializeField] private LobbyCommandRoute[] commandRoutes; // 버튼 이름과 로비 액션을 연결하는 라우팅 목록

    private GameContext context; // 로비 UI가 참조할 현재 게임 상태
    private CurrencyHudPresenter currencyHudPresenter; // 재화 HUD MVP Presenter
    private LobbyPresenter lobbyPresenter; // 로비 화면 단위 Presenter
    private bool isInitialized; // 씬 루트와 자체 초기화가 중복 호출되는 일을 막습니다.

    [Serializable]
    private sealed class LobbyCommandRoute
    {
        [SerializeField] private string commandKey; // 클릭된 버튼 이름과 비교할 명령 키
        [SerializeField] private LobbyCommandAction action; // 명령 키가 실행할 로비 액션
        [SerializeField] private BasePopup popupPrefab; // 팝업 액션일 때 PopupManager가 생성할 프리팹
        [SerializeField] private bool useDim; // 팝업을 열 때 배경 DimLayer를 사용할지 여부

        public string CommandKey => commandKey;
        public LobbyCommandAction Action => action;
        public BasePopup PopupPrefab => popupPrefab;
        public bool UseDim => useDim;

        // 라우트가 현재 버튼 명령을 처리할 수 있는지 확인합니다.
        public bool Matches(string targetCommandKey)
        {
            return !string.IsNullOrWhiteSpace(commandKey)
                   && string.Equals(commandKey, targetCommandKey, StringComparison.Ordinal);
        }
    }

    private enum LobbyCommandAction
    {
        OpenPopup,
        ClosePopups,
        LoadNightDefense
    }

    // 로비 Static UI에 현재 게임 상태를 전달하고 필요한 Presenter를 생성합니다.
    // Root는 Presenter 조립만 담당하고, 실제 UI 표시 규칙은 Presenter에 맡깁니다.
    public void Initialize(GameContext gameContext)
    {
        if (isInitialized)
        {
            Debug.Log("[LobbyStaticUIRoot] 이미 초기화되어 중복 호출을 무시합니다.");
            return;
        }

        if (gameContext == null)
        {
            Debug.LogError("[LobbyStaticUIRoot] GameContext가 없어 로비 고정 UI를 초기화할 수 없습니다.");
            return;
        }

        context = gameContext;
        isInitialized = true;
        Debug.Log($"[LobbyStaticUIRoot] 초기화 시작. LobbyScreen: {lobbyScreen != null}, CommandRoutes: {(commandRoutes == null ? 0 : commandRoutes.Length)}");
        CreateCurrencyHudPresenter();
        CreateLobbyPresenter();
        Debug.Log("[LobbyStaticUIRoot] 초기화 완료");
    }

    private void Start()
    {
        InitializeWhenContextReadyAsync().Forget();
    }

    private void OnDestroy()
    {
        DisposePresenters();
    }

    // LobbySceneRoot 연결이 누락되거나 씬 활성 순서가 흔들려도 Presenter 구독을 보장합니다.
    private async UniTaskVoid InitializeWhenContextReadyAsync()
    {
        if (isInitialized)
            return;

        await UniTask.WaitUntil(() => GameRoot.Instance != null && GameRoot.Instance.Context != null);

        if (!isInitialized)
            Initialize(GameRoot.Instance.Context);
    }

    // CurrencyProgress(Model), CurrencyHudView(View), CurrencyHudPresenter(Presenter)를 조립합니다.
    // 고정 UI View는 씬에서 명시적으로 연결되어야 합니다.
    private void CreateCurrencyHudPresenter()
    {
        if (currencyHudView == null)
        {
            Debug.LogWarning("[LobbyStaticUIRoot] CurrencyHudView 참조가 비어 있어 재화 HUD Presenter 생성을 건너뜁니다.");
            return;
        }

        currencyHudPresenter?.Dispose();
        currencyHudPresenter = new CurrencyHudPresenter(context.CurrencyProgress, currencyHudView);
        currencyHudPresenter.Initialize();
    }

    // LobbyScreen(View)과 로비 명령 라우터를 화면 단위 Presenter로 연결합니다.
    // Presenter는 버튼별 기능을 모르고, Root가 명령 키를 해석합니다.
    private void CreateLobbyPresenter()
    {
        if (lobbyScreen == null)
        {
            Debug.LogError("[LobbyStaticUIRoot] LobbyScreen 참조가 비어 있습니다. 로비 고정 UI Root에서 화면 View를 직접 연결해야 합니다.");
            return;
        }

        lobbyPresenter?.Dispose();
        lobbyPresenter = new LobbyPresenter(lobbyScreen, ExecuteLobbyCommandAsync);
        lobbyPresenter.Initialize();
    }

    // 버튼 이름으로 들어온 로비 명령을 라우팅 테이블에서 찾아 실행합니다.
    private async UniTask<bool> ExecuteLobbyCommandAsync(string commandKey)
    {
        Debug.Log($"[LobbyStaticUIRoot] 명령 실행 요청: {commandKey}");

        LobbyCommandRoute route = FindCommandRoute(commandKey);
        if (route == null)
        {
            Debug.LogError($"[LobbyStaticUIRoot] 명령 라우트를 찾지 못했습니다. CommandKey: {commandKey}");
            return false;
        }

        Debug.Log($"[LobbyStaticUIRoot] 명령 라우트 발견. CommandKey: {route.CommandKey}, Action: {route.Action}, PopupPrefab: {(route.PopupPrefab == null ? "NULL" : route.PopupPrefab.name)}");
        return await ExecuteCommandRouteAsync(route);
    }

    // 라우팅 테이블에 등록된 액션 타입에 맞춰 실제 동작을 실행합니다.
    private async UniTask<bool> ExecuteCommandRouteAsync(LobbyCommandRoute route)
    {
        switch (route.Action)
        {
            case LobbyCommandAction.OpenPopup:
                Debug.Log($"[LobbyStaticUIRoot] 팝업 열기 실행: {route.CommandKey}");
                await OpenPopupAsync(route);
                return true;

            case LobbyCommandAction.ClosePopups:
                Debug.Log($"[LobbyStaticUIRoot] 팝업 전체 닫기 실행: {route.CommandKey}");
                await CloseLobbyPopupsAsync();
                return true;

            case LobbyCommandAction.LoadNightDefense:
                Debug.Log($"[LobbyStaticUIRoot] 전투 씬 이동 실행: {route.CommandKey}");
                await CloseLobbyPopupsAsync();
                return await GameRoot.Instance.GameFlowController.LoadNightDefenseAsync();

            default:
                Debug.LogWarning($"[LobbyStaticUIRoot] 처리하지 않는 로비 액션입니다. CommandKey: {route.CommandKey}, Action: {route.Action}");
                return false;
        }
    }

    // 버튼 이름에 맞는 로비 명령 라우트를 찾습니다.
    private LobbyCommandRoute FindCommandRoute(string commandKey)
    {
        if (commandRoutes == null || string.IsNullOrWhiteSpace(commandKey))
            return null;

        for (int i = 0; i < commandRoutes.Length; i++)
        {
            LobbyCommandRoute route = commandRoutes[i];
            if (route != null && route.Matches(commandKey))
                return route;
        }

        return null;
    }

    // 라우팅 테이블에 등록된 팝업을 PopupManager 레이어 아래에 생성합니다.
    private async UniTask OpenPopupAsync(LobbyCommandRoute route)
    {
        BasePopup popupPrefab = route.PopupPrefab;
        if (popupPrefab == null)
        {
            Debug.LogError($"[LobbyStaticUIRoot] {route.CommandKey} 팝업 프리팹 참조가 비어 있습니다.");
            return;
        }

        string popupKey = popupPrefab.name;
        PopupRequest<BasePopup> request = new PopupRequest<BasePopup>(
            popupKey,
            popupPrefab,
            PopupOpenPolicy.SingleInstance,
            PopupPriority.Normal,
            route.UseDim);

        Debug.Log($"[LobbyStaticUIRoot] PopupManager.OpenAsync 호출. Key: {popupKey}, Prefab: {popupPrefab.name}, UseDim: {route.UseDim}");
        await GameRoot.Instance.PopupManager.OpenAsync(request);
    }

    // Spire 탭이나 씬 전환 전에 로비 팝업을 모두 닫습니다.
    private UniTask CloseLobbyPopupsAsync()
    {
        return GameRoot.Instance.PopupManager.CloseAllAsync();
    }

    // 씬이 파괴될 때 Presenter 구독을 해제합니다.
    // R3 구독은 Presenter가 소유하므로 Root 생명주기에 맞춰 정리합니다.
    private void DisposePresenters()
    {
        currencyHudPresenter?.Dispose();
        currencyHudPresenter = null;

        lobbyPresenter?.Dispose();
        lobbyPresenter = null;
    }
}
