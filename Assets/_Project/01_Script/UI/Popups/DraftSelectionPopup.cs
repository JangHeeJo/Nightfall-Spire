using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 웨이브 사이에 제시되는 드래프트 카드 선택 팝업입니다.
// 카드 슬롯 UI 구성은 Unity에서 직접 만들고, 이 스크립트는 후보 목록과 선택 결과만 관리합니다.
public sealed class DraftSelectionPopup : BasePopup
{
    public const string PopupKey = "DraftSelection"; // PopupManager 중복 정책에 사용할 고정 Key

    [SerializeField] private Button closeButton; // 닫기 또는 선택 포기 버튼

    private IReadOnlyList<int> offeredCardIds = Array.Empty<int>(); // 현재 팝업에 표시할 후보 카드 ID 목록

    public IReadOnlyList<int> OfferedCardIds => offeredCardIds;

    // PopupManager가 팝업을 만든 뒤 버튼 이벤트를 연결합니다.
    protected override void OnInitialized()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(HandleCloseClicked);
    }

    // 외부 드래프트 서비스가 만든 후보 카드 목록을 팝업에 주입합니다.
    public void Configure(DraftSelectionPopupPayload payload)
    {
        offeredCardIds = payload.OfferedCardIds ?? Array.Empty<int>();
        RefreshView();
    }

    // Unity UI 카드 버튼에서 선택한 카드 ID를 넘길 때 호출합니다.
    public UniTask SelectCardAsync(int cardId)
    {
        return RequestCloseAsync(PopupCloseReason.Confirmed, new DraftSelectionPopupResult(cardId));
    }

    // 팝업이 닫힐 때 Unity Button 이벤트를 정리합니다.
    protected override void OnClosed()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleCloseClicked);
    }

    // 후보 카드 목록이 바뀐 뒤 화면 갱신을 넣을 자리입니다.
    private void RefreshView()
    {
        // 실제 카드 슬롯 생성과 텍스트 연결은 Unity UI 작업 단계에서 추가합니다.
    }

    // 닫기 버튼 입력을 취소 결과로 변환합니다.
    private void HandleCloseClicked()
    {
        RequestCloseAsync(PopupCloseReason.Cancelled).Forget();
    }
}
