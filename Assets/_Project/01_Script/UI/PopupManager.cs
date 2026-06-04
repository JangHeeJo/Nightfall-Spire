using UnityEngine;

// 현재 씬의 팝업 레이어 참조를 보관합니다.
// 실제 팝업 생성/닫기 연출은 다음 단계에서 이 레이어 정보를 기준으로 붙입니다.
public sealed class PopupManager
{
    private Object registeredOwner; // 레이어를 등록한 씬 Root
    private string registeredSceneName; // 디버깅용 씬 이름

    public Transform PopupLayer { get; private set; } // 팝업 생성 부모
    public CanvasGroup DimLayer { get; private set; } // 팝업 뒤 딤 처리
    public Transform ToastLayer { get; private set; } // 토스트 생성 부모

    public bool HasActiveLayers => PopupLayer != null && DimLayer != null && ToastLayer != null;

    public void RegisterSceneLayers(Object owner, string sceneName, Transform popupLayer, CanvasGroup dimLayer, Transform toastLayer)
    {
        registeredOwner = owner;
        registeredSceneName = sceneName;
        PopupLayer = popupLayer;
        DimLayer = dimLayer;
        ToastLayer = toastLayer;

        ResetDimLayer();
        Debug.Log($"[PopupManager] {registeredSceneName} UI 레이어 등록 완료");
    }

    public void UnregisterSceneLayers(Object owner)
    {
        if (registeredOwner != owner)
            return;

        registeredOwner = null;
        registeredSceneName = string.Empty;
        PopupLayer = null;
        DimLayer = null;
        ToastLayer = null;

        Debug.Log("[PopupManager] UI 레이어 등록 해제");
    }

    private void ResetDimLayer()
    {
        if (DimLayer == null)
            return;

        DimLayer.alpha = 0f;
        DimLayer.blocksRaycasts = false;
        DimLayer.interactable = false;
    }
}
