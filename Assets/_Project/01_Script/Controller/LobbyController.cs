using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 로비 화면 입력을 처리하는 Controller입니다.
// View는 어떤 버튼이 눌렸는지만 알려주고, 실제 팝업/씬 전환 결정은 이 Controller가 Root 라우터에 요청합니다.
public sealed class LobbyController : IDisposable
{
    private readonly ILobbyScreenView view; // 로비 화면 View
    private readonly Func<string, UniTask<bool>> executeCommandAsync; // 로비 명령 실행 라우터

    private bool isInitialized; // 중복 초기화 방지
    private bool isDisposed; // Dispose 이후 비동기 콜백 방지
    private bool isExecutingCommand; // 명령 실행 중 중복 입력 방지

    // 로비 View와 명령 실행 라우터를 받습니다.
    public LobbyController(
        ILobbyScreenView view,
        Func<string, UniTask<bool>> executeCommandAsync)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.executeCommandAsync = executeCommandAsync ?? throw new ArgumentNullException(nameof(executeCommandAsync));
    }

    // 로비 화면 입력 구독을 시작합니다.
    public void Initialize()
    {
        if (isInitialized || isDisposed)
            return;

        if (!view.IsReady())
            Debug.LogWarning("[LobbyController] LobbyScreen의 버튼 참조가 완전히 준비되지 않았습니다.");

        view.CommandRequested += OnCommandRequested;
        isInitialized = true;
    }

    // View에서 전달한 버튼 이름을 로비 명령으로 실행합니다.
    private void OnCommandRequested(string commandKey)
    {
        ExecuteCommandAsync(commandKey).Forget();
    }

    // 명령 실행 중에는 같은 프레임의 중복 입력을 막고, 처리 후 버튼을 다시 활성화합니다.
    private async UniTaskVoid ExecuteCommandAsync(string commandKey)
    {
        if (isExecutingCommand || isDisposed || string.IsNullOrWhiteSpace(commandKey))
            return;

        isExecutingCommand = true;
        view.SetCommandInteractable(commandKey, false);

        try
        {
            await executeCommandAsync(commandKey);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[LobbyController] 로비 명령 처리 중 예외 발생: {commandKey}\n{exception}");
        }
        finally
        {
            if (!isDisposed)
                view.SetCommandInteractable(commandKey, true);

            isExecutingCommand = false;
        }
    }

    // 로비 화면 입력 구독을 해제합니다.
    public void Dispose()
    {
        if (isDisposed)
            return;

        view.CommandRequested -= OnCommandRequested;
        isDisposed = true;
    }
}
