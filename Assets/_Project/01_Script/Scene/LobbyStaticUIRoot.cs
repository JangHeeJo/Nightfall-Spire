using UnityEngine;
using UnityEngine.Serialization;

// LobbyScene의 Static UI 초기화 지점입니다.
// 위치가 고정된 로비 UI들은 이 Root를 통해 GameContext를 전달받습니다.
public sealed class LobbyStaticUIRoot : MonoBehaviour
{
    [FormerlySerializedAs("currencyHud")]
    [SerializeField] private CurrencyHudView currencyHudView; // 로비 상단 재화 HUD View
    [SerializeField] private FightStartView fightStartView; // 밤 방어 시작 버튼 View

    private GameContext context; // 로비 UI가 참조할 현재 게임 상태
    private CurrencyHudPresenter currencyHudPresenter; // 재화 HUD MVP Presenter
    private FightStartPresenter fightStartPresenter; // 밤 방어 시작 버튼 Presenter

    // 로비 Static UI에 현재 게임 상태를 전달하고 필요한 Presenter를 생성합니다.
    // Root는 Presenter 조립만 담당하고, 실제 UI 표시 규칙은 Presenter에 맡깁니다.
    public void Initialize(GameContext gameContext)
    {
        context = gameContext;
        CreateCurrencyHudPresenter();
        CreateFightStartPresenter();
    }

    private void OnDestroy()
    {
        DisposePresenters();
    }

    // CurrencyProgress(Model), CurrencyHudView(View), CurrencyHudPresenter(Presenter)를 조립합니다.
    // Inspector 연결이 비어 있으면 자식 오브젝트에서 CurrencyHudView를 찾아 초기 세팅 단계의 실수를 줄입니다.
    private void CreateCurrencyHudPresenter()
    {
        currencyHudView ??= GetComponentInChildren<CurrencyHudView>(true);

        if (currencyHudView == null)
        {
            Debug.LogWarning("[LobbyStaticUIRoot] CurrencyHudView가 연결되지 않았습니다.");
            return;
        }

        currencyHudPresenter?.Dispose();
        currencyHudPresenter = new CurrencyHudPresenter(context.CurrencyProgress, currencyHudView);
        currencyHudPresenter.Initialize();
    }

    // FightStartView(View)와 GameFlowController(Model/Flow)를 Presenter로 연결합니다.
    // 씬에 스크립트가 아직 직접 붙어 있지 않으면 오브젝트 이름으로 찾아 자동 추가합니다.
    private void CreateFightStartPresenter()
    {
        fightStartView ??= GetComponentInChildren<FightStartView>(true);
        fightStartView ??= FindFightStartViewInChildren();

        if (fightStartView == null)
        {
            Debug.LogWarning("[LobbyStaticUIRoot] FightStartView를 찾을 수 없습니다.");
            return;
        }

        fightStartPresenter?.Dispose();
        fightStartPresenter = new FightStartPresenter(fightStartView, GameRoot.Instance.GameFlowController.LoadNightDefenseAsync);
        fightStartPresenter.Initialize();
    }

    // 기존 씬에 있는 FightStartView 이름의 오브젝트를 찾아 View 컴포넌트를 보장합니다.
    // FightStartView는 LobbyStaticUIRoot의 자식이 아니라 같은 Canvas 아래 형제일 수 있으므로 부모 Canvas부터 찾습니다.
    private FightStartView FindFightStartViewInChildren()
    {
        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name != "FightStartView")
                continue;

            return children[i].GetComponent<FightStartView>() ?? children[i].gameObject.AddComponent<FightStartView>();
        }

        return null;
    }

    // 씬이 파괴될 때 Presenter 구독을 해제합니다.
    // R3 구독은 Presenter가 소유하므로 Root 생명주기에 맞춰 정리합니다.
    private void DisposePresenters()
    {
        currencyHudPresenter?.Dispose();
        currencyHudPresenter = null;

        fightStartPresenter?.Dispose();
        fightStartPresenter = null;
    }
}
