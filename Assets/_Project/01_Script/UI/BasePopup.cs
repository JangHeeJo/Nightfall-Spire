using Cysharp.Threading.Tasks;
using UnityEngine;

// 모든 팝업 프리팹이 상속받는 공통 부모입니다.
// 팝업 생성 위치, 딤 처리, 스택 관리는 PopupManager가 담당하고
// 개별 팝업은 여기서 열림/닫힘 생명주기만 맞춰서 확장합니다.
public class BasePopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup; // 팝업 전체 표시와 입력을 제어합니다.

    private PopupManager owner; // 이 팝업을 생성한 PopupManager

    public bool IsOpen { get; private set; } // 팝업이 현재 열린 상태인지 나타냅니다.
    public CanvasGroup CanvasGroup => canvasGroup;

    // PopupManager가 팝업을 생성한 직후 호출합니다.
    // 팝업이 자기 생성자를 직접 가질 수 없으므로, 공통 의존성은 이 함수에서 받습니다.
    public void Initialize(PopupManager popupManager)
    {
        owner = popupManager;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetVisible(false);
        OnInitialized();
    }

    // 팝업이 처음 만들어졌을 때 개별 팝업이 추가 초기화를 할 수 있는 자리입니다.
    // 예를 들어 버튼 이벤트 연결이나 텍스트 주입 같은 작업을 자식 클래스에서 처리합니다.
    protected virtual void OnInitialized()
    {
    }

    // PopupManager가 팝업을 화면에 표시할 때 호출합니다.
    // 실제 연출은 PopupTweenManager가 담당하고, 이 함수는 공통 상태를 정리합니다.
    public virtual async UniTask OpenAsync(PopupTweenManager tweenManager)
    {
        IsOpen = true;
        SetVisible(true);
        await tweenManager.PlayOpenAsync(this);
        OnOpened();
    }

    // 팝업이 완전히 열린 뒤 개별 팝업이 후처리를 할 수 있는 자리입니다.
    // 첫 포커스 지정이나 안내 연출 시작 같은 작업을 자식 클래스에서 처리합니다.
    protected virtual void OnOpened()
    {
    }

    // 외부 버튼이나 닫기 입력에서 호출하는 닫기 요청 함수입니다.
    // 팝업 제거 정책은 PopupManager가 갖고 있으므로, 팝업 자신은 관리자에게 닫기를 요청합니다.
    public UniTask RequestCloseAsync()
    {
        if (owner == null)
            return UniTask.CompletedTask;

        return owner.CloseAsync(this);
    }

    // PopupManager가 팝업을 닫을 때 호출합니다.
    // 닫기 연출이 끝난 뒤 비활성 상태로 되돌리고 후처리 훅을 실행합니다.
    public virtual async UniTask CloseAsync(PopupTweenManager tweenManager)
    {
        if (!IsOpen)
            return;

        await tweenManager.PlayCloseAsync(this);
        IsOpen = false;
        SetVisible(false);
        OnClosed();
    }

    // 팝업이 완전히 닫힌 뒤 개별 팝업이 정리 작업을 할 수 있는 자리입니다.
    // R3 구독이나 임시 상태 해제가 필요하면 자식 클래스에서 처리합니다.
    protected virtual void OnClosed()
    {
    }

    // CanvasGroup 기준으로 팝업 표시와 입력 가능 여부를 함께 바꿉니다.
    // 팝업이 닫혀 있을 때 뒤쪽 UI 입력을 막지 않도록 raycast도 같이 제어합니다.
    protected void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
