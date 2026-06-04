using UnityEngine;

// LobbyScene의 Dynamic UI 초기화 지점입니다.
// 재화 HUD, 팝업 레이어, 토스트, 페이드 같은 값 변경 UI가 이 Root 아래에서 관리됩니다.
public sealed class LobbyDynamicUIRoot : MonoBehaviour
{
    [SerializeField] private Transform popupLayer; // 팝업이 생성될 부모 레이어
    [SerializeField] private CanvasGroup dimLayer; // 팝업 뒤 딤 처리 레이어
    [SerializeField] private Transform toastLayer; // 토스트 메시지 부모 레이어

    private GameContext context; // 동적 UI가 참조할 현재 게임 상태

    public Transform PopupLayer => popupLayer;
    public CanvasGroup DimLayer => dimLayer;
    public Transform ToastLayer => toastLayer;

    public void Initialize(GameContext gameContext)
    {
        context = gameContext;
    }
}
