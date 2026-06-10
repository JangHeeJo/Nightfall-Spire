using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 로비 화면 단위 Presenter입니다.
// 버튼별 기능을 직접 알지 않고, View가 전달한 명령 키를 로비 Root의 라우터로 넘깁니다.
public sealed class LobbyPresenter : IDisposable
{
    private readonly ILobbyScreenView view; // 로비 화면 View
    private readonly Func<string, UniTask<bool>> executeCommandAsync; // 로비 명령 실행 라우터

    private bool isInitialized; // 중복 초기화 방지
    private bool isDisposed; // Dispose 이후 비동기 콜백 방지
    private bool isExecutingCommand; // 명령 실행 중 중복 입력 방지

    // 로비 View와 명령 실행 라우터를 주입받습니다.
    public LobbyPresenter(
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
            Debug.LogWarning("[LobbyPresenter] LobbyScreen의 버튼 참조가 완전히 준비되지 않았습니다.");

        view.CommandRequested += OnCommandRequested;
        isInitialized = true;
    }

    // 로비 View에서 전달한 명령 키를 공통 실행 흐름으로 넘깁니다.
    private void OnCommandRequested(string commandKey)
    {
        Debug.Log($"[LobbyPresenter] 로비 명령 수신: {commandKey}");
        ExecuteCommandAsync(commandKey).Forget();
    }

    // 명령 실행 중에는 같은 프레임의 중복 입력을 막고, 실패 시 버튼을 다시 활성화합니다.
    private async UniTaskVoid ExecuteCommandAsync(string commandKey)
    {
        if (isExecutingCommand || isDisposed || string.IsNullOrWhiteSpace(commandKey))
            return;

        isExecutingCommand = true;
        view.SetCommandInteractable(commandKey, false);

        try
        {
            bool handled = await executeCommandAsync(commandKey);
            Debug.Log($"[LobbyPresenter] 로비 명령 처리 결과: {commandKey}, Handled: {handled}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"[LobbyPresenter] 로비 명령 처리 중 예외 발생: {commandKey}\n{exception}");
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
