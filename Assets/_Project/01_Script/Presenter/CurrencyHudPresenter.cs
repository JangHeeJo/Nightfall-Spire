using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

// CurrencyProgress(Model)와 ICurrencyHudView(View)를 연결하는 MVP Presenter입니다.
// Model 값 구독, 표시 문자열 생성, View 갱신 순서를 모두 Presenter가 책임집니다.
public sealed class CurrencyHudPresenter : IDisposable
{
    private readonly CurrencyProgress model; // 재화 상태 모델
    private readonly ICurrencyHudView view; // 재화 HUD View
    private readonly List<IDisposable> subscriptions = new(); // R3 구독 해제용 목록
    private bool isDisposed; // Presenter 폐기 여부

    // Presenter가 다룰 Model과 View를 받습니다.
    // 실제 구독 시작은 Initialize에서 명시적으로 처리합니다.
    public CurrencyHudPresenter(CurrencyProgress model, ICurrencyHudView view)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        this.view = view ?? throw new ArgumentNullException(nameof(view));
    }

    // View 준비 상태를 확인하고, 현재 Model 값을 렌더링한 뒤 이후 변경을 구독합니다.
    public void Initialize()
    {
        if (isDisposed)
            throw new ObjectDisposedException(nameof(CurrencyHudPresenter));

        if (subscriptions.Count > 0)
            return;

        if (!view.IsReady())
            Debug.LogWarning("[CurrencyHudPresenter] CurrencyHudView의 텍스트 참조가 모두 연결되지 않았습니다.");

        RenderCurrentState();

        subscriptions.Add(model.Gold.Subscribe(_ => RenderCurrentState()));
        subscriptions.Add(model.Gem.Subscribe(_ => RenderCurrentState()));
    }

    // 현재 Model 값을 ViewState로 변환해 View에 전달합니다.
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
    // 현재는 천 단위 구분만 적용하고, K/M/B 축약 표기는 밸런스 기준이 잡힌 뒤 추가합니다.
    private string FormatAmount(long amount)
    {
        return amount.ToString("N0");
    }

    // Root나 View가 파괴될 때 Model 구독을 해제합니다.
    // Presenter가 구독 생명주기를 소유하므로 View에는 R3 의존성이 들어가지 않습니다.
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
