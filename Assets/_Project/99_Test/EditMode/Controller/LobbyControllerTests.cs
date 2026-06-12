using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

// LobbyController가 로비 화면 입력을 명령 라우터로 전달하는지 검증합니다.
public sealed class LobbyControllerTests
{
    // 초기화 시 로비 화면 입력을 받을 준비를 해야 합니다.
    [Test]
    public void Initialize_ConnectsLobbyInput()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();
        LobbyController controller = new LobbyController(view, _ => UniTask.FromResult(true));

        controller.Initialize();

        Assert.That(view.IsReady(), Is.True);
    }

    // 밤 방어 시작 요청이 들어오면 명령 라우터가 한 번 호출되어야 합니다.
    [Test]
    public void NightDefenseRequest_ExecutesCommandRouter()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();
        int requestCount = 0;
        LobbyController controller = new LobbyController(view, _ =>
        {
            requestCount += 1;
            return UniTask.FromResult(true);
        });

        controller.Initialize();
        view.RequestCommand("BottomButton_Battle");

        Assert.That(requestCount, Is.EqualTo(1));
        Assert.That(view.LastInteractableCommandKey, Is.EqualTo("BottomButton_Battle"));
    }

    // 명령 처리가 끝나면 입력을 다시 활성화해야 합니다.
    [Test]
    public void CommandFinished_ReenablesInput()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();
        LobbyController controller = new LobbyController(view, _ => UniTask.FromResult(false));

        controller.Initialize();
        view.RequestCommand("BottomButton_Battle");

        Assert.That(view.LastInteractableState, Is.True);
    }

    // Dispose 이후에는 로비 화면 입력을 더 이상 처리하면 안 됩니다.
    [Test]
    public void Dispose_StopsHandlingScreenInput()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();
        int requestCount = 0;
        LobbyController controller = new LobbyController(view, _ =>
        {
            requestCount += 1;
            return UniTask.FromResult(true);
        });

        controller.Initialize();
        controller.Dispose();
        view.RequestCommand("BottomButton_Battle");

        Assert.That(requestCount, Is.EqualTo(0));
    }

    // Hero 버튼 요청도 같은 명령 실행 라우터로 전달되어야 합니다.
    [Test]
    public void HeroListRequest_ExecutesCommandRouter()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();
        int openCount = 0;
        LobbyController controller = new LobbyController(
            view,
            commandKey =>
            {
                if (commandKey == "BottomButton_Hero")
                    openCount += 1;

                return UniTask.FromResult(true);
            });

        controller.Initialize();
        view.RequestCommand("BottomButton_Hero");

        Assert.That(openCount, Is.EqualTo(1));
    }

    // Controller는 null View나 null 라우터로 생성되면 즉시 실패해야 합니다.
    [Test]
    public void Constructor_RejectsNullDependencies()
    {
        FakeLobbyScreenView view = new FakeLobbyScreenView();

        Assert.That(() => new LobbyController(null, _ => UniTask.FromResult(true)), Throws.ArgumentNullException);
        Assert.That(() => new LobbyController(view, null), Throws.ArgumentNullException);
    }

    // 테스트용 로비 화면 View입니다. Unity UI 없이 화면 입력과 활성화 상태만 기록합니다.
    private sealed class FakeLobbyScreenView : ILobbyScreenView
    {
        public event Action<string> CommandRequested; // 로비 명령 요청

        public string LastInteractableCommandKey { get; private set; } // 마지막으로 활성화 상태가 바뀐 명령 키
        public bool LastInteractableState { get; private set; } // 마지막 입력 가능 상태
        public string FocusedCommandKey { get; private set; } // 마지막으로 선택 표시된 명령 키
        public string AlertCommandKey { get; private set; } // 마지막으로 알림 상태가 바뀐 명령 키
        public bool AlertVisible { get; private set; } // 마지막 알림 표시 상태

        // 테스트 View는 항상 준비된 상태로 둡니다.
        public bool IsReady()
        {
            return true;
        }

        // Controller가 바꾼 명령 입력 가능 상태를 저장합니다.
        public void SetCommandInteractable(string commandKey, bool isInteractable)
        {
            LastInteractableCommandKey = commandKey;
            LastInteractableState = isInteractable;
        }

        // 테스트에서는 Focus 요청이 들어온 마지막 명령 키만 기록합니다.
        public void SetFocusedCommand(string commandKey)
        {
            FocusedCommandKey = commandKey;
        }

        // 테스트에서는 알림 표시 요청이 들어온 마지막 상태만 기록합니다.
        public void SetCommandAlertVisible(string commandKey, bool isVisible)
        {
            AlertCommandKey = commandKey;
            AlertVisible = isVisible;
        }

        // 테스트에서 로비 명령 요청을 직접 발생시킵니다.
        public void RequestCommand(string commandKey)
        {
            CommandRequested?.Invoke(commandKey);
        }
    }
}
