using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 영웅 상세 팝업 View입니다.
// 버튼 이벤트와 UI 반영만 맡고, 이전/다음/닫기 흐름은 HeroDetailController가 담당합니다.
public sealed class HeroDetailPopup : BasePopup
{
    [Header("Header")]
    [SerializeField] private Image characterImage; // 선택된 영웅 이미지
    [SerializeField] private TextMeshProUGUI nameText; // 영웅 이름
    [SerializeField] private TextMeshProUGUI levelText; // 영웅 레벨
    [SerializeField] private TextMeshProUGUI roleText; // 근접/원거리 역할
    [SerializeField] private TextMeshProUGUI rarityText; // 등급 표시가 별도 텍스트로 필요한 프리팹에서만 쓰는 선택 필드

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI healthText; // 체력 표시
    [SerializeField] private TextMeshProUGUI defenseText; // 방어력 표시
    [SerializeField] private TextMeshProUGUI attackText; // 공격력 표시
    [SerializeField] private TextMeshProUGUI attackSpeedText; // 공격속도 표시
    [SerializeField] private TextMeshProUGUI attackRangeText; // 공격범위 표시

    [Header("Skill And Upgrade")]
    [SerializeField] private GameObject skillInfoRoot; // 스킬 데이터가 준비되기 전까지 숨길 스킬 정보 영역
    [SerializeField] private Button upgradeButton; // 추후 강화 데이터가 준비되면 연결할 버튼

    [Header("Navigation")]
    [SerializeField] private Button previousButton; // 이전 해금 영웅 보기
    [SerializeField] private Button nextButton; // 다음 해금 영웅 보기
    [SerializeField] private Button backButton; // 상세 팝업 닫기

    private UnityAction previousClick; // 버튼 이벤트 해제용 캐시
    private UnityAction nextClick; // 버튼 이벤트 해제용 캐시
    private UnityAction backClick; // 버튼 이벤트 해제용 캐시
    private bool referencesResolved; // 이름 기반 보정 탐색을 한 번만 실행하기 위한 플래그

    public event Action PreviousRequested; // 이전 버튼 클릭 이벤트
    public event Action NextRequested; // 다음 버튼 클릭 이벤트
    public event Action BackRequested; // 뒤로가기 버튼 클릭 이벤트
    public event Action<HeroDetailPopupData> DataBound; // 상세 표시 데이터 바인딩 이벤트

    // PopupManager가 팝업을 생성한 직후 버튼 이벤트와 UI 참조를 준비합니다.
    protected override void OnInitialized()
    {
        ResolveMissingReferences();
        RegisterButtons();
    }

    // HeroListController에서 선택한 영웅 정보를 상세 Controller에 전달합니다.
    public void Bind(HeroDetailPopupData data)
    {
        ResolveMissingReferences();
        DataBound?.Invoke(data);
    }

    // Controller가 만든 표시 상태를 실제 UI에 반영합니다.
    public void Render(HeroDetailViewState viewState)
    {
        SetImage(characterImage, viewState.CharacterSprite);
        SetText(nameText, viewState.NameText);
        SetText(levelText, viewState.LevelText);
        SetText(roleText, viewState.RoleText);
        SetText(healthText, viewState.HealthText);
        SetText(defenseText, viewState.DefenseText);
        SetText(attackText, viewState.AttackText);
        SetText(attackSpeedText, viewState.AttackSpeedText);
        SetText(attackRangeText, viewState.AttackRangeText);

        if (skillInfoRoot != null)
            skillInfoRoot.SetActive(false);

        if (upgradeButton != null)
            upgradeButton.interactable = false;

        if (previousButton != null)
            previousButton.interactable = viewState.CanNavigate;

        if (nextButton != null)
            nextButton.interactable = viewState.CanNavigate;
    }

    // 버튼 이벤트를 한 번만 연결합니다.
    private void RegisterButtons()
    {
        previousClick = () => PreviousRequested?.Invoke();
        nextClick = () => NextRequested?.Invoke();
        backClick = () => BackRequested?.Invoke();

        if (previousButton != null)
            previousButton.onClick.AddListener(previousClick);

        if (nextButton != null)
            nextButton.onClick.AddListener(nextClick);

        if (backButton != null)
            backButton.onClick.AddListener(backClick);
    }

    // 팝업이 파괴되기 전에 등록한 버튼 이벤트를 해제합니다.
    private void OnDestroy()
    {
        if (previousButton != null && previousClick != null)
            previousButton.onClick.RemoveListener(previousClick);

        if (nextButton != null && nextClick != null)
            nextButton.onClick.RemoveListener(nextClick);

        if (backButton != null && backClick != null)
            backButton.onClick.RemoveListener(backClick);

    }

