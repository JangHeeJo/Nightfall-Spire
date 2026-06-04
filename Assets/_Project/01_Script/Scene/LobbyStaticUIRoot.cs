using UnityEngine;

// LobbyScene의 Static UI 초기화 지점입니다.
// 위치가 고정된 로비 UI들은 이 Root를 통해 GameContext를 전달받습니다.
public sealed class LobbyStaticUIRoot : MonoBehaviour
{
    [SerializeField] private CurrencyHud currencyHud; // 로비 상단 재화 HUD

    private GameContext context; // 로비 UI가 참조할 현재 게임 상태
    private CurrencyHudBinder currencyHudBinder; // 재화 모델과 HUD View를 연결하는 Binder

    // 로비 Static UI에 현재 게임 상태를 전달하고 필요한 Binder를 생성합니다.
    // View는 화면 표시만 맡고, Binder가 Model 구독과 View 갱신을 담당합니다.
    public void Initialize(GameContext gameContext)
    {
        context = gameContext;
        BindCurrencyHud();
    }

    private void OnDestroy()
    {
        DisposeBinders();
    }

    // CurrencyProgress와 CurrencyHud를 연결합니다.
    // Inspector 연결이 비어 있으면 자식 오브젝트에서 CurrencyHud를 찾아 초기 세팅 단계의 실수를 줄입니다.
    private void BindCurrencyHud()
    {
        currencyHud ??= GetComponentInChildren<CurrencyHud>(true);

        if (currencyHud == null)
        {
            Debug.LogWarning("[LobbyStaticUIRoot] CurrencyHud가 연결되지 않았습니다.");
            return;
        }

        currencyHudBinder?.Dispose();
        currencyHudBinder = new CurrencyHudBinder(context.CurrencyProgress, currencyHud);
    }

    // 씬이 파괴될 때 Binder 구독을 해제합니다.
    // R3 구독이 남아 있으면 파괴된 View를 갱신하려 할 수 있으므로 Root 생명주기에 맞춰 정리합니다.
    private void DisposeBinders()
    {
        currencyHudBinder?.Dispose();
        currencyHudBinder = null;
    }
}
