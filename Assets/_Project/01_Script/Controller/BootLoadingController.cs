using System;

// BootScene 로딩 화면을 제어하는 Controller입니다.
// GameRoot가 진행 상태를 계산하고, Controller는 그 값을 View에 맞는 표시 상태로 전달합니다.
public sealed class BootLoadingController
{
    private readonly IBootLoadingView view; // BootScene에 배치된 로딩 View

    // 로딩 표시를 갱신할 View를 받습니다.
    public BootLoadingController(IBootLoadingView view)
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
