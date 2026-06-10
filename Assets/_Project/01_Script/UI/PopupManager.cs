using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 현재 씬의 팝업 레이어와 열린 팝업 스택을 관리합니다.
// 팝업 요청은 전역에서 들어오지만 실제 생성 위치는 현재 씬의 PopupLayer 아래로 제한합니다.
public sealed class PopupManager
{
    private readonly List<PopupHandle> openedPopups = new(); // 현재 열린 팝업 목록
    private readonly PopupTweenManager tweenManager = new(); // 팝업 열기/닫기 공통 연출 담당

    private Object registeredOwner; // 레이어를 등록한 씬 Root
    private string registeredSceneName; // 디버깅용 씬 이름
    private GameContext context; // 팝업 상태를 기록할 현재 게임 Context
    private int nextHandleId = 1; // 팝업 핸들 ID 발급값

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

    // 기존 호출 호환용 함수입니다.
    // 별도 정책이 필요 없는 단순 팝업은 Stack 정책과 타입명 Key로 엽니다.
    public async UniTask<TPopup> OpenAsync<TPopup>(TPopup popupPrefab) where TPopup : BasePopup
    {
        PopupRequest<TPopup> request = new PopupRequest<TPopup>(typeof(TPopup).Name, popupPrefab);
        PopupHandle handle = await OpenAsync(request);
        return handle?.Popup as TPopup;
    }

    // 팝업 요청을 현재 씬의 PopupLayer 아래에 생성하고 엽니다.
    // 중복 처리 정책과 결과 핸들을 함께 관리합니다.
    public async UniTask<PopupHandle> OpenAsync<TPopup>(PopupRequest<TPopup> request) where TPopup : BasePopup
    {
        Debug.Log($"[PopupManager] OpenAsync 요청. Key: {(request == null ? "NULL" : request.Key)}, Prefab: {(request == null || request.Prefab == null ? "NULL" : request.Prefab.name)}, HasActiveLayers: {HasActiveLayers}, PopupLayer: {(PopupLayer == null ? "NULL" : PopupLayer.name)}");

        if (request == null)
        {
            Debug.LogError("[PopupManager] 팝업 요청이 없습니다.");
            return null;
        }

        if (!HasActiveLayers)
        {
            Debug.LogError("[PopupManager] 현재 씬의 팝업 레이어가 등록되지 않았습니다.");
            return null;
        }

        PopupHandle existingHandle = FindOpenHandle(request.Key);

        if (existingHandle != null && request.OpenPolicy == PopupOpenPolicy.SingleInstance)
        {
            Debug.Log($"[PopupManager] 이미 열린 팝업을 재사용합니다. Key: {request.Key}");
            return existingHandle;
        }

        if (request.OpenPolicy == PopupOpenPolicy.ReplaceTop)
            await CloseTopAsync(PopupCloseReason.Replaced);
        else if (request.OpenPolicy == PopupOpenPolicy.ReplaceAll)
            await CloseAllAsync(PopupCloseReason.Replaced);

        Debug.Log($"[PopupManager] 팝업 생성 직전. Key: {request.Key}, Parent: {PopupLayer.name}");
        TPopup popup = Object.Instantiate(request.Prefab, PopupLayer);
        Debug.Log($"[PopupManager] 팝업 생성 완료. Key: {request.Key}, Instance: {popup.name}, Parent: {(popup.transform.parent == null ? "NULL" : popup.transform.parent.name)}");
        popup.Initialize(this);

        PopupHandle handle = new PopupHandle(nextHandleId++, request.Key, popup, request.Priority, request.UseDim);
        openedPopups.Add(handle);
        context?.PopupProgress.IncreaseOpenCount();
        RefreshDimLayer();

        await popup.OpenAsync(tweenManager);
        Debug.Log($"[PopupManager] 팝업 열기 완료. Key: {request.Key}, OpenPopupCount: {openedPopups.Count}");
        return handle;
    }

    // 특정 팝업을 닫고 스택에서 제거합니다.
    // 닫기 연출과 Destroy 순서를 한곳에서 관리해 중복 제거와 상태 꼬임을 막습니다.
    public UniTask CloseAsync(BasePopup popup)
    {
        return CloseAsync(popup, PopupCloseReason.Dismissed);
    }

