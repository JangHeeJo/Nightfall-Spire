using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 드래프트 카드 한 장의 UI 슬롯입니다.
// 실제 이미지와 텍스트 아트는 프리팹 하위 오브젝트에 붙이고, 이 View는 선택 이벤트와 기본 상태만 관리합니다.
public sealed class DraftCardSlotView : MonoBehaviour
{
    private const string SelectButtonName = "Button_Select"; // 카드 선택 입력을 받는 버튼 이름
    private static readonly Color TitleColor = Color.white; // 카드 제목 기본 글자색
    private static readonly Color DescriptionColor = new(1f, 1f, 1f, 0.86f); // 카드 설명 기본 글자색
    private static readonly Color StepColor = new(1f, 0.92f, 0.35f, 1f); // 강화 단계 기본 글자색

    [SerializeField] private Button selectButton; // 카드 전체를 누르는 선택 버튼
    [SerializeField] private Image cardBackgroundImage; // 카드 배경 이미지 자리
    [SerializeField] private Image cardIconImage; // 영웅/스킬/버프 아이콘 이미지 자리
    [SerializeField] private RectTransform gradeBadgeRoot; // 등급 배지 이미지가 들어갈 자리
    [SerializeField] private RectTransform typeBadgeRoot; // HeroRecruit, SkillUpgrade 같은 타입 배지 자리
    [SerializeField] private RectTransform titleTextRoot; // 카드 이름 텍스트 자리
    [SerializeField] private RectTransform descriptionTextRoot; // 카드 설명 텍스트 자리
    [SerializeField] private RectTransform stepTextRoot; // 강화 단계 텍스트 자리
    [SerializeField] private TMP_Text titleText; // DraftCardData.NameKey를 임시 로컬라이즈 문구로 표시합니다.
    [SerializeField] private TMP_Text descriptionText; // DraftCardData.DescKey를 카드 설명으로 표시합니다.
    [SerializeField] private TMP_Text stepText; // 강화 카드의 현재 단계 표시입니다.

    private UnityAction selectButtonClick; // OnDestroy에서 제거할 버튼 콜백
    private int boundCardId; // 현재 슬롯에 연결된 DraftCardData.CardId

    public event Action<int> Selected; // 이 슬롯의 카드가 선택되었음을 알립니다.

    public int BoundCardId => boundCardId;
    public Image CardBackgroundImage => cardBackgroundImage;
    public Image CardIconImage => cardIconImage;
    public RectTransform GradeBadgeRoot => gradeBadgeRoot;
    public RectTransform TypeBadgeRoot => typeBadgeRoot;
    public RectTransform TitleTextRoot => titleTextRoot;
    public RectTransform DescriptionTextRoot => descriptionTextRoot;
    public RectTransform StepTextRoot => stepTextRoot;

    private void Awake()
    {
        CacheReferences();
        RegisterButton();
    }

    private void OnDestroy()
    {
        UnregisterButton();
    }

    // Row를 찾지 못한 예외 상황에서도 선택 ID만 최소 표시해 디버깅할 수 있게 합니다.
    public void BindCard(int cardId)
    {
        boundCardId = cardId;
        gameObject.SetActive(cardId > 0);
        SetText(titleText, cardId > 0 ? cardId.ToString() : string.Empty);
        SetText(descriptionText, string.Empty);
        SetText(stepText, string.Empty);
        ClearImagePlaceholders();
    }

    // DraftCardData.tsv에서 읽은 실제 카드 Row를 화면 텍스트로 표시합니다.
    public void BindCard(DraftCardDataRow card)
    {
        boundCardId = card?.CardId ?? 0;
        gameObject.SetActive(boundCardId > 0);

        if (card == null)
        {
            SetText(titleText, string.Empty);
            SetText(descriptionText, string.Empty);
            SetText(stepText, string.Empty);
            ClearImagePlaceholders();
            return;
        }

        SetText(titleText, card.NameKey);
        SetText(descriptionText, card.DescKey);
        SetText(stepText, BuildStepText(card));
        ClearImagePlaceholders();
    }

    // 선택 가능 여부를 버튼과 함께 제어합니다.
    public void SetInteractable(bool isInteractable)
    {
        if (selectButton != null)
            selectButton.interactable = isInteractable;
    }

    // 인스펙터 참조가 비어 있어도 프리팹 이름 기준으로 한 번 보정합니다.
    private void CacheReferences()
    {
        if (selectButton == null)
            selectButton = FindChildComponent<Button>(SelectButtonName);

        cardBackgroundImage ??= FindChildComponent<Image>("Image_CardBackground");
        cardIconImage ??= FindChildComponent<Image>("Image_CardIcon");
        gradeBadgeRoot ??= FindChildRect("GradeBadge");
        typeBadgeRoot ??= FindChildRect("TypeBadge");
        titleTextRoot ??= FindChildRect("Text_Title");
        descriptionTextRoot ??= FindChildRect("Text_Description");
        stepTextRoot ??= FindChildRect("Text_Step");
        titleText ??= EnsureText(titleTextRoot, 22f, TitleColor, FontStyles.Bold);
        descriptionText ??= EnsureText(descriptionTextRoot, 16f, DescriptionColor, FontStyles.Normal);
        stepText ??= EnsureText(stepTextRoot, 14f, StepColor, FontStyles.Bold);
    }

    // View는 선택된 CardId만 알리고, 실제 선택 처리는 Controller/GameFlow가 맡습니다.
    private void RegisterButton()
    {
        if (selectButton == null)
            return;

        selectButtonClick = () => Selected?.Invoke(boundCardId);
        selectButton.onClick.AddListener(selectButtonClick);
    }

    private void UnregisterButton()
    {
        if (selectButton != null && selectButtonClick != null)
            selectButton.onClick.RemoveListener(selectButtonClick);

        selectButtonClick = null;
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<T>();
    }

    private RectTransform FindChildRect(string childName)
    {
        Transform child = transform.Find(childName);
        return child as RectTransform;
    }

    // 이미지 리소스는 아직 붙이지 않으므로 자리만 남기고 실제 스프라이트는 비워둡니다.
    private void ClearImagePlaceholders()
    {
        ClearImage(cardIconImage);
    }

    // 강화 단계가 있는 카드만 "1/4" 같은 진행도를 표시합니다.
    private static string BuildStepText(DraftCardDataRow card)
    {
        if (card == null || card.UpgradeStep <= 0 || card.MaxStep <= 0)
            return string.Empty;

        return $"{card.UpgradeStep}/{card.MaxStep}";
    }

    // 프리팹에 텍스트 오브젝트만 있고 TMP 컴포넌트가 빠져 있어도 화면에 바로 표시되게 보정합니다.
    private static TMP_Text EnsureText(RectTransform root, float fontSize, Color color, FontStyles fontStyle)
    {
        if (root == null)
            return null;

        TMP_Text text = root.GetComponent<TMP_Text>();

        if (text == null)
            text = root.gameObject.AddComponent<TextMeshProUGUI>();

        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(8f, fontSize * 0.65f);
        text.fontSizeMax = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.margin = Vector4.zero;
        return text;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private static void ClearImage(Image image)
    {
        if (image == null)
            return;

        image.sprite = null;
        image.enabled = false;
    }
}
