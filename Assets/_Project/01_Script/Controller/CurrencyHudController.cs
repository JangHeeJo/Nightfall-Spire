using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

// 로비 상단 재화 HUD를 제어하는 Controller입니다.
// CurrencyProgress(Model)를 구독하고, View에는 화면에 필요한 문자열만 전달합니다.
public sealed class CurrencyHudController : IDisposable
{
    private readonly CurrencyProgress model; // 현재 재화 상태 모델
    private readonly ICurrencyHudView view; // 로비 상단 재화 View
    private readonly List<IDisposable> subscriptions = new(); // R3 구독 해제용 목록
    private bool isDisposed; // Controller 폐기 여부

    // 재화 HUD에 필요한 Model과 View를 받습니다.
    public CurrencyHudController(CurrencyProgress model, ICurrencyHudView view)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        this.view = view ?? throw new ArgumentNullException(nameof(view));
    }

    // 현재 재화 값을 즉시 표시하고 이후 변경을 구독합니다.
    public void Initialize()
    {
        if (isDisposed)
            throw new ObjectDisposedException(nameof(CurrencyHudController));

        if (subscriptions.Count > 0)
            return;

        if (!view.IsReady())
            Debug.LogWarning("[CurrencyHudController] CurrencyHudView의 텍스트 참조가 모두 연결되지 않았습니다.");

        RenderCurrentState();

        subscriptions.Add(model.Gold.Subscribe(_ => RenderCurrentState()));
        subscriptions.Add(model.Gem.Subscribe(_ => RenderCurrentState()));
    }

    // 현재 Model 값을 View가 바로 표시할 수 있는 상태로 바꿉니다.
    private void RenderCurrentState()
    {
        if (isDisposed)
            return;

        CurrencyHudViewState viewState = new CurrencyHudViewState(
            FormatAmount(model.Gold.Value),
            FormatAmount(model.Gem.Value));

        view.Render(viewState);
    }

    // 재화 숫자를 화면 표시용 문자열로 변환합니다.
    private static string FormatAmount(long amount)
    {
        return amount.ToString("N0");
    }

    // 씬이 내려갈 때 Model 구독을 정리합니다.
    public void Dispose()
    {
        if (isDisposed)
            return;

        for (int i = 0; i < subscriptions.Count; i++)
            subscriptions[i]?.Dispose();

        subscriptions.Clear();
        isDisposed = true;
    }
}
