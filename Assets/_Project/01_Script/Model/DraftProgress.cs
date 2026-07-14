using System.Collections.Generic;
using R3;

// 전투 중 1-of-3 로그라이트 카드 선택 상태를 관리합니다.
// 실제 카드 효과 적용은 전투 시스템이 붙은 뒤 별도 Resolver에서 처리합니다.
public sealed class DraftProgress
{
    private readonly List<int> offeredCardIds = new(); // 현재 선택지로 제시된 카드 ID 목록
    private readonly List<int> selectedCardIds = new(); // 이번 밤 방어전에서 선택한 카드 ID 목록
    private readonly List<int> recruitedHeroIds = new(); // 이번 밤 방어전에서 모집한 모든 영웅 목록
    private readonly Dictionary<int, int> selectedCardCounts = new(); // 카드별 이번 전투 선택 횟수
    private readonly Dictionary<string, int> heroUpgradeSteps = new(); // 영웅별 강화 라인의 현재 단계

    public ReactiveProperty<bool> IsDraftOpen { get; } = new(false); // 카드 선택 UI가 열려 있는지 여부
    public ReactiveProperty<DraftDeckType> CurrentDeckType { get; } = new(DraftDeckType.LevelUp); // 현재 열린 드래프트 종류
    public ReactiveProperty<int> OpeningHeroRecruitCount { get; } = new(0); // 전투 시작 전용 영웅 모집을 완료한 횟수
    public IReadOnlyList<int> OfferedCardIds => offeredCardIds; // 현재 1-of-3 후보 카드들
    public IReadOnlyList<int> SelectedCardIds => selectedCardIds; // 누적 선택 카드들
    public IReadOnlyList<int> RecruitedHeroIds => recruitedHeroIds; // 시작 모집과 레벨업 모집으로 이번 전투에서 얻은 영웅들

    // 현재 열린 선택지에서 이 카드를 선택할 수 있는지 확인합니다.
    public bool CanSelectCard(int cardId)
    {
        return IsDraftOpen.Value && offeredCardIds.Contains(cardId);
    }

    // 카드 선택지를 엽니다.
    public void OpenDraft(IReadOnlyList<int> cardIds)
    {
        OpenDraft(cardIds, DraftDeckType.LevelUp);
    }

    // 지정한 덱 타입으로 카드 선택지를 엽니다.
    public void OpenDraft(IReadOnlyList<int> cardIds, DraftDeckType deckType)
    {
        offeredCardIds.Clear();
        CurrentDeckType.Value = deckType;

        if (cardIds != null)
        {
            for (int i = 0; i < cardIds.Count; i++)
            {
                if (cardIds[i] > 0)
                    offeredCardIds.Add(cardIds[i]);
            }
        }

        IsDraftOpen.Value = offeredCardIds.Count > 0;
    }

    // 선택한 카드 ID를 기록하고 선택 UI를 닫습니다.
    public bool SelectCard(int cardId)
    {
        if (!CanSelectCard(cardId))
            return false;

        // 선택 기록은 고유 카드 재등장 방지와 MaxStack 제한 계산에 같이 사용됩니다.
        selectedCardIds.Add(cardId);
        selectedCardCounts.TryGetValue(cardId, out int count);
        selectedCardCounts[cardId] = count + 1;
        CloseDraft();
        return true;
    }

    // 영웅 모집 카드 선택 결과를 이번 전투 상태에 반영합니다. 시작 2회 이후 레벨업 모집도 여기에 누적됩니다.
    public void RecruitHero(int heroId)
    {
        if (heroId <= 0 || recruitedHeroIds.Contains(heroId))
            return;

        recruitedHeroIds.Add(heroId);
    }

    // 전투 시작 전용 영웅 모집 선택이 완료되었음을 기록합니다.
    public void CompleteOpeningHeroRecruit()
    {
        OpeningHeroRecruitCount.Value += 1;
    }

    // 이번 전투에서 이미 소환한 영웅인지 확인합니다.
    public bool IsHeroRecruited(int heroId)
    {
        return recruitedHeroIds.Contains(heroId);
    }

    // 이번 전투에서 해당 카드를 몇 번 골랐는지 반환합니다.
    public int GetSelectedCardCount(int cardId)
    {
        return selectedCardCounts.TryGetValue(cardId, out int count) ? count : 0;
    }

    // 영웅 강화 카드 선택 결과를 단계 상태에 반영합니다.
    public void SetHeroUpgradeStep(int heroId, string upgradeKey, int step)
    {
        if (heroId <= 0 || string.IsNullOrWhiteSpace(upgradeKey) || step <= 0)
            return;

        heroUpgradeSteps[BuildHeroUpgradeKey(heroId, upgradeKey)] = step;
    }

    // 영웅 강화 라인의 현재 단계를 반환합니다.
    public int GetHeroUpgradeStep(int heroId, string upgradeKey)
    {
        if (heroId <= 0 || string.IsNullOrWhiteSpace(upgradeKey))
            return 0;

        return heroUpgradeSteps.TryGetValue(BuildHeroUpgradeKey(heroId, upgradeKey), out int step) ? step : 0;
    }

    // 현재 선택지만 닫습니다.
    public void CloseDraft()
    {
        offeredCardIds.Clear();
        IsDraftOpen.Value = false;
    }

    // 새 밤 방어전을 시작할 때 누적 선택 카드도 초기화합니다.
    public void ResetForNewDefenseSession()
    {
        selectedCardIds.Clear();
        recruitedHeroIds.Clear();
        selectedCardCounts.Clear();
        heroUpgradeSteps.Clear();
        OpeningHeroRecruitCount.Value = 0;
        CurrentDeckType.Value = DraftDeckType.LevelUp;
        CloseDraft();
    }

    private static string BuildHeroUpgradeKey(int heroId, string upgradeKey)
    {
        // 같은 UpgradeKey라도 영웅이 다르면 별도 강화 라인으로 취급합니다.
        return $"{heroId}:{upgradeKey}";
    }
}
