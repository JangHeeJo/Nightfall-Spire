using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Cysharp.Threading.Tasks;
using R3;

// LobbyScene의 Static UI 초기화 지점입니다.
// 위치가 고정된 로비 UI들은 이 Root를 통해 GameContext를 전달받습니다.
public sealed class LobbyStaticUIRoot : MonoBehaviour
{
    private const string MagicCommandKey = "BottomButton_Magic"; // 마법/소환 계열 하단 탭
    private const string HeroCommandKey = "BottomButton_Hero"; // 영웅 목록 하단 탭
    private const string SpireCommandKey = "BottomButton_Spire"; // 성채/스파이어 하단 탭
    private const string BattleCommandKey = "BottomButton_Battle"; // 전투 시작 하단 탭
    private const string ShopCommandKey = "BottomButton_Shop"; // 상점 하단 탭

    [FormerlySerializedAs("currencyHud")]
    [SerializeField] private CurrencyHudView currencyHudView; // 로비 상단 재화 HUD View
    [SerializeField] private LobbyScreen lobbyScreen; // 로비 화면 단위 View
    [FormerlySerializedAs("defaultContentPopup")]
    [SerializeField] private BasePopup defaultSpirePopup; // 로비에 열린 Content 팝업이 없을 때 자동으로 띄울 Spire_Popup
    [SerializeField] private LobbyCommandRoute[] commandRoutes; // 버튼 이름과 로비 액션을 연결하는 라우팅 목록

    private GameContext context; // 로비 UI가 참조할 현재 게임 상태
    private CurrencyHudController currencyHudController; // 재화 HUD Controller
    private LobbyController lobbyController; // 로비 화면 단위 Controller
    private LobbyNavigationController lobbyNavigationController; // 로비 버튼 명령 실행 Controller
    private LobbyTabNotificationController lobbyTabNotificationController; // 하단 탭 알림 점 Controller
    private readonly List<IDisposable> commandUnlockSubscriptions = new(); // 하단 탭 해금 상태 변경 구독 목록
    private bool isInitialized; // 씬 루트와 자체 초기화가 중복 호출되는 일을 막습니다.

    [Serializable]
    private sealed class LobbyCommandRoute : ILobbyCommandRoute
    {
        [SerializeField] private string commandKey; // 클릭된 버튼 이름과 비교할 명령 키
        [SerializeField] private LobbyCommandAction action; // 명령 키가 실행할 로비 액션
        [SerializeField] private BasePopup popupPrefab; // 팝업 액션일 때 PopupManager가 생성할 프리팹
        [SerializeField] private PopupLayerSlot popupLayerSlot; // 팝업을 열 논리 슬롯
        [SerializeField] private bool useDim; // 팝업을 열 때 배경 DimLayer를 사용할지 여부

        public string CommandKey => commandKey;
        public LobbyCommandAction Action => action;
        public BasePopup PopupPrefab => popupPrefab;
        public PopupLayerSlot PopupLayerSlot => popupLayerSlot;
        public bool UseDim => useDim;
    }