    // 프리팹 연결 누락을 줄이기 위해 이름이 고정된 UI를 한 번만 보정 조회합니다.
    private void ResolveMissingReferences()
    {
        if (referencesResolved)
            return;

        referencesResolved = true;

        characterImage ??= FindImageIn("Character");
        nameText ??= FindText("Text_Name");
        levelText ??= FindText("Text_Level");
        roleText ??= FindTextUnder("Type", "Text_Value");
        healthText ??= FindTextUnder("Hp", "Text_Value");
        defenseText ??= FindTextUnder("Reduction", "Text_Value");
        attackText ??= FindTextUnder("Atk", "Text_Value");
        attackSpeedText ??= FindTextUnder("AtkSpeed", "Text_Value");
        attackRangeText ??= FindTextUnder("AtkRange", "Text_Value");
        skillInfoRoot ??= FindTransform("SkillInfo")?.gameObject;
        upgradeButton ??= FindButton("UpgradeButton");
        previousButton ??= FindButton("ArrowButton_Prev");
        nextButton ??= FindButton("ArrowButton_Next");
        backButton ??= FindButton("ArrowButton_Back") ?? FindButton("BackButton");
    }

    // 지정한 이름의 자식 Transform을 찾습니다.
    private Transform FindTransform(string targetName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == targetName)
                return transforms[i];
        }

        return null;
    }

    // 지정한 이름의 TMP 텍스트를 찾습니다.
    private TextMeshProUGUI FindText(string targetName)
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == targetName)
                return texts[i];
        }

        return null;
    }

    // 특정 영역 아래의 TMP 텍스트를 찾습니다.
    private TextMeshProUGUI FindTextUnder(string parentName, string childName)
    {
        Transform parent = FindTransform(parentName);

        if (parent == null)
            return null;

        TextMeshProUGUI[] texts = parent.GetComponentsInChildren<TextMeshProUGUI>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == childName)
                return texts[i];
        }

        return texts.Length > 0 ? texts[^1] : null;
    }

    // 특정 영역 아래의 대표 이미지를 찾습니다.
    private Image FindImageIn(string parentName)
    {
        Transform parent = FindTransform(parentName);

        if (parent == null)
            return null;

        Image[] images = parent.GetComponentsInChildren<Image>(true);
        return images.Length > 0 ? images[^1] : null;
    }

    // 지정한 이름의 버튼을 찾습니다.
    private Button FindButton(string targetName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == targetName)
                return buttons[i];
        }

        return null;
    }

    // 이미지가 없으면 빈 흰 사각형이 보이지 않도록 비활성화합니다.
    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.enabled = sprite != null;
        image.sprite = sprite;
        image.preserveAspect = true;
    }

    // 텍스트 참조가 있을 때만 값을 적용합니다.
    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }
}

// HeroDetailPopup이 열릴 때 필요한 영웅 목록, 선택 위치, 이미지 조회 함수를 묶어 전달합니다.
public sealed class HeroDetailPopupData
{
    private readonly IReadOnlyList<HeroRosterEntry> entries; // 상세 팝업에서 이전/다음 이동에 사용할 해금 영웅 목록
    private readonly Func<HeroDataRow, Sprite> spriteResolver; // HeroData에 맞는 Sprite를 반환하는 함수
    private readonly Func<int, UniTask> openHeroAtIndexAsync; // 이전/다음 이동 시 등급별 상세 프리팹을 다시 열기 위한 함수

    public int SelectedIndex { get; } // 처음 표시할 영웅 위치
    public int EntryCount => entries?.Count ?? 0; // 이동 가능한 영웅 수

    // 상세 팝업에 필요한 표시 데이터 묶음을 만듭니다.
    public HeroDetailPopupData(
        IReadOnlyList<HeroRosterEntry> entries,
        int selectedIndex,
        Func<HeroDataRow, Sprite> spriteResolver,
        Func<int, UniTask> openHeroAtIndexAsync)
    {
        this.entries = entries ?? Array.Empty<HeroRosterEntry>();
        SelectedIndex = selectedIndex;
        this.spriteResolver = spriteResolver;
        this.openHeroAtIndexAsync = openHeroAtIndexAsync;
    }

    // 현재 인덱스에 해당하는 영웅 로스터 항목을 반환합니다.
    public bool TryGetEntry(int index, out HeroRosterEntry entry)
    {
        entry = default;

        if (entries == null || index < 0 || index >= entries.Count)
            return false;

        entry = entries[index];
        return entry.Hero != null;
    }

    // HeroData에 맞는 캐릭터 이미지를 가져옵니다.
    public Sprite ResolveSprite(HeroDataRow hero)
    {
        return spriteResolver?.Invoke(hero);
    }

    // 이전/다음 버튼에서 선택 위치가 바뀌었을 때 새 등급 프리팹으로 상세 팝업을 다시 엽니다.
    public UniTask RequestOpenHeroAtIndexAsync(int index)
    {
        return openHeroAtIndexAsync != null ? openHeroAtIndexAsync(index) : UniTask.CompletedTask;
    }
}
