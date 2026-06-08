using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 낮 성장 화면에서 업그레이드 구매를 확정하는 팝업입니다.
// 비용, 레벨, 버튼 배치는 Unity에서 만들고, 이 스크립트는 구매 정보와 확정 결과만 관리합니다.
public sealed class UpgradePurchasePopup : BasePopup
{
    public const string PopupKey = "UpgradePurchase"; // PopupManager 중복 정책에 사용할 고정 Key

    [SerializeField] private Button purchaseButton; // 구매 확정 버튼
    [SerializeField] private Button cancelButton; // 구매 취소 버튼
    [SerializeField] private Button closeButton; // 닫기 버튼

    private UpgradePurchasePopupPayload payload; // 현재 표시 중인 업그레이드 구매 정보

    public UpgradePurchasePopupPayload Payload => payload;

    // PopupManager가 팝업을 만든 뒤 버튼 이벤트를 연결합니다.
    protected override void OnInitialized()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(HandlePurchaseClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(HandleCancelClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(HandleCancelClicked);
    }

    // 구매하려는 업그레이드 정보와 비용을 팝업에 주입합니다.
    public void Configure(UpgradePurchasePopupPayload nextPayload)
    {
        payload = nextPayload;
        RefreshView();
    }

    // 외부에서 구매 확정을 직접 호출해야 할 때 사용합니다.
    public UniTask ConfirmPurchaseAsync()
    {
        UpgradePurchasePopupResult result = new UpgradePurchasePopupResult(payload.Category, payload.UpgradeId);
        return RequestCloseAsync(PopupCloseReason.Confirmed, result);
    }

    // 외부에서 구매 취소를 직접 호출해야 할 때 사용합니다.
    public UniTask CancelPurchaseAsync()
    {
        return RequestCloseAsync(PopupCloseReason.Cancelled);
    }

    // 팝업이 닫힐 때 Unity Button 이벤트를 정리합니다.
    protected override void OnClosed()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(HandleCancelClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleCancelClicked);
    }

    // 구매 정보가 바뀐 뒤 화면 갱신을 넣을 자리입니다.
    private void RefreshView()
    {
        // 실제 비용 텍스트, 아이콘, 버튼 상태 연결은 Unity UI 작업 단계에서 추가합니다.
    }

    // 구매 버튼 입력을 확정 결과로 변환합니다.
    private void HandlePurchaseClicked()
    {
        ConfirmPurchaseAsync().Forget();
    }

    // 취소 또는 닫기 버튼 입력을 취소 결과로 변환합니다.
    private void HandleCancelClicked()
    {
        CancelPurchaseAsync().Forget();
    }
}
