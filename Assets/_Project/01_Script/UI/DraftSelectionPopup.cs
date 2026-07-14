using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 전투 중 카드 드래프트를 표시하는 팝업 View입니다.
// 배경 딤 패널이 raycast를 받아 뒤쪽 전투 UI 입력을 막고, 전투 일시정지는 BattleSessionRuntimeController 상태가 담당합니다.
public sealed class DraftSelectionPopup : BasePopup
{
    private const string DimBackgroundName = "Panel_DimBackground"; // 전투 화면을 어둡게 덮는 반투명 배경
    private const string CardSlotsRootName = "Group_CardSlots"; // 카드 슬롯 2~3장이 들어가는 부모
    private const string TitleTextName = "Text_Title"; // 드래프트 종류를 보여주는 제목 텍스트

    [SerializeField] private Image dimBackground; // 반투명 배경 이미지
    [SerializeField] private DraftCardSlotView[] cardSlots = Array.Empty<DraftCardSlotView>(); // 표시 가능한 카드 슬롯들
    [SerializeField] private TMP_Text titleText; // 시작 영웅 선택/레벨업 카드 선택 제목

    public event Action<int> CardSelected; // 카드 슬롯에서 선택된 CardId를 Controller로 전달합니다.

    public Image DimBackground => dimBackground;
    public IReadOnlyList<DraftCardSlotView> CardSlots => cardSlots;

    // 팝업 생성 직후 프리팹 참조와 카드 슬롯 이벤트를 준비합니다.
    protected override void OnInitialized()
    {
        CacheReferences();
        RegisterCardSlots();
    }

    private void OnDestroy()
    {
        UnregisterCardSlots();
    }

    // DraftProgress.OfferedCardIds를 UI 슬롯에 연결합니다.
    // 시작 모집은 2장, 레벨업은 3장을 넘기면 같은 프리팹으로 처리됩니다.
    public void BindCards(IReadOnlyList<int> cardIds)
    {
        CacheReferences();
        ArrangeCardSlots(cardIds?.Count ?? 0);
        SetTitle("Select Card");

        for (int i = 0; i < cardSlots.Length; i++)
        {
            int cardId = cardIds != null && i < cardIds.Count ? cardIds[i] : 0;
            cardSlots[i]?.BindCard(cardId);
        }

    }

    // 테이블 Row가 준비된 실제 전투 드래프트에서는 이름/설명까지 슬롯에 함께 연결합니다.
    public void BindCards(IReadOnlyList<DraftCardDataRow> cards)
    {
        CacheReferences();
        ArrangeCardSlots(cards?.Count ?? 0);
        SetTitle(IsHeroRecruitOffer(cards) ? "Select Hero" : "Select Card");

        for (int i = 0; i < cardSlots.Length; i++)
        {
            DraftCardDataRow card = cards != null && i < cards.Count ? cards[i] : null;
            cardSlots[i]?.BindCard(card);
        }
    }

    // 현재 후보가 모두 HeroRecruit면 시작/추가 영웅 선택 제목을 사용합니다.
    private static bool IsHeroRecruitOffer(IReadOnlyList<DraftCardDataRow> cards)
    {
        if (cards == null || cards.Count == 0)
            return false;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null || cards[i].CardType != DraftCardType.HeroRecruit)
                return false;
        }

        return true;
    }

    // 시작 영웅 2장, 레벨업 3장, 예외적인 1장 후보 모두 화면 중앙 기준으로 배치합니다.
    private void ArrangeCardSlots(int activeCardCount)
    {
        int visibleCount = Mathf.Clamp(activeCardCount, 0, cardSlots.Length);

        if (visibleCount <= 0)
            return;

        for (int i = 0; i < cardSlots.Length; i++)
        {
            RectTransform slotRect = cardSlots[i] == null ? null : cardSlots[i].transform as RectTransform;

            if (slotRect == null)
                continue;

            if (i >= visibleCount)
                continue;

            SetCenteredSlotRect(slotRect, i, visibleCount);
        }
    }

    // 카드 수에 맞춰 슬롯 폭과 간격을 재계산합니다.
    private static void SetCenteredSlotRect(RectTransform slotRect, int slotIndex, int visibleCount)
    {
        const float gap = 0.035f; // 카드 사이 여백
        float cardWidth = visibleCount == 1 ? 0.32f : visibleCount == 2 ? 0.31f : 0.29f;
        float totalWidth = visibleCount * cardWidth + (visibleCount - 1) * gap;
        float startX = 0.5f - totalWidth * 0.5f;
        float minX = startX + slotIndex * (cardWidth + gap);
        float maxX = minX + cardWidth;

        slotRect.anchorMin = new Vector2(minX, 0f);
        slotRect.anchorMax = new Vector2(maxX, 1f);
        slotRect.anchoredPosition = Vector2.zero;
        slotRect.sizeDelta = Vector2.zero;
        slotRect.pivot = new Vector2(0.5f, 0.5f);
    }

    // 카드 선택 후 중복 클릭을 막거나, 새 후보 표시 때 다시 열 때 사용합니다.
    public void SetCardsInteractable(bool isInteractable)
    {
        for (int i = 0; i < cardSlots.Length; i++)
            cardSlots[i]?.SetInteractable(isInteractable);
    }

    // 딤 배경 투명도를 조절합니다. 기본 프리팹은 전투 화면이 보이도록 55% 검정입니다.
    public void SetDimAlpha(float alpha)
    {
        if (dimBackground == null)
            return;

        Color color = dimBackground.color;
        color.a = Mathf.Clamp01(alpha);
        dimBackground.color = color;
    }

    // 인스펙터 연결이 비어 있어도 프리팹 이름 기준으로 한 번 보정합니다.
    private void CacheReferences()
    {
        if (dimBackground == null)
        {
            Transform dimTransform = transform.Find(DimBackgroundName);
            dimBackground = dimTransform == null ? null : dimTransform.GetComponent<Image>();
        }

        if (cardSlots == null || cardSlots.Length == 0)
        {
            Transform slotsRoot = transform.Find(CardSlotsRootName);
            cardSlots = slotsRoot == null
                ? GetComponentsInChildren<DraftCardSlotView>(true)
                : slotsRoot.GetComponentsInChildren<DraftCardSlotView>(true);
        }

        if (titleText == null)
        {
            Transform titleTransform = transform.Find(TitleTextName);
            titleText = EnsureTitleText(titleTransform as RectTransform);
        }
    }

    // 프리팹 타이틀 자리에 TMP 컴포넌트가 빠져 있어도 제목이 보이게 보정합니다.
    private static TMP_Text EnsureTitleText(RectTransform titleRoot)
    {
        if (titleRoot == null)
            return null;

        TMP_Text text = titleRoot.GetComponent<TMP_Text>();

        if (text == null)
            text = titleRoot.gameObject.AddComponent<TextMeshProUGUI>();

        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 34f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        return text;
    }

    private void SetTitle(string value)
    {
        if (titleText != null)
            titleText.text = value ?? string.Empty;
    }

    private void RegisterCardSlots()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null)
                cardSlots[i].Selected += HandleCardSelected;
        }
    }

    private void UnregisterCardSlots()
    {
        if (cardSlots == null)
            return;

        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null)
                cardSlots[i].Selected -= HandleCardSelected;
        }
    }

    private void HandleCardSelected(int cardId)
    {
        if (cardId <= 0)
            return;

        CardSelected?.Invoke(cardId);
    }
}
