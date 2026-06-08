using System;
using System.Collections.Generic;
using System.Linq;

// 전투 중 드래프트 카드 후보 생성과 선택 시작을 담당합니다.
public sealed class DraftService
{
    private readonly IDraftDataSource dataSource; // 드래프트 테이블 조회 계약
    private readonly DraftProgress draftProgress; // 드래프트 진행 모델

    // 드래프트 규칙에 필요한 데이터와 진행 모델을 받습니다.
    public DraftService(IDraftDataSource dataSource, DraftProgress draftProgress)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        this.draftProgress = draftProgress ?? throw new ArgumentNullException(nameof(draftProgress));
    }

    // 드래프트 풀 기준으로 후보를 만들고 DraftProgress를 엽니다.
    public DraftOfferResult TryOpenOffer(int draftPoolId)
    {
        DraftOfferResult result = BuildOffer(draftPoolId, draftProgress.SelectedCardIds);

        if (!result.IsSuccess)
            return result;

        draftProgress.OpenDraft(result.OfferedCardIds);
        return result;
    }

    // 드래프트 풀 기준으로 카드 후보를 계산합니다.
    public DraftOfferResult BuildOffer(int draftPoolId, IReadOnlyList<int> alreadySelectedCardIds)
    {
        if (!dataSource.TryGetDraftPool(draftPoolId, out DraftPoolDataRow poolRow))
            return DraftOfferResult.Fail(DraftFailureReason.PoolNotFound);

        List<DraftCardDataRow> candidates = new List<DraftCardDataRow>();
        IReadOnlyList<DraftCardDataRow> cards = dataSource.GetDraftCards();

        for (int i = 0; i < cards.Count; i++)
        {
            DraftCardDataRow card = cards[i];

            if (!CanOfferCard(poolRow, card, alreadySelectedCardIds))
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

        if (offeredCardIds.Count == 0)
            return DraftOfferResult.Fail(DraftFailureReason.NoCandidateCards);

        return DraftOfferResult.Success(offeredCardIds);
    }

    // 드래프트 풀 조건과 이미 선택한 카드 조건을 기준으로 후보 가능 여부를 판단합니다.
    private static bool CanOfferCard(DraftPoolDataRow poolRow, DraftCardDataRow card, IReadOnlyList<int> alreadySelectedCardIds)
    {
        if (card == null)
            return false;

        if (card.IsUnique && alreadySelectedCardIds != null && alreadySelectedCardIds.Contains(card.CardId))
            return false;

        if (HasExcludedTag(poolRow, card))
            return false;

        if (!HasIncludedTag(poolRow, card))
            return false;

        return true;
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

public enum DraftFailureReason
{
    None,
    PoolNotFound,
    NoCandidateCards
}

// 드래프트 후보 생성 결과입니다.
public readonly struct DraftOfferResult
{
    public bool IsSuccess { get; } // 후보 생성 성공 여부
    public DraftFailureReason FailureReason { get; } // 실패 이유
    public IReadOnlyList<int> OfferedCardIds { get; } // 제시할 카드 ID 목록

    private DraftOfferResult(bool isSuccess, DraftFailureReason failureReason, IReadOnlyList<int> offeredCardIds)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        OfferedCardIds = offeredCardIds;
    }

    public static DraftOfferResult Success(IReadOnlyList<int> offeredCardIds)
    {
        return new DraftOfferResult(true, DraftFailureReason.None, offeredCardIds);
    }

    public static DraftOfferResult Fail(DraftFailureReason failureReason)
    {
        return new DraftOfferResult(false, failureReason, Array.Empty<int>());
    }
}
