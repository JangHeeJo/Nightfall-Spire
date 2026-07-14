using System;
using System.Collections.Generic;
using System.Linq;

// 전투 중 드래프트 카드 후보 생성과 선택 시작을 담당합니다.
public sealed class DraftService
{
    public const int OpeningHeroRecruitPickCount = 2; // 시작 영웅 모집은 2장 중 1장
    public const int RequiredOpeningHeroRecruitCount = 2; // 전투 시작 때만 영웅 모집을 2번 강제

    private readonly IDraftDataSource dataSource; // 드래프트 테이블 조회 계약
    private readonly DraftProgress draftProgress; // 드래프트 진행 모델
    private readonly GameContext context; // 영웅 해금과 이번 전투 모집 상태 조회

    // 드래프트 규칙에 필요한 데이터와 진행 모델을 받습니다.
    public DraftService(IDraftDataSource dataSource, DraftProgress draftProgress, GameContext context = null)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        this.draftProgress = draftProgress ?? throw new ArgumentNullException(nameof(draftProgress));
        this.context = context;
    }

    // 전투 시작용 영웅 모집 후보를 열어 2장 중 1장을 고르게 합니다. 전체 모집 한도는 해금 영웅 수가 결정합니다.
    public DraftOfferResult TryOpenOpeningHeroRecruitOffer()
    {
        DraftOfferResult result = BuildOpeningHeroRecruitOffer();

        if (!result.IsSuccess)
            return result;

        draftProgress.OpenDraft(result.OfferedCardIds, DraftDeckType.OpeningHeroRecruit);
        return result;
    }

    // 전투 중 레벨업 후보를 열어 3장 중 1장을 고르게 합니다. 미모집 해금 영웅도 여기서 계속 나올 수 있습니다.
    public DraftOfferResult TryOpenLevelUpOffer(int draftPoolId)
    {
        DraftOfferResult result = BuildLevelUpOffer(draftPoolId);

        if (!result.IsSuccess)
            return result;

        draftProgress.OpenDraft(result.OfferedCardIds, DraftDeckType.LevelUp);
        return result;
    }

    // 시작 영웅 모집은 해금되어 있고 이번 전투에서 아직 모집하지 않은 HeroRecruit 카드만 사용합니다.
    public DraftOfferResult BuildOpeningHeroRecruitOffer()
    {
        List<int> offeredCardIds = BuildHeroRecruitCandidates()
            .OrderByDescending(card => card.Weight)
            .ThenBy(card => card.CardId)
            .Take(OpeningHeroRecruitPickCount)
            .Select(card => card.CardId)
            .ToList();

        return offeredCardIds.Count == 0
            ? DraftOfferResult.Fail(DraftFailureReason.NoCandidateCards)
            : DraftOfferResult.Success(offeredCardIds);
    }

    // 레벨업 드래프트는 미모집 해금 영웅 모집 카드와 이미 모집된 영웅 강화 카드를 함께 후보로 씁니다.
    public DraftOfferResult BuildLevelUpOffer(int draftPoolId)
    {
        if (!dataSource.TryGetDraftPool(draftPoolId, out DraftPoolDataRow poolRow))
            return DraftOfferResult.Fail(DraftFailureReason.PoolNotFound);

        // 레벨업 드래프트는 카드 타입마다 후보 조건이 달라서 CanOfferLevelUpCard로 한 번에 모읍니다.
        List<DraftCardDataRow> candidates = new List<DraftCardDataRow>();
        IReadOnlyList<DraftCardDataRow> cards = dataSource.GetDraftCards();

        for (int i = 0; i < cards.Count; i++)
        {
            DraftCardDataRow card = cards[i];

            if (!CanOfferLevelUpCard(poolRow, card))
                continue;

            candidates.Add(card);
        }

        if (candidates.Count == 0)
            return DraftOfferResult.Fail(DraftFailureReason.NoCandidateCards);

        List<int> offeredCardIds = candidates
            .OrderByDescending(card => card.Weight)
            .ThenBy(card => card.CardId)
            .Take(poolRow.PickCount)
            .Select(card => card.CardId)
            .ToList();

        return offeredCardIds.Count == 0
            ? DraftOfferResult.Fail(DraftFailureReason.NoCandidateCards)
            : DraftOfferResult.Success(offeredCardIds);
    }

    // 열린 카드 하나를 선택하고, 카드 타입별 전투 중 드래프트 상태를 반영합니다.
    public bool TrySelectOfferedCard(int cardId)
    {
        if (!draftProgress.CanSelectCard(cardId))
            return false;

        if (!TryGetDraftCard(cardId, out DraftCardDataRow card))
            return false;

        ApplyCardSelectionState(card);

        return draftProgress.SelectCard(cardId);
    }

    // 효과 테이블 적용이 필요한 카드인지 확인합니다.
    public bool ShouldApplyCardEffects(int cardId)
    {
        return TryGetDraftCard(cardId, out DraftCardDataRow card)
               && card.CardType != DraftCardType.HeroRecruit;
    }

    // 드래프트 풀 공통 조건을 기준으로 후보 가능 여부를 판단합니다.
    private bool CanOfferPooledCard(DraftPoolDataRow poolRow, DraftCardDataRow card)
    {
        if (card == null)
            return false;

        if (HasReachedPickLimit(card))
            return false;

        if (HasExcludedTag(poolRow, card))
            return false;

        if (!HasIncludedTag(poolRow, card))
            return false;

        return true;
    }

    // 레벨업 후보 가능 여부를 판단합니다.
    private bool CanOfferLevelUpCard(DraftPoolDataRow poolRow, DraftCardDataRow card)
    {
        // 시작 2회가 끝난 뒤에도 미모집 해금 영웅 카드는 레벨업 드래프트에 계속 섞일 수 있습니다.
        if (card?.CardType == DraftCardType.HeroRecruit)
            return !HasExcludedTag(poolRow, card) && CanOfferHeroRecruitCard(card);

        if (!CanOfferPooledCard(poolRow, card))
            return false;

        if (card.CardType == DraftCardType.SkillUpgrade || card.CardType == DraftCardType.HeroStatUpgrade)
            return CanOfferHeroUpgradeCard(card);

        return true;
    }

    // 모집 가능한 영웅 카드 후보를 만듭니다.
    private List<DraftCardDataRow> BuildHeroRecruitCandidates()
    {
        List<DraftCardDataRow> candidates = new List<DraftCardDataRow>();
        IReadOnlyList<DraftCardDataRow> cards = dataSource.GetDraftCards();

        for (int i = 0; i < cards.Count; i++)
        {
            DraftCardDataRow card = cards[i];

            if (card?.CardType != DraftCardType.HeroRecruit)
                continue;

            if (!CanOfferHeroRecruitCard(card))
                continue;

            candidates.Add(card);
        }

        return candidates;
    }

    // 해금되어 있고 이번 전투에 아직 모집되지 않은 영웅만 모집 후보로 허용합니다.
    private bool CanOfferHeroRecruitCard(DraftCardDataRow card)
    {
        if (card == null || card.TargetHeroId <= 0 || draftProgress.IsHeroRecruited(card.TargetHeroId))
            return false;

        if (context?.HeroCollectionProgress == null)
            return true;

        if (context.HeroCollectionProgress.TryGetHero(card.TargetHeroId, out HeroRuntimeState heroState))
            return heroState.IsUnlocked.Value;

        return false;
    }

    // 카드 타입별 선택 결과를 이번 전투 드래프트 상태에 반영합니다.
    private void ApplyCardSelectionState(DraftCardDataRow card)
    {
        // 카드 효과 Resolver가 모르는 드래프트 전용 상태는 여기서 먼저 반영합니다.
        if (card.CardType == DraftCardType.HeroRecruit)
            draftProgress.RecruitHero(card.TargetHeroId);

        if (card.CardType == DraftCardType.SkillUpgrade || card.CardType == DraftCardType.HeroStatUpgrade)
            draftProgress.SetHeroUpgradeStep(card.TargetHeroId, card.UpgradeKey, card.UpgradeStep);

        if (draftProgress.CurrentDeckType.Value == DraftDeckType.OpeningHeroRecruit)
            draftProgress.CompleteOpeningHeroRecruit();
    }

    // 카드별 최대 선택 횟수를 넘었는지 판단합니다.
    private bool HasReachedPickLimit(DraftCardDataRow card)
    {
        int selectedCount = draftProgress.GetSelectedCardCount(card.CardId);

        if (card.IsUnique && selectedCount > 0)
            return true;

        return card.MaxStack > 0 && selectedCount >= card.MaxStack;
    }

    // 모집된 영웅의 다음 강화 단계 카드만 후보로 허용합니다.
    private bool CanOfferHeroUpgradeCard(DraftCardDataRow card)
    {
        if (card.TargetHeroId <= 0 || string.IsNullOrWhiteSpace(card.UpgradeKey))
            return false;

        // 아직 전투 중 모집하지 않은 영웅의 스킬/개별 강화는 후보에서 제외합니다.
        if (!draftProgress.IsHeroRecruited(card.TargetHeroId))
            return false;

        int currentStep = draftProgress.GetHeroUpgradeStep(card.TargetHeroId, card.UpgradeKey);

        if (card.MaxStep > 0 && currentStep >= card.MaxStep)
            return false;

        // 1단계 다음에는 2단계처럼 현재 단계 바로 다음 카드만 등장합니다.
        return card.UpgradeStep == currentStep + 1;
    }

    // 카드 ID로 드래프트 카드 Row를 찾습니다.
    private bool TryGetDraftCard(int cardId, out DraftCardDataRow row)
    {
        IReadOnlyList<DraftCardDataRow> cards = dataSource.GetDraftCards();

        for (int i = 0; i < cards.Count; i++)
        {
            DraftCardDataRow candidate = cards[i];

            if (candidate.CardId != cardId)
                continue;

            row = candidate;
            return true;
        }

        row = null;
        return false;
    }

    // 풀의 제외 태그가 카드에 하나라도 있으면 후보에서 제외합니다.
    private static bool HasExcludedTag(DraftPoolDataRow poolRow, DraftCardDataRow card)
    {
        for (int i = 0; i < poolRow.ExcludeTagList.Count; i++)
        {
            if (card.CardTagList.Contains(poolRow.ExcludeTagList[i]))
                return true;
        }

        return false;
    }

    // 풀의 포함 태그가 비어 있으면 전체 허용, 아니면 하나 이상 일치해야 허용합니다.
    private static bool HasIncludedTag(DraftPoolDataRow poolRow, DraftCardDataRow card)
    {
        if (poolRow.IncludeTagList.Count == 0)
            return true;

        for (int i = 0; i < poolRow.IncludeTagList.Count; i++)
        {
            if (card.CardTagList.Contains(poolRow.IncludeTagList[i]))
                return true;
        }

        return false;
    }
}
