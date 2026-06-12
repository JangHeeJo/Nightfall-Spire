using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 영웅 상세 팝업의 입력과 표시 상태를 제어하는 Controller입니다.
// HeroDetailPopup(View)은 버튼 이벤트와 UI 반영만 맡고, PREV/NEXT/닫기 흐름은 이 Controller가 담당합니다.
public sealed class HeroDetailController : IDisposable
{
    private readonly HeroDetailPopup view; // 상세 팝업 View
    private HeroDetailPopupData popupData; // 상세 팝업 표시와 이동에 필요한 데이터
    private int currentIndex; // 현재 표시 중인 해금 영웅 인덱스
    private bool isDisposed; // 팝업 파괴 이후 이벤트 처리를 막습니다.

    // 제어할 상세 View를 받습니다.
    public HeroDetailController(HeroDetailPopup view)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
    }

    // View 이벤트 구독을 시작합니다.
    public void Initialize()
    {
        view.DataBound += Bind;
        view.PreviousRequested += ShowPreviousHero;
        view.NextRequested += ShowNextHero;
        view.BackRequested += CloseDetail;
    }

    // 선택한 영웅 데이터를 상세 화면에 반영합니다.
    public void Bind(HeroDetailPopupData data)
    {
        if (isDisposed)
            return;

        popupData = data;
        currentIndex = data != null ? data.SelectedIndex : -1;
        RenderCurrentHero();
    }

    // 현재 인덱스의 영웅 데이터를 ViewState로 변환해 View에 전달합니다.
    private void RenderCurrentHero()
    {
        if (popupData == null || !popupData.TryGetEntry(currentIndex, out HeroRosterEntry entry))
        {
            Debug.LogError("[HeroDetailController] 표시할 영웅 데이터가 없습니다.");
            return;
        }

        HeroDataRow hero = entry.Hero;
        HeroDetailViewState viewState = new HeroDetailViewState(
            hero.CharacterName,
            $"Lv.{entry.Level}",
            FormatRole(hero.HeroRole),
            hero.BaseHealth.ToString(),
            hero.BaseDefense.ToString(),
            hero.BaseAttack.ToString(),
            hero.BaseAttackSpeed.ToString("0.##"),
            hero.BaseAttackRange.ToString("0.##"),
            popupData.ResolveSprite(hero),
            popupData.EntryCount > 1);

        view.Render(viewState);
    }

    // 이전 해금 영웅으로 이동합니다.
    // 등급별 프리팹 외형까지 바뀌어야 하므로 상세 View를 직접 바꾸지 않고 목록 Controller에 재오픈을 요청합니다.
    private void ShowPreviousHero()
    {
        int count = popupData?.EntryCount ?? 0;

        if (count <= 1)
            return;

        int nextIndex = (currentIndex - 1 + count) % count;
        popupData.RequestOpenHeroAtIndexAsync(nextIndex).Forget();
    }

    // 다음 해금 영웅으로 이동합니다.
    // 등급별 프리팹 외형까지 바뀌어야 하므로 상세 View를 직접 바꾸지 않고 목록 Controller에 재오픈을 요청합니다.
    private void ShowNextHero()
    {
        int count = popupData?.EntryCount ?? 0;

        if (count <= 1)
            return;

        int nextIndex = (currentIndex + 1) % count;
        popupData.RequestOpenHeroAtIndexAsync(nextIndex).Forget();
    }

    // 상세 팝업을 닫고 아래에 남아 있는 영웅 목록으로 돌아갑니다.
    private void CloseDetail()
    {
        view.RequestCloseAsync().Forget();
    }

    // 내부 enum 값을 UI 표시 문자열로 바꿉니다.
    private static string FormatRole(HeroRole role)
    {
        return role switch
        {
            HeroRole.Melee => "근접",
            HeroRole.Ranged => "원거리",
            _ => role.ToString()
        };
    }

    // 팝업이 파괴될 때 View 이벤트 구독을 해제합니다.
    public void Dispose()
    {
        if (isDisposed)
            return;

        view.DataBound -= Bind;
        view.PreviousRequested -= ShowPreviousHero;
        view.NextRequested -= ShowNextHero;
        view.BackRequested -= CloseDetail;
        isDisposed = true;
    }
}

// HeroDetailPopup이 화면에 표시할 완성된 상태입니다.
public readonly struct HeroDetailViewState
{
    public string NameText { get; } // 영웅 이름
    public string LevelText { get; } // 레벨 표시
    public string RoleText { get; } // 근접/원거리 표시
    public string HealthText { get; } // 체력 표시
    public string DefenseText { get; } // 방어력 표시
    public string AttackText { get; } // 공격력 표시
    public string AttackSpeedText { get; } // 공격속도 표시
    public string AttackRangeText { get; } // 공격범위 표시
    public Sprite CharacterSprite { get; } // 캐릭터 이미지
    public bool CanNavigate { get; } // 이전/다음 버튼 활성 여부

    // 상세 팝업에 표시할 값을 한 번에 보관합니다.
    public HeroDetailViewState(
        string nameText,
        string levelText,
        string roleText,
        string healthText,
        string defenseText,
        string attackText,
        string attackSpeedText,
        string attackRangeText,
        Sprite characterSprite,
        bool canNavigate)
    {
        NameText = nameText;
        LevelText = levelText;
        RoleText = roleText;
        HealthText = healthText;
        DefenseText = defenseText;
        AttackText = attackText;
        AttackSpeedText = attackSpeedText;
        AttackRangeText = attackRangeText;
        CharacterSprite = characterSprite;
        CanNavigate = canNavigate;
    }
}
