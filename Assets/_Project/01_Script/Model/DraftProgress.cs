using System.Collections.Generic;
using R3;

// 전투 중 1-of-3 로그라이트 카드 선택 상태를 관리합니다.
// 실제 카드 효과 적용은 전투 시스템이 붙은 뒤 별도 Resolver에서 처리합니다.
public sealed class DraftProgress
{
    private readonly List<int> offeredCardIds = new(); // 현재 선택지로 제시된 카드 ID 목록
    private readonly List<int> selectedCardIds = new(); // 이번 밤 방어전에서 선택한 카드 ID 목록

    public ReactiveProperty<bool> IsDraftOpen { get; } = new(false); // 카드 선택 UI가 열려 있는지 여부
    public IReadOnlyList<int> OfferedCardIds => offeredCardIds; // 현재 1-of-3 후보 카드들
    public IReadOnlyList<int> SelectedCardIds => selectedCardIds; // 누적 선택 카드들

    // 현재 열린 선택지에서 이 카드를 선택할 수 있는지 확인합니다.
    public bool CanSelectCard(int cardId)
    {
        return IsDraftOpen.Value && offeredCardIds.Contains(cardId);
    }

    // 카드 선택지를 엽니다.
    public void OpenDraft(IReadOnlyList<int> cardIds)
    {
        offeredCardIds.Clear();

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

        selectedCardIds.Add(cardId);
        CloseDraft();
        return true;
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
        CloseDraft();
    }
}
