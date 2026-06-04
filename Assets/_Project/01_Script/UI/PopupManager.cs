using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 현재 씬의 팝업 레이어와 열린 팝업 스택을 관리합니다.
// 팝업 요청은 전역에서 들어오지만 실제 생성 위치는 현재 씬의 PopupLayer 아래로 제한합니다.
public sealed class PopupManager
{
    private readonly List<BasePopup> openedPopups = new(); // 현재 열린 팝업 목록
    private readonly PopupTweenManager tweenManager = new(); // 팝업 열기/닫기 공통 연출 담당

    private Object registeredOwner; // 레이어를 등록한 씬 Root
    private string registeredSceneName; // 디버깅용 씬 이름
    private GameContext context; // 팝업 상태를 기록할 현재 게임 Context

    public Transform PopupLayer { get; private set; } // 팝업 생성 부모
    public CanvasGroup DimLayer { get; private set; } // 팝업 뒤 딤 처리
    public Transform ToastLayer { get; private set; } // 토스트 생성 부모

    public bool HasActiveLayers => PopupLayer != null && DimLayer != null && ToastLayer != null;
    public int OpenPopupCount => openedPopups.Count;

    // GameRoot가 GameContext를 만든 뒤 호출합니다.
    // PopupManager는 전역 시스템이지만 팝업 상태 값은 현재 Context에 기록해야 하므로 별도로 주입받습니다.
    public void SetContext(GameContext gameContext)
    {
        context = gameContext;
    }

    // 현재 씬이 가진 팝업/딤/토스트 레이어를 등록합니다.
    // 씬이 바뀌면 DynamicUIRoot가 새 레이어를 다시 등록하고, 이전 씬 레이어는 해제됩니다.
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

    // 등록한 씬 Root가 파괴될 때 레이어 참조를 해제합니다.
    // 현재 등록 주체가 아닌 오브젝트의 해제 요청은 무시해서 씬 전환 타이밍 충돌을 막습니다.
    public void UnregisterSceneLayers(Object owner)
    {
        if (registeredOwner != owner)
            return;

        ClearOpenPopupReferences();
        registeredOwner = null;
        registeredSceneName = string.Empty;
        PopupLayer = null;
        DimLayer = null;
        ToastLayer = null;

        Debug.Log("[PopupManager] UI 레이어 등록 해제");
    }

    // 팝업 프리팹을 현재 씬의 PopupLayer 아래에 생성하고 엽니다.
    // 아직 레이어가 등록되지 않은 상태에서 호출되면 잘못된 위치에 생성하지 않고 실패시킵니다.
    public async UniTask<TPopup> OpenAsync<TPopup>(TPopup popupPrefab) where TPopup : BasePopup
    {
        if (popupPrefab == null)
        {
            Debug.LogError("[PopupManager] 팝업 프리팹이 없습니다.");
            return null;
        }

        if (!HasActiveLayers)
        {
            Debug.LogError("[PopupManager] 현재 씬의 팝업 레이어가 등록되지 않았습니다.");
            return null;
        }

        TPopup popup = Object.Instantiate(popupPrefab, PopupLayer);
        popup.Initialize(this);

        openedPopups.Add(popup);
        context?.PopupProgress.IncreaseOpenCount();
        RefreshDimLayer();

        await popup.OpenAsync(tweenManager);
        return popup;
    }

    // 특정 팝업을 닫고 스택에서 제거합니다.
    // 닫기 연출과 Destroy 순서를 한곳에서 관리해 중복 제거와 상태 꼬임을 막습니다.
    public async UniTask CloseAsync(BasePopup popup)
    {
        if (popup == null)
            return;

        if (!openedPopups.Remove(popup))
            return;

        await popup.CloseAsync(tweenManager);
        context?.PopupProgress.DecreaseOpenCount();
        RefreshDimLayer();
        Object.Destroy(popup.gameObject);
    }

    // 가장 위에 있는 팝업을 닫습니다.
    // 뒤로가기 버튼이나 공통 닫기 입력을 붙일 때 이 함수를 사용합니다.
    public UniTask CloseTopAsync()
    {
        if (openedPopups.Count == 0)
            return UniTask.CompletedTask;

        BasePopup topPopup = openedPopups[^1];
        return CloseAsync(topPopup);
    }

    // 현재 열린 모든 팝업을 닫습니다.
    // 씬 전환이나 로그아웃 같은 큰 상태 전환에서 팝업 잔여물을 정리할 때 사용합니다.
    public async UniTask CloseAllAsync()
    {
        while (openedPopups.Count > 0)
            await CloseTopAsync();
    }

    // 딤 레이어를 닫힌 기본 상태로 되돌립니다.
    // 새 씬이 레이어를 등록할 때 이전 씬의 팝업 흔적이 남지 않도록 초기화합니다.
    private void ResetDimLayer()
    {
        if (DimLayer == null)
            return;

        DimLayer.alpha = 0f;
        DimLayer.blocksRaycasts = false;
        DimLayer.interactable = false;
    }

    // 열린 팝업 개수에 맞춰 딤 레이어 표시와 입력 차단 상태를 갱신합니다.
    // 팝업이 하나라도 열려 있으면 뒤쪽 UI 입력을 막고, 모두 닫히면 다시 풀어줍니다.
    private void RefreshDimLayer()
    {
        if (DimLayer == null)
            return;

        bool hasPopup = openedPopups.Count > 0;
        DimLayer.alpha = hasPopup ? 0.65f : 0f;
        DimLayer.blocksRaycasts = hasPopup;
        DimLayer.interactable = hasPopup;
    }

    // 씬 레이어가 사라질 때 열린 팝업 참조와 PopupProgress 개수를 함께 정리합니다.
    // Unity가 씬 오브젝트를 이미 파괴하는 중일 수 있으므로 여기서는 별도 Destroy를 호출하지 않습니다.
    private void ClearOpenPopupReferences()
    {
        int openCount = openedPopups.Count;
        openedPopups.Clear();

        for (int i = 0; i < openCount; i++)
            context?.PopupProgress.DecreaseOpenCount();

        ResetDimLayer();
    }
}
