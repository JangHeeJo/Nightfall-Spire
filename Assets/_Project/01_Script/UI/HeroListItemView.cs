using TMPro;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 히어로 카드 한 칸의 View입니다.
// 카드 내부 UI 참조를 프리팹에 고정해두고, 팝업에서는 Bind만 호출하게 합니다.
public sealed class HeroListItemView : MonoBehaviour
{
    [SerializeField] private Graphic cardFrameGraphic; // 등급 색상을 직접 받을 카드 프레임 그래픽
    [SerializeField] private Button cardFrameButton; // 프레임이 Button으로 구성된 경우 targetGraphic 색상도 같이 맞춥니다.
    [SerializeField] private Image characterImage; // 캐릭터 대표 이미지
    [SerializeField] private TextMeshProUGUI levelText; // 현재 히어로 레벨 표시

    private UnityAction clickCallback; // 카드 재바인딩 때 이전 클릭 연결을 제거하기 위한 캐시

    // 히어로 데이터 하나를 카드 UI에 반영합니다.
    public void Bind(HeroDataRow hero, Sprite sprite, int level, bool isUnlocked)
    {
        SetFrameColor(GetRarityColor(hero.Rarity));
        SetCharacterImage(sprite, isUnlocked);
        SetLevel(level, isUnlocked);
    }

    // 해금된 카드 클릭 시 외부에서 넘긴 선택 처리를 실행합니다.
    public void BindClick(HeroRosterEntry heroEntry, Action<HeroRosterEntry> onClicked)
    {
        if (cardFrameButton == null)
            return;

        if (clickCallback != null)
            cardFrameButton.onClick.RemoveListener(clickCallback);

        cardFrameButton.interactable = heroEntry.Hero != null && heroEntry.IsUnlocked && onClicked != null;

        if (!cardFrameButton.interactable)
        {
            clickCallback = null;
            return;
        }

        clickCallback = () => onClicked(heroEntry);
        cardFrameButton.onClick.AddListener(clickCallback);
    }

    // 등급에 맞는 프레임 색상을 적용합니다.
    private void SetFrameColor(Color color)
    {
        if (cardFrameGraphic != null)
            cardFrameGraphic.color = color;

        if (cardFrameButton == null || cardFrameButton.targetGraphic == null)
            return;

        cardFrameButton.targetGraphic.color = color;
        ColorBlock colorBlock = cardFrameButton.colors;
        colorBlock.normalColor = color;
        colorBlock.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colorBlock.pressedColor = Color.Lerp(color, Color.black, 0.12f);
        colorBlock.selectedColor = colorBlock.highlightedColor;
        cardFrameButton.colors = colorBlock;
    }

    // 캐릭터 이미지를 넣고, 미해금 상태면 어둡게 표시합니다.
    private void SetCharacterImage(Sprite sprite, bool isUnlocked)
    {
        if (characterImage == null)
            return;

        characterImage.enabled = sprite != null;
        characterImage.sprite = sprite;
        characterImage.preserveAspect = true;
        characterImage.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    // 해금된 카드에서만 레벨 텍스트를 켜고 현재 레벨을 표시합니다.
    private void SetLevel(int level, bool isUnlocked)
    {
        if (levelText == null)
            return;

        levelText.gameObject.SetActive(isUnlocked);
        levelText.text = $"Lv.{level}";
    }

    // 등급별 카드 프레임 색상입니다. 아케이드 UI에 맞춰 채도는 살리고 너무 원색으로 튀지 않게 잡습니다.
    private static Color GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => new Color(0.48f, 0.92f, 0.50f, 1f),
            Rarity.Rare => new Color(0.38f, 0.70f, 1.00f, 1f),
            Rarity.Epic => new Color(0.78f, 0.48f, 1.00f, 1f),
            Rarity.Legendary => new Color(1.00f, 0.82f, 0.28f, 1f),
            _ => Color.white
        };
    }
}
