using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 로비의 밤 방어 시작 버튼 입력을 GameFlowController로 연결하는 Presenter입니다.
public sealed class FightStartPresenter : IDisposable
{
    private readonly IFightStartView view; // 전투 시작 입력 View
    private readonly Func<UniTask<bool>> loadNightDefenseAsync; // 밤 방어 씬 로드 요청

    private bool isInitialized; // 중복 초기화 방지
    private bool isDisposed; // Dispose 이후 비동기 콜백 방지
    private bool isLoading; // 중복 클릭 방지

    // View와 밤 방어 씬 로드 함수를 주입받습니다.
    public FightStartPresenter(IFightStartView view, Func<UniTask<bool>> loadNightDefenseAsync)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
        this.loadNightDefenseAsync = loadNightDefenseAsync ?? throw new ArgumentNullException(nameof(loadNightDefenseAsync));
    }

    // View 클릭 이벤트 구독을 시작합니다.
    public void Initialize()
    {
        if (isInitialized || isDisposed)
            return;

        view.Clicked += OnClicked;
        view.SetInteractable(true);
        isInitialized = true;
    }

    // 사용자가 전투 시작을 눌렀을 때 밤 방어 씬 로드를 요청합니다.
    private void OnClicked()
    {
        LoadNightDefenseAsync().Forget();
    }

    // 중복 입력을 막은 뒤 밤 방어 씬으로 이동합니다.
    private async UniTaskVoid LoadNightDefenseAsync()
    {
        if (isLoading || isDisposed)
            return;

        isLoading = true;
        view.SetInteractable(false);

        bool loaded = await loadNightDefenseAsync();

        if (isDisposed)
            return;

        if (!loaded)
        {
            Debug.LogWarning("[FightStartPresenter] 밤 방어 씬 로드 요청이 거부되었습니다.");
            isLoading = false;
            view.SetInteractable(true);
        }
    }

    // View 이벤트 구독을 해제합니다.
    public void Dispose()
    {
        if (isDisposed)
            return;

        view.Clicked -= OnClicked;
        isDisposed = true;
    }
}
