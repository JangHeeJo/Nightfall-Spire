using System.Collections.Generic;

// 실제 기능 팝업들이 주고받는 작은 Payload와 Result 값을 모아둡니다.
// 팝업마다 별도 스크립트를 가지되, 값 타입 파일은 기능별로 무작정 늘리지 않기 위해 이 파일에 둡니다.

// 드래프트 선택 팝업에 전달할 후보 카드 목록입니다.
public readonly struct DraftSelectionPopupPayload
{
    public IReadOnlyList<int> OfferedCardIds { get; } // 화면에 표시할 후보 카드 ID 목록

    // 드래프트 후보 카드 목록을 보관합니다.
    public DraftSelectionPopupPayload(IReadOnlyList<int> offeredCardIds)
    {
        OfferedCardIds = offeredCardIds;
    }
}

// 드래프트 선택 팝업이 닫힐 때 돌려줄 선택 결과입니다.
public readonly struct DraftSelectionPopupResult
{
    public int SelectedCardId { get; } // 사용자가 선택한 카드 ID

    // 선택된 카드 ID를 보관합니다.
    public DraftSelectionPopupResult(int selectedCardId)
    {
        SelectedCardId = selectedCardId;
    }
}

// 보상 결과 팝업에 표시할 보상 묶음입니다.
public readonly struct RewardResultPopupPayload
{
    public IReadOnlyList<RewardLine> RewardLines { get; } // 화면에 표시할 보상 목록
    public long Gold { get; } // 골드 합계
    public long Gem { get; } // 젬 합계

    // 보상 목록과 재화 합계를 보관합니다.
    public RewardResultPopupPayload(IReadOnlyList<RewardLine> rewardLines, long gold, long gem)
    {
        RewardLines = rewardLines;
        Gold = gold;
        Gem = gem;
    }
}

// 업그레이드 구매 팝업에 표시할 구매 정보입니다.
public readonly struct UpgradePurchasePopupPayload
{
    public UpgradeCategory Category { get; } // 업그레이드 분류
    public int UpgradeId { get; } // 구매하려는 업그레이드 ID
    public int CurrentLevel { get; } // 현재 레벨
    public long CostGold { get; } // 골드 비용
    public long CostGem { get; } // 젬 비용

    // 업그레이드 구매에 필요한 표시 값을 보관합니다.
    public UpgradePurchasePopupPayload(UpgradeCategory category, int upgradeId, int currentLevel, long costGold, long costGem)
    {
        Category = category;
        UpgradeId = upgradeId;
        CurrentLevel = currentLevel;
        CostGold = costGold;
        CostGem = costGem;
    }
}

// 업그레이드 구매 팝업에서 확정한 구매 요청입니다.
public readonly struct UpgradePurchasePopupResult
{
    public UpgradeCategory Category { get; } // 업그레이드 분류
    public int UpgradeId { get; } // 구매 확정한 업그레이드 ID

    // 확정한 업그레이드 정보를 보관합니다.
    public UpgradePurchasePopupResult(UpgradeCategory category, int upgradeId)
    {
        Category = category;
        UpgradeId = upgradeId;
    }
}

// 방어 결과 팝업에서 사용자가 선택할 후속 행동입니다.
public enum DefenseResultPopupAction
{
    None, // 아직 행동이 선택되지 않은 상태
    ReturnToLobby, // 로비로 돌아가기
    Retry // 같은 방어를 다시 시도하기
}

// 밤 방어 결과 팝업에 표시할 전투 결과입니다.
public readonly struct DefenseResultPopupPayload
{
    public DefenseOutcome Outcome { get; } // 승리, 패배, 포기 같은 방어 결과
    public int SessionId { get; } // 완료한 방어 세션 ID
    public int ClearedWaveIndex { get; } // 도달하거나 클리어한 웨이브 번호
    public float DurationSec { get; } // 전투 진행 시간
    public IReadOnlyList<RewardLine> RewardLines { get; } // 전투 결과 보상 목록

    // 방어 결과 화면에 필요한 표시 값을 보관합니다.
    public DefenseResultPopupPayload(DefenseOutcome outcome, int sessionId, int clearedWaveIndex, float durationSec, IReadOnlyList<RewardLine> rewardLines)
    {
        Outcome = outcome;
        SessionId = sessionId;
        ClearedWaveIndex = clearedWaveIndex;
        DurationSec = durationSec;
        RewardLines = rewardLines;
    }
}

// 밤 방어 결과 팝업이 닫힐 때 돌려줄 후속 행동입니다.
public readonly struct DefenseResultPopupResult
{
    public DefenseResultPopupAction Action { get; } // 사용자가 선택한 후속 행동

    // 선택된 후속 행동을 보관합니다.
    public DefenseResultPopupResult(DefenseResultPopupAction action)
    {
        Action = action;
    }
}
