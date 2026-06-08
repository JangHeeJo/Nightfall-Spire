using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 로비 화면 단위 Presenter입니다.
// 버튼 하나마다 Presenter를 만들지 않고 로비의 주요 화면 액션을 한 곳에서 GameFlowController로 연결합니다.
public sealed class LobbyPresenter : IDisposable
{
    private readonly ILobbyScreenView view; // 로비 화면 View
    private readonly Func<UniTask<bool>> loadNightDefenseAsync; // 밤 방어 씬 로드 요청

    private bool isInitialized; // 중복 초기화 방지
    private bool isDisposed; // Dispose 이후 비동기 콜백 방지
    private bool isLoadingNightDefense; // 밤 방어 로딩 중 중복 입력 방지

    // 로비 View와 밤 방어 씬 로드 함수를 주입받습니다.
    public LobbyPresenter(ILobbyScreenView view, Func<UniTask<bool>> loadNightDefenseAsync)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.loadNightDefenseAsync = loadNightDefenseAsync ?? throw new ArgumentNullException(nameof(loadNightDefenseAsync));
    }

    // 로비 화면 입력 구독을 시작합니다.
    public void Initialize()
    {
        if (isInitialized || isDisposed)
            return;

        if (!view.IsReady())
            Debug.LogWarning("[LobbyPresenter] LobbyScreen의 버튼 참조가 완전히 준비되지 않았습니다.");

        view.NightDefenseRequested += OnNightDefenseRequested;
        view.SetNightDefenseStartInteractable(true);
        isInitialized = true;
    }

    // 로비에서 밤 방어 시작 입력이 들어오면 씬 로드 흐름을 시작합니다.
    private void OnNightDefenseRequested()
    {
        LoadNightDefenseAsync().Forget();
    }

    // 중복 입력을 막고 GameFlowController에 밤 방어 씬 로드를 요청합니다.
    private async UniTaskVoid LoadNightDefenseAsync()
    {
        if (isLoadingNightDefense || isDisposed)
            return;

        isLoadingNightDefense = true;
        view.SetNightDefenseStartInteractable(false);

        bool loaded = await loadNightDefenseAsync();

        if (isDisposed)
            return;

        if (!loaded)
        {
            Debug.LogWarning("[LobbyPresenter] 밤 방어 씬 로드 요청이 거부되었습니다.");
            isLoadingNightDefense = false;
            view.SetNightDefenseStartInteractable(true);
        }
    }

    // 로비 화면 입력 구독을 해제합니다.
    public void Dispose()
    {
        if (isDisposed)
            return;

        view.NightDefenseRequested -= OnNightDefenseRequested;
        isDisposed = true;
    }
}
