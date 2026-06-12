using UnityEngine;
using Cysharp.Threading.Tasks;

// LobbyScene의 Dynamic UI 초기화 지점입니다.
// 재화 HUD, 팝업 레이어, 토스트, 페이드 같은 값 변경 UI가 이 Root 아래에서 관리됩니다.
public sealed class LobbyDynamicUIRoot : MonoBehaviour
{
    [SerializeField] private Transform popupLayer; // 팝업이 생성될 부모 레이어
    [SerializeField] private CanvasGroup dimLayer; // 팝업 뒤 딤 처리 레이어
    [SerializeField] private Transform toastLayer; // 토스트 메시지 부모 레이어
    [SerializeField] private ScreenFadeView screenFadeView; // 씬 전환용 화면 페이드 View

    private bool isInitialized; // 씬 루트와 자체 초기화가 중복 호출되는 일을 막습니다.

    public Transform PopupLayer => popupLayer;
    public CanvasGroup DimLayer => dimLayer;
    public Transform ToastLayer => toastLayer;
    public ScreenFadeView ScreenFadeView => screenFadeView;

    // 로비 Dynamic UI에 현재 게임 상태를 전달하고 전역 UI 매니저에 씬 레이어를 등록합니다.
    // 팝업과 페이드는 전역에서 요청될 수 있지만 실제 표시는 현재 씬 Canvas 아래에서 처리합니다.
    public void Initialize(GameContext gameContext)
    {
        if (isInitialized)
        {
            Debug.Log("[LobbyDynamicUIRoot] 이미 초기화되어 중복 호출을 무시합니다.");
            return;
        }

        if (gameContext == null)
        {
            Debug.LogError("[LobbyDynamicUIRoot] GameContext가 없어 로비 동적 UI를 초기화할 수 없습니다.");
            return;
        }

        isInitialized = true;
        GameRoot.Instance?.PopupManager.RegisterSceneLayers(this, gameObject.scene.name, popupLayer, dimLayer, toastLayer);
        GameRoot.Instance?.ScreenFadeManager.RegisterSceneFade(this, gameObject.scene.name, screenFadeView);
    }

    private void Start()
    {
        InitializeWhenContextReadyAsync().Forget();
    }

    // 씬이 파괴될 때 현재 씬이 등록했던 동적 UI 참조를 해제합니다.
    // 다음 씬에서 이전 Canvas를 참조하는 일을 막기 위해 Root 생명주기에 맞춰 정리합니다.
    private void OnDestroy()
    {
        GameRoot.Instance?.PopupManager.UnregisterSceneLayers(this);
        GameRoot.Instance?.ScreenFadeManager.UnregisterSceneFade(this);
    }

    // LobbySceneRoot 연결이 누락되거나 씬 활성 순서가 흔들려도 팝업 레이어 등록을 보장합니다.
    private async UniTaskVoid InitializeWhenContextReadyAsync()
    {
        if (isInitialized)
            return;

        await UniTask.WaitUntil(() => GameRoot.Instance != null && GameRoot.Instance.Context != null);

        if (!isInitialized)
            Initialize(GameRoot.Instance.Context);
    }
}
