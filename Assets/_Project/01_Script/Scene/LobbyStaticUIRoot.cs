using UnityEngine;
using UnityEngine.Serialization;

// LobbyScene의 Static UI 초기화 지점입니다.
// 위치가 고정된 로비 UI들은 이 Root를 통해 GameContext를 전달받습니다.
public sealed class LobbyStaticUIRoot : MonoBehaviour
{
    [FormerlySerializedAs("currencyHud")]
    [SerializeField] private CurrencyHudView currencyHudView; // 로비 상단 재화 HUD View
    [SerializeField] private LobbyScreen lobbyScreen; // 로비 화면 단위 View

    private GameContext context; // 로비 UI가 참조할 현재 게임 상태
    private CurrencyHudPresenter currencyHudPresenter; // 재화 HUD MVP Presenter
    private LobbyPresenter lobbyPresenter; // 로비 화면 단위 Presenter

    // 로비 Static UI에 현재 게임 상태를 전달하고 필요한 Presenter를 생성합니다.
    // Root는 Presenter 조립만 담당하고, 실제 UI 표시 규칙은 Presenter에 맡깁니다.
    public void Initialize(GameContext gameContext)
    {
        context = gameContext;
        CreateCurrencyHudPresenter();
        CreateLobbyPresenter();
    }

    private void OnDestroy()
    {
        DisposePresenters();
    }

    // CurrencyProgress(Model), CurrencyHudView(View), CurrencyHudPresenter(Presenter)를 조립합니다.
    // Lobby_Default 프리팹의 ResourceBar를 실제 View로 연결하기 전까지는 HUD Presenter 생성을 건너뜁니다.
    private void CreateCurrencyHudPresenter()
    {
        currencyHudView ??= GetComponentInChildren<CurrencyHudView>(true);

        if (currencyHudView == null)
            return;

        currencyHudPresenter?.Dispose();
        currencyHudPresenter = new CurrencyHudPresenter(context.CurrencyProgress, currencyHudView);
        currencyHudPresenter.Initialize();
    }

    // LobbyScreen(View)과 GameFlowController(Model/Flow)를 화면 단위 Presenter로 연결합니다.
    // 버튼마다 Presenter를 만들지 않고 로비 화면의 주요 액션을 LobbyPresenter가 관리합니다.
    private void CreateLobbyPresenter()
    {
        lobbyScreen ??= FindLobbyScreen();

        if (lobbyScreen == null)
        {
            Debug.LogWarning("[LobbyStaticUIRoot] LobbyScreen을 만들 수 없습니다.");
            return;
        }

        lobbyPresenter?.Dispose();
        lobbyPresenter = new LobbyPresenter(lobbyScreen, GameRoot.Instance.GameFlowController.LoadNightDefenseAsync);
        lobbyPresenter.Initialize();
    }

    // LobbyStaticUIRoot와 같은 Canvas에 LobbyScreen 컴포넌트를 보장합니다.
    // 로비 화면 입력은 이 컴포넌트 하나에서 모으고, 세부 버튼 스크립트는 만들지 않습니다.
    private LobbyScreen FindLobbyScreen()
    {
        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        return searchRoot.GetComponent<LobbyScreen>() ?? searchRoot.gameObject.AddComponent<LobbyScreen>();
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
