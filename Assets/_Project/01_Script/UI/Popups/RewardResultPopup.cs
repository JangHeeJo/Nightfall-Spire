using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 전투, 오프라인 보상, 성장 보상 등 지급 결과를 보여주는 팝업입니다.
// 보상 아이템 UI 배치는 Unity에서 만들고, 이 스크립트는 표시 데이터와 수령 완료만 관리합니다.
public sealed class RewardResultPopup : BasePopup
{
    public const string PopupKey = "RewardResult"; // PopupManager 중복 정책에 사용할 고정 Key

    [SerializeField] private Button claimButton; // 보상 확인/수령 버튼
    [SerializeField] private Button closeButton; // 닫기 버튼

    private IReadOnlyList<RewardLine> rewardLines = Array.Empty<RewardLine>(); // 표시할 보상 목록
    private long gold; // 표시할 골드 합계
    private long gem; // 표시할 젬 합계

    public IReadOnlyList<RewardLine> RewardLines => rewardLines;
    public long Gold => gold;
    public long Gem => gem;

    // PopupManager가 팝업을 만든 뒤 버튼 이벤트를 연결합니다.
    protected override void OnInitialized()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(HandleClaimClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(HandleCloseClicked);
    }

    // 보상 서비스가 계산한 보상 목록과 재화 합계를 팝업에 주입합니다.
    public void Configure(RewardResultPopupPayload payload)
    {
        rewardLines = payload.RewardLines ?? Array.Empty<RewardLine>();
        gold = payload.Gold;
        gem = payload.Gem;
        RefreshView();
    }

    // 외부에서 보상 수령을 직접 확정해야 할 때 호출합니다.
    public UniTask ClaimAsync()
    {
        return RequestCloseAsync(PopupCloseReason.Confirmed, new RewardResultPopupPayload(rewardLines, gold, gem));
    }

    // 팝업이 닫힐 때 Unity Button 이벤트를 정리합니다.
    protected override void OnClosed()
    {
        if (claimButton != null)
            claimButton.onClick.RemoveListener(HandleClaimClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleCloseClicked);
    }

    // 보상 목록과 재화 합계가 바뀐 뒤 화면 갱신을 넣을 자리입니다.
    private void RefreshView()
    {
        // 실제 보상 슬롯 생성과 텍스트 연결은 Unity UI 작업 단계에서 추가합니다.
    }

    // 수령 버튼 입력을 확정 결과로 변환합니다.
    private void HandleClaimClicked()
    {
        ClaimAsync().Forget();
    }

    // 닫기 버튼 입력을 단순 닫힘 결과로 변환합니다.
    private void HandleCloseClicked()
    {
        RequestCloseAsync(PopupCloseReason.Dismissed).Forget();
    }
}
