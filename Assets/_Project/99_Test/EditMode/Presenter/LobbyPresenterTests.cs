using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

// LobbyPresenter가 로비 화면 입력을 GameFlowController 흐름으로 연결하는지 검증합니다.
public sealed class LobbyPresenterTests
{
    // 초기화 시 로비 화면의 밤 방어 시작 입력을 활성화해야 합니다.
    [Test]
    public void Initialize_EnablesNightDefenseStart()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();
        LobbyPresenter presenter = new LobbyPresenter(view, () => UniTask.FromResult(true));

        presenter.Initialize();

        Assert.That(view.IsNightDefenseStartInteractable, Is.True);
    }

    // 밤 방어 시작 요청이 들어오면 씬 로드 함수가 한 번 호출되어야 합니다.
    [Test]
    public void NightDefenseRequest_LoadsNightDefense()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();
        int requestCount = 0;
        LobbyPresenter presenter = new LobbyPresenter(view, () =>
        {
            requestCount += 1;
            return UniTask.FromResult(true);
        });

        presenter.Initialize();
        view.RequestNightDefense();

        Assert.That(requestCount, Is.EqualTo(1));
        Assert.That(view.IsNightDefenseStartInteractable, Is.False);
    }

    // 씬 로드가 거부되면 시작 입력을 다시 활성화해야 합니다.
    [Test]
    public void LoadRejected_ReenablesNightDefenseStart()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();
        LobbyPresenter presenter = new LobbyPresenter(view, () => UniTask.FromResult(false));

        presenter.Initialize();
        view.RequestNightDefense();

        Assert.That(view.IsNightDefenseStartInteractable, Is.True);
    }

    // Dispose 이후에는 로비 화면 입력을 더 이상 처리하면 안 됩니다.
    [Test]
    public void Dispose_StopsHandlingScreenInput()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();
        int requestCount = 0;
        LobbyPresenter presenter = new LobbyPresenter(view, () =>
        {
            requestCount += 1;
            return UniTask.FromResult(true);
        });

        presenter.Initialize();
        presenter.Dispose();
        view.RequestNightDefense();

        Assert.That(requestCount, Is.EqualTo(0));
    }

    // Presenter는 null View나 null 로드 함수로 생성되면 즉시 실패해야 합니다.
    [Test]
    public void Constructor_RejectsNullDependencies()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();

        Assert.That(() => new LobbyPresenter(null, () => UniTask.FromResult(true)), Throws.ArgumentNullException);
        Assert.That(() => new LobbyPresenter(view, null), Throws.ArgumentNullException);
    }

    // 테스트용 로비 화면 View입니다. Unity UI 없이 화면 입력과 활성화 상태만 기록합니다.
    private sealed class FakeLobbyScreenView : ILobbyScreenView
    {
        public event Action NightDefenseRequested; // 밤 방어 시작 요청

        public bool IsNightDefenseStartInteractable { get; private set; } // 마지막 시작 버튼 활성화 상태

        // 테스트 View는 항상 준비된 상태로 둡니다.
        public bool IsReady()
        {
            return true;
        }

        // Presenter가 바꾼 밤 방어 시작 입력 가능 상태를 저장합니다.
        public void SetNightDefenseStartInteractable(bool isInteractable)
        {
            IsNightDefenseStartInteractable = isInteractable;
        }

        // 테스트에서 밤 방어 시작 요청을 직접 발생시킵니다.
        public void RequestNightDefense()
        {
            NightDefenseRequested?.Invoke();
        }
    }
}
