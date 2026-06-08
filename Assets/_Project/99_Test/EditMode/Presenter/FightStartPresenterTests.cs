using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

// FightStartPresenter가 로비 시작 버튼 입력을 밤 방어 씬 로드 요청으로 바꾸는지 검증합니다.
public sealed class FightStartPresenterTests
{
    // 초기화 시 View를 클릭 가능한 상태로 만들고 클릭 이벤트를 구독해야 합니다.
    [Test]
    public void Initialize_EnablesView()
    {
        FakeFightStartView view = new FakeFightStartView();
        FightStartPresenter presenter = new FightStartPresenter(view, () => UniTask.FromResult(true));

        presenter.Initialize();

        Assert.That(view.IsInteractable, Is.True);
    }

    // 버튼 클릭 시 밤 방어 씬 로드 요청 함수가 한 번 호출되어야 합니다.
    [Test]
    public void Click_RequestsNightDefenseLoad()
    {
        FakeFightStartView view = new FakeFightStartView();
        int requestCount = 0;
        FightStartPresenter presenter = new FightStartPresenter(view, () =>
        {
            requestCount += 1;
            return UniTask.FromResult(true);
        });

        presenter.Initialize();
        view.Click();

        Assert.That(requestCount, Is.EqualTo(1));
        Assert.That(view.IsInteractable, Is.False);
    }

    // 씬 로드가 거부되면 다시 클릭 가능한 상태로 되돌려야 합니다.
    [Test]
    public void LoadRejected_ReenablesView()
    {
        FakeFightStartView view = new FakeFightStartView();
        FightStartPresenter presenter = new FightStartPresenter(view, () => UniTask.FromResult(false));

        presenter.Initialize();
        view.Click();

        Assert.That(view.IsInteractable, Is.True);
    }

    // Dispose 이후에는 View 클릭이 로드 요청으로 이어지면 안 됩니다.
    [Test]
    public void Dispose_StopsClickHandling()
    {
        FakeFightStartView view = new FakeFightStartView();
        int requestCount = 0;
        FightStartPresenter presenter = new FightStartPresenter(view, () =>
        {
            requestCount += 1;
            return UniTask.FromResult(true);
        });

        presenter.Initialize();
        presenter.Dispose();
        view.Click();

        Assert.That(requestCount, Is.EqualTo(0));
    }

    // Presenter는 null View나 null 로드 함수로 생성되면 즉시 실패해야 합니다.
    [Test]
    public void Constructor_RejectsNullDependencies()
    {
        FakeFightStartView view = new FakeFightStartView();

        Assert.That(() => new FightStartPresenter(null, () => UniTask.FromResult(true)), Throws.ArgumentNullException);
        Assert.That(() => new FightStartPresenter(view, null), Throws.ArgumentNullException);
    }

    // 테스트용 View입니다. Unity UI 없이 Presenter의 입력 가능 상태와 클릭 이벤트만 검증합니다.
    private sealed class FakeFightStartView : IFightStartView
    {
        public event Action Clicked; // Presenter가 구독하는 클릭 이벤트

        public bool IsInteractable { get; private set; } // 마지막 입력 가능 상태

        // Presenter가 버튼 입력 가능 여부를 바꾼 값을 저장합니다.
        public void SetInteractable(bool isInteractable)
        {
            IsInteractable = isInteractable;
        }

        // 테스트에서 버튼 클릭을 직접 발생시킵니다.
        public void Click()
        {
            Clicked?.Invoke();
        }
    }
}
