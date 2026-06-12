using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 히어로 목록 팝업의 흐름을 제어하는 Controller입니다.
// HeroListPopup(View)은 화면 생성과 이벤트 전달만 맡고, 데이터 조회와 상세 팝업 오픈은 이 Controller가 담당합니다.
public sealed class HeroListController : IDisposable
{
    private readonly HeroListPopup view; // 히어로 목록 팝업 View
    private readonly GameContext context; // 현재 게임 상태와 서비스 묶음
    private readonly PopupManager popupManager; // 상세 팝업 생성 관리자
    private readonly List<HeroRosterEntry> unlockedHeroes = new(); // 상세 PREV/NEXT 기준이 되는 해금 영웅 목록

    private bool isDisposed; // 팝업 파괴 이후 이벤트 처리를 막습니다.

    // 히어로 목록 제어에 필요한 View, Model/Service, 팝업 관리자를 받습니다.
    public HeroListController(HeroListPopup view, GameContext context, PopupManager popupManager)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.popupManager = popupManager ?? throw new ArgumentNullException(nameof(popupManager));
    }

    // View 이벤트 구독을 시작합니다.
    public void Initialize()
    {
        view.Opened += Refresh;
        view.HeroItemClicked += OnHeroItemClicked;
    }

    // 팝업이 열릴 때마다 현재 저장 데이터와 테이블 기준으로 목록을 다시 구성합니다.
    public void Refresh()
    {
        if (isDisposed)
            return;

        HeroRosterService heroRosterService = context.HeroRosterService;
        if (heroRosterService == null)
        {
            Debug.LogError("[HeroListController] HeroRosterService가 준비되지 않아 히어로 목록을 만들 수 없습니다.");
            return;
        }

        IReadOnlyList<HeroRosterEntry> heroes = heroRosterService.BuildRoster();
        unlockedHeroes.Clear();

        for (int i = 0; i < heroes.Count; i++)
        {
            if (heroes[i].IsUnlocked)
                unlockedHeroes.Add(heroes[i]);
        }

        view.RenderHeroes(heroes);
    }

    // 해금된 영웅 카드를 클릭하면 등급에 맞는 상세 팝업을 엽니다.
    private void OnHeroItemClicked(HeroRosterEntry heroEntry)
    {
        OpenHeroDetailAsync(heroEntry, PopupOpenPolicy.Stack).Forget();
    }

    // PopupManager에 상세 팝업 생성을 요청하고, 열기 전에 선택 영웅 데이터를 주입합니다.
    private async UniTask OpenHeroDetailAsync(HeroRosterEntry heroEntry, PopupOpenPolicy openPolicy)
    {
        if (isDisposed || heroEntry.Hero == null || !heroEntry.IsUnlocked)
            return;

        HeroDetailPopup popupPrefab = view.GetDetailPopupPrefab(heroEntry.Hero.Rarity);
        if (popupPrefab == null)
        {
            Debug.LogError($"[HeroListController] {heroEntry.Hero.Rarity} 등급 상세 팝업 프리팹이 연결되지 않았습니다.");
            return;
        }

        int selectedIndex = unlockedHeroes.IndexOf(heroEntry);
        if (selectedIndex < 0)
        {
            Debug.LogError($"[HeroListController] 선택한 영웅이 해금 목록에 없습니다. HeroId: {heroEntry.Hero.HeroId}");
            return;
        }

        HeroDetailPopupData popupData = new HeroDetailPopupData(
            unlockedHeroes,
            selectedIndex,
            view.ResolveHeroSprite,
            OpenHeroDetailAtIndexAsync);

        PopupRequest<HeroDetailPopup> request = new PopupRequest<HeroDetailPopup>(
            $"HeroDetail_{heroEntry.Hero.HeroId}",
            popupPrefab,
            openPolicy,
            PopupLayerSlot.Overlay,
            PopupPriority.Normal,
            true,
            popup => popup.Bind(popupData));

        await popupManager.OpenAsync(request);
    }

    // 상세 팝업의 PREV/NEXT 요청을 받아 현재 상세 팝업만 새 등급 프리팹으로 교체합니다.
    private UniTask OpenHeroDetailAtIndexAsync(int selectedIndex)
    {
        if (isDisposed || selectedIndex < 0 || selectedIndex >= unlockedHeroes.Count)
            return UniTask.CompletedTask;

        return OpenHeroDetailAsync(unlockedHeroes[selectedIndex], PopupOpenPolicy.ReplaceTop);
    }

    // 팝업이 파괴될 때 View 이벤트 구독을 해제합니다.
    public void Dispose()
    {
        if (isDisposed)
            return;

        view.Opened -= Refresh;
        view.HeroItemClicked -= OnHeroItemClicked;
        isDisposed = true;
    }
}
