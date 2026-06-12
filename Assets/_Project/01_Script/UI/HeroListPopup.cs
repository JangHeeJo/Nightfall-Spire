using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 히어로 목록 팝업 View입니다.
// 화면 오브젝트 생성, 이미지 조회, 카드 클릭 이벤트 전달만 맡고 데이터 흐름은 HeroListController가 담당합니다.
public sealed class HeroListPopup : BasePopup
{
    private const string CharacterSpriteAssetFolder = "Assets/_Project/02_Prefab/CharacterPrefabs"; // 에디터 플레이 중 캐릭터 PNG를 찾을 기본 폴더
    private const string PopupAssetFolder = "Assets/_Project/03_Art/LobbyScene/Popup_UI"; // 에디터 플레이 중 상세 팝업 프리팹 참조 누락을 보정할 기본 폴더

    [SerializeField] private ScrollRect unlockedHeroScrollRect; // 해금된 히어로가 들어갈 목록
    [SerializeField] private ScrollRect lockedHeroScrollRect; // 아직 해금되지 않은 히어로가 들어갈 목록
    [SerializeField] private HeroListItemView heroItemPrefab; // Content 아래에 반복 생성할 히어로 카드 프리팹
    [SerializeField] private List<HeroSpriteEntry> heroSprites = new(); // HeroData.IconKey로 찾는 캐릭터 이미지 목록
    [SerializeField] private HeroDetailPopup commonDetailPopupPrefab; // 일반 등급 영웅 상세 팝업
    [SerializeField] private HeroDetailPopup rareDetailPopupPrefab; // 희귀 등급 영웅 상세 팝업
    [SerializeField] private HeroDetailPopup epicDetailPopupPrefab; // 에픽 등급 영웅 상세 팝업
    [SerializeField] private HeroDetailPopup legendaryDetailPopupPrefab; // 전설 등급 영웅 상세 팝업

    private readonly Dictionary<string, Sprite> spriteByKey = new(); // IconKey별 Sprite 빠른 조회 캐시

    public event Action Opened; // 팝업이 열렸을 때 Controller가 데이터 갱신을 시작하는 이벤트
    public event Action<HeroRosterEntry> HeroItemClicked; // 해금된 히어로 카드 클릭 이벤트

    // 팝업이 처음 생성될 때 정적 참조 캐시를 준비합니다.
    protected override void OnInitialized()
    {
        ResolveMissingDetailPopupPrefabs();
        RebuildSpriteCache();
    }

    // 팝업이 열릴 때마다 Controller에게 현재 데이터 기준 갱신을 요청합니다.
    protected override void OnOpened()
    {
        ResolveMissingDetailPopupPrefabs();
        RebuildSpriteCache();
        Opened?.Invoke();
    }

    // Controller가 넘긴 로스터를 화면 카드로 생성합니다.
    public void RenderHeroes(IReadOnlyList<HeroRosterEntry> heroes)
    {
        if (heroItemPrefab == null)
        {
            Debug.LogError("[HeroListPopup] ListItem_Hero 프리팹이 연결되지 않았습니다.", this);
            return;
        }

        if (!TryGetContent(unlockedHeroScrollRect, out Transform unlockedContent) || !TryGetContent(lockedHeroScrollRect, out Transform lockedContent))
        {
            Debug.LogError("[HeroListPopup] 해금/미해금 ScrollRect 또는 Content가 연결되지 않았습니다.", this);
            return;
        }

        ClearContent(unlockedContent);
        ClearContent(lockedContent);

        if (heroes == null)
            return;

        for (int i = 0; i < heroes.Count; i++)
        {
            HeroRosterEntry heroEntry = heroes[i];
            HeroDataRow hero = heroEntry.Hero;

            if (hero == null)
                continue;

            Transform parent = heroEntry.IsUnlocked ? unlockedContent : lockedContent;
            HeroListItemView item = Instantiate(heroItemPrefab, parent);

            item.gameObject.name = $"ListItem_Hero_{hero.CharacterName}";
            item.Bind(hero, ResolveHeroSprite(hero), heroEntry.Level, heroEntry.IsUnlocked);
            item.BindClick(heroEntry, HandleHeroItemClicked);
        }
    }

    // 영웅 등급에 맞는 상세 팝업 프리팹을 반환합니다.
    public HeroDetailPopup GetDetailPopupPrefab(Rarity rarity)
    {
        ResolveMissingDetailPopupPrefabs();

        return rarity switch
        {
            Rarity.Common => commonDetailPopupPrefab,
            Rarity.Rare => rareDetailPopupPrefab,
            Rarity.Epic => epicDetailPopupPrefab,
            Rarity.Legendary => legendaryDetailPopupPrefab,
            _ => null
        };
    }

