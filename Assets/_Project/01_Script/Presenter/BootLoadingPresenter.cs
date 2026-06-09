using System;

// BootScene 로딩 화면에 초기화 진행 상태를 전달하는 Presenter입니다.
// 실제 초기화 작업은 GameRoot가 진행하고, 이 클래스는 표시용 상태 갱신만 담당합니다.
public sealed class BootLoadingPresenter
{
    private readonly IBootLoadingView view; // 로딩 화면 View 계약

    // 표시할 View를 받습니다.
    public BootLoadingPresenter(IBootLoadingView view)
    {
        this.view = view ?? throw new ArgumentNullException(nameof(view));
    }

    // 로딩 화면 초기 상태를 표시합니다.
    public void Initialize(string versionText)
    {
        view.SetProgress(0f);
        view.SetStatusText("Starting...");
        view.SetVersionText(versionText);
    }

    // 초기화 단계 진행률과 문구를 갱신합니다.
    public void Report(float progress01, string statusText)
    {
        view.SetProgress(Clamp01(progress01));
        view.SetStatusText(statusText);
    }

    // 다음 씬으로 넘어가기 직전 완료 상태를 표시합니다.
    public void Complete()
    {
        view.SetProgress(1f);
        view.SetStatusText("Entering the spire...");
    }

    // 진행률 값이 UI 범위를 벗어나지 않게 제한합니다.
    private static float Clamp01(float value)
    {
        if (value < 0f)
            return 0f;

        if (value > 1f)
            return 1f;

        return value;
    }
}
