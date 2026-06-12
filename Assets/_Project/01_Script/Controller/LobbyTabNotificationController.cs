using System;
using System.Collections.Generic;
using R3;

// 로비 하단 탭의 빨간 알림 점 상태를 현재 진행 데이터 기준으로 갱신합니다.
// LobbyScreen은 오브젝트 표시만 맡고, 어떤 탭에 알림이 필요한지 판단하는 책임은 이 Controller가 가집니다.
public sealed class LobbyTabNotificationController : IDisposable
{
    private const string MagicCommandKey = "BottomButton_Magic"; // 소환/마법 계열 하단 탭
    private const string HeroCommandKey = "BottomButton_Hero"; // 영웅 목록 하단 탭
    private const string SpireCommandKey = "BottomButton_Spire"; // 성채/스파이어 하단 탭
    private const string BattleCommandKey = "BottomButton_Battle"; // 전투 시작 하단 탭
    private const string ShopCommandKey = "BottomButton_Shop"; // 상점 하단 탭

    private readonly GameContext context; // 알림 조건을 판단할 현재 게임 상태
    private readonly ILobbyScreenView view; // 하단 탭 알림 표시 View
    private readonly List<IDisposable> subscriptions = new(); // 진행값 변경 구독 해제용

    private bool isDisposed; // 씬 종료 후 갱신 방지

    // 알림 판단에 필요한 현재 게임 상태와 로비 화면 View를 받습니다.
    public LobbyTabNotificationController(GameContext context, ILobbyScreenView view)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.view = view ?? throw new ArgumentNullException(nameof(view));
    }

    // 현재 상태를 즉시 반영하고, 알림 조건에 영향을 주는 진행값 변경을 구독합니다.
    public void Initialize()
    {
        if (isDisposed)
            throw new ObjectDisposedException(nameof(LobbyTabNotificationController));

        if (subscriptions.Count > 0)
            return;

        RefreshAlerts();

        subscriptions.Add(context.DayProgress.CitadelFloorCount.Subscribe(_ => RefreshAlerts()));
        subscriptions.Add(context.DayProgress.MagicLibraryUnlocked.Subscribe(_ => RefreshAlerts()));
        subscriptions.Add(context.CurrencyProgress.Gold.Subscribe(_ => RefreshAlerts()));
        subscriptions.Add(context.CurrencyProgress.Gem.Subscribe(_ => RefreshAlerts()));
    }

    // 각 하단 탭의 알림 점을 현재 조건으로 다시 계산합니다.
    private void RefreshAlerts()
    {
        if (isDisposed)
            return;

        view.SetCommandAlertVisible(MagicCommandKey, HasMagicNotification());
        view.SetCommandAlertVisible(HeroCommandKey, HasHeroNotification());
        view.SetCommandAlertVisible(SpireCommandKey, HasSpireNotification());
        view.SetCommandAlertVisible(BattleCommandKey, HasBattleNotification());
        view.SetCommandAlertVisible(ShopCommandKey, HasShopNotification());
    }

    // 마법/소환 탭은 추후 소환권, 무료 소환, 마법 기능 해금 조건이 생기면 여기서 판단합니다.
    private bool HasMagicNotification()
    {
        return false;
    }

    // 테이블 조건으로 사용 가능해졌지만 아직 저장 데이터상 수동 해금 처리되지 않은 영웅이 있으면 알림을 켭니다.
    private bool HasHeroNotification()
    {
        HeroRosterService heroRosterService = context.HeroRosterService;
        if (heroRosterService == null)
            return false;

        IReadOnlyList<HeroRosterEntry> heroes = heroRosterService.BuildRoster();
        for (int i = 0; i < heroes.Count; i++)
        {
            HeroRosterEntry entry = heroes[i];
            if (entry.Hero == null || !entry.IsUnlocked)
                continue;

            if (!context.HeroCollectionProgress.IsManuallyUnlocked(entry.Hero.HeroId))
                return true;
        }

        return false;
    }

    // 성채 탭은 추후 층 해금/시설 강화 가능 여부가 정식으로 정리되면 여기서 판단합니다.
    private bool HasSpireNotification()
    {
        return false;
    }

    // 전투 탭은 추후 무료 보상, 전투 준비 완료, 티켓 충전 같은 조건이 생기면 여기서 판단합니다.
    private bool HasBattleNotification()
    {
        return false;
    }

    // 상점 탭은 추후 무료 상품, 일일 갱신, 광고 보상 같은 조건이 생기면 여기서 판단합니다.
    private bool HasShopNotification()
    {
        return false;
    }

    // 씬이 내려갈 때 진행값 구독을 정리합니다.
    public void Dispose()
    {
        if (isDisposed)
            return;

        for (int i = 0; i < subscriptions.Count; i++)
            subscriptions[i]?.Dispose();

        subscriptions.Clear();
        isDisposed = true;
    }
}
