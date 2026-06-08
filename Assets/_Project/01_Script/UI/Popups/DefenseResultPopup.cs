using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 밤 방어가 끝난 뒤 승리, 패배, 보상, 다음 행동을 보여주는 결과 팝업입니다.
// 결과 화면 레이아웃은 Unity에서 만들고, 이 스크립트는 표시 데이터와 후속 행동만 관리합니다.
public sealed class DefenseResultPopup : BasePopup
{
    public const string PopupKey = "DefenseResult"; // PopupManager 중복 정책에 사용할 고정 Key

    [SerializeField] private Button returnToLobbyButton; // 로비 복귀 버튼
    [SerializeField] private Button retryButton; // 다시 시도 버튼
    [SerializeField] private Button closeButton; // 닫기 버튼

    private DefenseResultPopupPayload payload; // 현재 표시 중인 방어 결과 정보
    private IReadOnlyList<RewardLine> rewardLines = Array.Empty<RewardLine>(); // 결과 보상 목록

    public DefenseResultPopupPayload Payload => payload;
    public IReadOnlyList<RewardLine> RewardLines => rewardLines;

    // PopupManager가 팝업을 만든 뒤 버튼 이벤트를 연결합니다.
    protected override void OnInitialized()
    {
        if (returnToLobbyButton != null)
            returnToLobbyButton.onClick.AddListener(HandleReturnToLobbyClicked);

        if (retryButton != null)
            retryButton.onClick.AddListener(HandleRetryClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(HandleReturnToLobbyClicked);
    }

    // 방어 런타임이 계산한 결과와 보상 목록을 팝업에 주입합니다.
    public void Configure(DefenseResultPopupPayload nextPayload)
    {
        payload = nextPayload;
        rewardLines = nextPayload.RewardLines ?? Array.Empty<RewardLine>();
        RefreshView();
    }

    // 로비 복귀를 확정 결과로 반환합니다.
    public UniTask ReturnToLobbyAsync()
    {
        DefenseResultPopupResult result = new DefenseResultPopupResult(DefenseResultPopupAction.ReturnToLobby);
        return RequestCloseAsync(PopupCloseReason.Confirmed, result);
    }

    // 같은 방어 재시도를 확정 결과로 반환합니다.
    public UniTask RetryAsync()
    {
        DefenseResultPopupResult result = new DefenseResultPopupResult(DefenseResultPopupAction.Retry);
        return RequestCloseAsync(PopupCloseReason.Confirmed, result);
    }

    // 팝업이 닫힐 때 Unity Button 이벤트를 정리합니다.
    protected override void OnClosed()
    {
        if (returnToLobbyButton != null)
            returnToLobbyButton.onClick.RemoveListener(HandleReturnToLobbyClicked);

        if (retryButton != null)
            retryButton.onClick.RemoveListener(HandleRetryClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleReturnToLobbyClicked);
    }

    // 방어 결과와 보상 목록이 바뀐 뒤 화면 갱신을 넣을 자리입니다.
    private void RefreshView()
    {
        // 실제 승패 라벨, 웨이브 정보, 보상 슬롯 연결은 Unity UI 작업 단계에서 추가합니다.
    }

    // 로비 복귀 버튼 입력을 후속 행동 결과로 변환합니다.
    private void HandleReturnToLobbyClicked()
    {
        ReturnToLobbyAsync().Forget();
    }

    // 다시 시도 버튼 입력을 후속 행동 결과로 변환합니다.
    private void HandleRetryClicked()
    {
        RetryAsync().Forget();
    }
}