    // HeroData의 IconKey, CharacterName, PrefabKey 순서로 캐릭터 이미지를 찾습니다.
    public Sprite ResolveHeroSprite(HeroDataRow hero)
    {
        if (hero == null)
            return null;

        if (TryGetCachedSprite(hero.IconKey, out Sprite sprite))
            return sprite;

        if (TryGetCachedSprite(hero.CharacterName, out sprite))
            return sprite;

        if (TryGetCachedSprite(hero.PrefabKey, out sprite))
            return sprite;

#if UNITY_EDITOR
        sprite = LoadEditorSprite(hero.IconKey);
        if (sprite != null)
            return sprite;

        sprite = LoadEditorSprite(hero.CharacterName);
        if (sprite != null)
            return sprite;

        sprite = LoadEditorSprite(hero.PrefabKey);
        if (sprite != null)
            return sprite;
#endif

        Debug.LogWarning($"[HeroListPopup] 캐릭터 이미지를 찾지 못했습니다. HeroId: {hero.HeroId}, IconKey: {hero.IconKey}, CharacterName: {hero.CharacterName}", this);
        return null;
    }

    // 카드 View에서 들어온 클릭 이벤트를 Controller로 전달합니다.
    private void HandleHeroItemClicked(HeroRosterEntry heroEntry)
    {
        HeroItemClicked?.Invoke(heroEntry);
    }

    // 상세 팝업 프리팹 참조가 Unity 직렬화 갱신 문제로 비어 있으면 에디터 에셋 경로에서 한 번 복구합니다.
    private void ResolveMissingDetailPopupPrefabs()
    {
#if UNITY_EDITOR
        commonDetailPopupPrefab ??= LoadDetailPopupPrefab("Hero_Detail_Common_PopUp");
        rareDetailPopupPrefab ??= LoadDetailPopupPrefab("Hero_Detail_Rare_PopUp");
        epicDetailPopupPrefab ??= LoadDetailPopupPrefab("Hero_Detail_Epic_PopUp");
        legendaryDetailPopupPrefab ??= LoadDetailPopupPrefab("Hero_Detail_Legendary_PopUp");
#endif
    }

#if UNITY_EDITOR
    // Popup_UI 폴더의 상세 팝업 프리팹에서 HeroDetailPopup 컴포넌트를 읽어옵니다.
    private static HeroDetailPopup LoadDetailPopupPrefab(string prefabName)
    {
        string assetPath = $"{PopupAssetFolder}/{prefabName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (prefab == null)
        {
            Debug.LogError($"[HeroListPopup] 상세 팝업 프리팹을 찾지 못했습니다. Path: {assetPath}");
            return null;
        }

        HeroDetailPopup popup = prefab.GetComponent<HeroDetailPopup>();
        if (popup == null)
            Debug.LogError($"[HeroListPopup] 상세 팝업 루트에 HeroDetailPopup 컴포넌트가 없습니다. Path: {assetPath}", prefab);

        return popup;
    }
#endif

    // 캐시에 등록된 Sprite를 키로 조회합니다.
    private bool TryGetCachedSprite(string key, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        return spriteByKey.TryGetValue(key, out sprite) && sprite != null;
    }

#if UNITY_EDITOR
    // 에디터 플레이 중에는 CharacterPrefabs 폴더의 PNG를 직접 Sprite로 읽어 수동 연결 누락을 보정합니다.
    private Sprite LoadEditorSprite(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string assetPath = $"{CharacterSpriteAssetFolder}/{key}.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (sprite != null)
            spriteByKey[key] = sprite;

        return sprite;
    }
#endif

    // 기존 샘플 카드나 이전에 생성된 카드를 제거합니다.
    private static void ClearContent(Transform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    // ScrollRect에 연결된 Content Transform을 가져옵니다.
    private static bool TryGetContent(ScrollRect scrollRect, out Transform content)
    {
        content = scrollRect != null ? scrollRect.content : null;
        return content != null;
    }

    // 테이블의 IconKey 문자열로 Sprite를 찾을 수 있게 Dictionary를 다시 만듭니다.
    private void RebuildSpriteCache()
    {
        spriteByKey.Clear();

        for (int i = 0; i < heroSprites.Count; i++)
        {
            HeroSpriteEntry entry = heroSprites[i];

            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Sprite == null)
                continue;

            spriteByKey[entry.Key] = entry.Sprite;
        }
    }

}

// HeroListPopup 프리팹에서 IconKey와 Sprite를 연결하기 위한 직렬화 항목입니다.
[Serializable]
public sealed class HeroSpriteEntry
{
    public string Key; // HeroData.IconKey와 같은 문자열
    public Sprite Sprite; // CharacterPrefabs 폴더의 캐릭터 이미지
}