    // 특정 팝업을 닫고 닫힘 이유와 결과 값을 호출자에게 전달합니다.
    public async UniTask CloseAsync(BasePopup popup, PopupCloseReason closeReason, object payload = null)
    {
        if (popup == null)
            return;

        PopupHandle handle = FindOpenHandle(popup);

        if (handle == null || !openedPopups.Remove(handle))
            return;

        await popup.CloseAsync(tweenManager);
        context?.PopupProgress.DecreaseOpenCount();
        RefreshDimLayer();
        handle.Complete(new PopupResult(closeReason, payload));
        Object.Destroy(popup.gameObject);
    }

    // 팝업 핸들을 기준으로 팝업을 닫습니다.
    public UniTask CloseAsync(PopupHandle handle, PopupCloseReason closeReason = PopupCloseReason.Dismissed, object payload = null)
    {
        if (handle == null)
            return UniTask.CompletedTask;

        return CloseAsync(handle.Popup, closeReason, payload);
    }

    // 가장 위에 있는 팝업을 닫습니다.
    // 뒤로가기 버튼이나 공통 닫기 입력을 붙일 때 이 함수를 사용합니다.
    public UniTask CloseTopAsync()
    {
        return CloseTopAsync(PopupCloseReason.Dismissed);
    }

    // 가장 위에 있는 팝업을 지정한 이유로 닫습니다.
    public UniTask CloseTopAsync(PopupCloseReason closeReason)
    {
        if (openedPopups.Count == 0)
            return UniTask.CompletedTask;

        PopupHandle topPopup = openedPopups[^1];
        return CloseAsync(topPopup, closeReason);
    }

    // 현재 열린 모든 팝업을 닫습니다.
    // 씬 전환이나 로그아웃 같은 큰 상태 전환에서 팝업 잔여물을 정리할 때 사용합니다.
    public async UniTask CloseAllAsync()
    {
        await CloseAllAsync(PopupCloseReason.Dismissed);
    }

    // 현재 열린 모든 팝업을 지정한 이유로 닫습니다.
    public async UniTask CloseAllAsync(PopupCloseReason closeReason)
    {
        while (openedPopups.Count > 0)
            await CloseTopAsync(closeReason);
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

        bool hasPopup = HasDimPopup();
        DimLayer.alpha = hasPopup ? 0.65f : 0f;
        DimLayer.blocksRaycasts = hasPopup;
        DimLayer.interactable = hasPopup;
    }

    // 현재 열린 팝업 중 딤 레이어가 필요한 팝업이 있는지 확인합니다.
    private bool HasDimPopup()
    {
        for (int i = 0; i < openedPopups.Count; i++)
        {
            if (openedPopups[i].UseDim)
                return true;
        }

        return false;
    }

    // 같은 Key를 가진 열린 팝업 핸들을 찾습니다.
    private PopupHandle FindOpenHandle(string key)
    {
        for (int i = openedPopups.Count - 1; i >= 0; i--)
        {
            if (openedPopups[i].Key == key)
                return openedPopups[i];
        }

        return null;
    }

    // 팝업 인스턴스에 대응하는 열린 핸들을 찾습니다.
    private PopupHandle FindOpenHandle(BasePopup popup)
    {
        for (int i = openedPopups.Count - 1; i >= 0; i--)
        {
            if (openedPopups[i].Popup == popup)
                return openedPopups[i];
        }

        return null;
    }

    // 씬 레이어가 사라질 때 열린 팝업 참조와 PopupProgress 개수를 함께 정리합니다.
    // Unity가 씬 오브젝트를 이미 파괴하는 중일 수 있으므로 여기서는 별도 Destroy를 호출하지 않습니다.
    private void ClearOpenPopupReferences()
    {
        int openCount = openedPopups.Count;

        for (int i = 0; i < openedPopups.Count; i++)
            openedPopups[i].Complete(new PopupResult(PopupCloseReason.SceneChanged));

        openedPopups.Clear();

        for (int i = 0; i < openCount; i++)
            context?.PopupProgress.DecreaseOpenCount();

        ResetDimLayer();
    }
}
