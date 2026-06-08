using NUnit.Framework;

// CurrencyHudPresenter가 MVP 계약을 지키는지 검증합니다.
public sealed class CurrencyHudPresenterTests
{
    // 초기화 시 저장된 재화 값을 화면 표시 상태로 변환해 View에 전달해야 합니다.
    [Test]
    public void Initialize_RendersCurrentCurrency()
    {
        SaveData saveData = SaveData.CreateDefault();
        saveData.Currency.Gold = 1234567;
        saveData.Currency.Gem = 89;

        CurrencyProgress model = new CurrencyProgress(saveData);
        FakeCurrencyHudView view = new FakeCurrencyHudView();
        CurrencyHudPresenter presenter = new CurrencyHudPresenter(model, view);

        presenter.Initialize();

        Assert.That(view.RenderCount, Is.EqualTo(1));
        Assert.That(view.LastState.GoldText, Is.EqualTo("1,234,567"));
        Assert.That(view.LastState.GemText, Is.EqualTo("89"));
    }

    // Model 값이 바뀌면 Presenter가 다시 ViewState를 만들어 View를 갱신해야 합니다.
    [Test]
    public void CurrencyChange_RendersUpdatedCurrency()
    {
        SaveData saveData = SaveData.CreateDefault();
        CurrencyProgress model = new CurrencyProgress(saveData);
        FakeCurrencyHudView view = new FakeCurrencyHudView();
        CurrencyHudPresenter presenter = new CurrencyHudPresenter(model, view);

        presenter.Initialize();
        model.AddGold(2500);
        model.AddGem(7);

        Assert.That(view.LastState.GoldText, Is.EqualTo("2,500"));
        Assert.That(view.LastState.GemText, Is.EqualTo("7"));
        Assert.That(view.RenderCount, Is.GreaterThanOrEqualTo(3));
    }

    // Initialize가 두 번 호출되어도 구독이 중복 생성되면 안 됩니다.
    [Test]
    public void InitializeTwice_DoesNotDuplicateSubscriptions()
    {
        SaveData saveData = SaveData.CreateDefault();
        CurrencyProgress model = new CurrencyProgress(saveData);
        FakeCurrencyHudView view = new FakeCurrencyHudView();
        CurrencyHudPresenter presenter = new CurrencyHudPresenter(model, view);

        presenter.Initialize();
        presenter.Initialize();
        int renderCountAfterInitialize = view.RenderCount;

        model.AddGold(1);

        Assert.That(renderCountAfterInitialize, Is.EqualTo(1));
        Assert.That(view.RenderCount, Is.EqualTo(2));
    }

    // Dispose 이후에는 Model 값이 바뀌어도 파괴된 View를 다시 갱신하면 안 됩니다.
    [Test]
    public void Dispose_StopsRendering()
    {
        SaveData saveData = SaveData.CreateDefault();
        CurrencyProgress model = new CurrencyProgress(saveData);
        FakeCurrencyHudView view = new FakeCurrencyHudView();
        CurrencyHudPresenter presenter = new CurrencyHudPresenter(model, view);

        presenter.Initialize();
        presenter.Dispose();
        int renderCountAfterDispose = view.RenderCount;

        model.AddGold(10);
        model.AddGem(10);

        Assert.That(view.RenderCount, Is.EqualTo(renderCountAfterDispose));
    }

    // Presenter는 null Model이나 null View로 생성되면 즉시 실패해야 합니다.
    [Test]
    public void Constructor_RejectsNullDependencies()
    {
        SaveData saveData = SaveData.CreateDefault();
        CurrencyProgress model = new CurrencyProgress(saveData);
        FakeCurrencyHudView view = new FakeCurrencyHudView();

        Assert.That(() => new CurrencyHudPresenter(null, view), Throws.ArgumentNullException);
        Assert.That(() => new CurrencyHudPresenter(model, null), Throws.ArgumentNullException);
    }

    // 테스트용 View입니다. Unity UI 없이 Presenter가 어떤 표시 상태를 넘겼는지 기록합니다.
    private sealed class FakeCurrencyHudView : ICurrencyHudView
    {
        public int RenderCount { get; private set; } // Render 호출 횟수
        public CurrencyHudViewState LastState { get; private set; } // 마지막 표시 상태

        // Presenter가 넘긴 표시 상태를 저장합니다.
        public void Render(CurrencyHudViewState viewState)
        {
            RenderCount += 1;
            LastState = viewState;
        }

        // 테스트 View는 항상 준비된 상태로 둡니다.
        public bool IsReady()
        {
            return true;
        }
    }
}