    // 로비 Static UI에 현재 게임 상태를 전달하고 필요한 Controller를 생성합니다.
    // Root는 씬 오브젝트 연결과 라우트 테이블을 보관하고, 실제 입력 흐름은 Controller가 처리합니다.
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
        CreateCurrencyHudController();
        CreateLobbyNavigationController();
        CreateLobbyController();
        CreateLobbyCommandUnlockBindings();
        CreateLobbyTabNotificationController();
        Debug.Log("[LobbyStaticUIRoot] 초기화 완료");
    }

    private void Start()
    {
        InitializeWhenContextReadyAsync().Forget();
    }

    private void OnDestroy()
    {
        DisposeControllers();
    }

    // LobbySceneRoot 연결이 누락되거나 씬 활성 순서가 흔들려도 Controller 구독을 보장합니다.
    private async UniTaskVoid InitializeWhenContextReadyAsync()
    {
        if (isInitialized)
            return;

        await UniTask.WaitUntil(() => GameRoot.Instance != null && GameRoot.Instance.Context != null);

        if (!isInitialized)
            Initialize(GameRoot.Instance.Context);
    }

    // CurrencyProgress(Model), CurrencyHudView(View), CurrencyHudController(Controller)를 조립합니다.
    // 고정 UI View는 씬에서 명시적으로 연결되어야 합니다.
    private void CreateCurrencyHudController()
    {
        if (currencyHudView == null)
        {
            Debug.LogWarning("[LobbyStaticUIRoot] CurrencyHudView 참조가 비어 있어 재화 HUD Controller 생성을 건너뜁니다.");
            return;
        }

        currencyHudController?.Dispose();
        currencyHudController = new CurrencyHudController(context.CurrencyProgress, currencyHudView);
        currencyHudController.Initialize();
    }

    // LobbyScreen(View)과 로비 명령 라우터를 화면 단위 Controller로 연결합니다.
    // Controller는 버튼 이름을 전달받고, Root의 라우트 테이블이 실제 액션을 해석합니다.
    private void CreateLobbyController()
    {
        if (lobbyScreen == null)
        {
            Debug.LogError("[LobbyStaticUIRoot] LobbyScreen 참조가 비어 있습니다. 로비 고정 UI Root에서 화면 View를 직접 연결해야 합니다.");
            return;
        }

        lobbyController?.Dispose();
        lobbyController = new LobbyController(lobbyScreen, lobbyNavigationController.ExecuteCommandAsync);
        lobbyController.Initialize();
    }

    // 로비 하단 탭의 빨간 알림 점을 현재 진행 데이터와 연결합니다.
    private void CreateLobbyTabNotificationController()
    {
        if (lobbyScreen == null)
            return;

        lobbyTabNotificationController?.Dispose();
        lobbyTabNotificationController = new LobbyTabNotificationController(context, lobbyScreen);
        lobbyTabNotificationController.Initialize();
    }

    // 로비 Static UI 버튼의 해금 상태를 현재 진행 데이터와 연결합니다.
    // 잠긴 버튼은 View 단계에서 interactable이 꺼져 눌림 연출과 명령 실행이 모두 막힙니다.
    private void CreateLobbyCommandUnlockBindings()
    {
        if (lobbyScreen == null)
            return;

        ClearLobbyCommandUnlockBindings();
        ApplyLobbyCommandUnlocks();

        commandUnlockSubscriptions.Add(context.DayProgress.MagicLibraryUnlocked.Subscribe(_ => ApplyLobbyCommandUnlocks()));
        commandUnlockSubscriptions.Add(context.DayProgress.CitadelFloorCount.Subscribe(_ => ApplyLobbyCommandUnlocks()));
    }

    // 현재 게임 진행 기준으로 각 하단 탭의 사용 가능 여부를 갱신합니다.
    private void ApplyLobbyCommandUnlocks()
    {
        if (lobbyScreen == null || context == null)
            return;

        lobbyScreen.SetCommandUnlocked(MagicCommandKey, context.DayProgress.MagicLibraryUnlocked.Value);
        lobbyScreen.SetCommandUnlocked(HeroCommandKey, true);
        lobbyScreen.SetCommandUnlocked(SpireCommandKey, true);
        lobbyScreen.SetCommandUnlocked(BattleCommandKey, true);
        lobbyScreen.SetCommandUnlocked(ShopCommandKey, false);
    }

    // 로비 버튼 명령을 팝업/씬 전환으로 바꿔 실행할 Controller를 생성합니다.
    private void CreateLobbyNavigationController()
    {
        lobbyNavigationController?.Dispose();
        if (GameRoot.Instance == null || GameRoot.Instance.PopupManager == null || GameRoot.Instance.GameFlowController == null)
        {
            Debug.LogError("[LobbyStaticUIRoot] 로비 명령 Controller를 만들 수 없습니다. GameRoot 핵심 시스템이 준비되지 않았습니다.");
            return;
        }

        lobbyNavigationController = new LobbyNavigationController(
            commandRoutes,
            defaultSpirePopup,
            GameRoot.Instance.PopupManager,
            GameRoot.Instance.GameFlowController);
        lobbyNavigationController.Initialize();
    }

    // 씬이 파괴될 때 Controller 구독을 해제합니다.
    // R3 구독은 Controller가 소유하므로 Root 생명주기에 맞춰 정리합니다.
    private void DisposeControllers()
    {
        ClearLobbyCommandUnlockBindings();

        currencyHudController?.Dispose();
        currencyHudController = null;

        lobbyController?.Dispose();
        lobbyController = null;

        lobbyNavigationController?.Dispose();
        lobbyNavigationController = null;

        lobbyTabNotificationController?.Dispose();
        lobbyTabNotificationController = null;
    }

    // 하단 탭 해금 상태 구독을 정리합니다.
    private void ClearLobbyCommandUnlockBindings()
    {
        for (int i = 0; i < commandUnlockSubscriptions.Count; i++)
            commandUnlockSubscriptions[i]?.Dispose();

        commandUnlockSubscriptions.Clear();
    }
}
