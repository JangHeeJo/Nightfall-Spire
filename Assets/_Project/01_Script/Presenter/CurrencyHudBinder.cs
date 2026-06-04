using System;
using System.Collections.Generic;
using R3;

// CurrencyProgress(Model)와 CurrencyHud(View)를 연결하는 Binder입니다.
// Model 값 변경을 구독해서 View 표시 함수만 호출하고, View는 Model을 직접 알지 못하게 분리합니다.
public sealed class CurrencyHudBinder : IDisposable
{
    private readonly CurrencyProgress model; // 재화 상태 모델
    private readonly CurrencyHud view; // 재화 표시 View
    private readonly List<IDisposable> subscriptions = new(); // R3 구독 해제용 목록

    public CurrencyHudBinder(CurrencyProgress model, CurrencyHud view)
    {
        this.model = model;
        this.view = view;

        Bind();
    }

    // Model의 현재 값과 이후 변경 값을 View에 반영합니다.
    // UI 갱신 규칙을 View 안에 넣지 않고 Binder에 모아두기 위해 여기서 구독을 생성합니다.
    private void Bind()
    {
        view.SetGold(model.Gold.Value);
        view.SetGem(model.Gem.Value);

        subscriptions.Add(model.Gold.Subscribe(value => view.SetGold(value)));
        subscriptions.Add(model.Gem.Subscribe(value => view.SetGem(value)));
    }

    // Root나 View가 파괴될 때 Model 구독을 해제합니다.
    // 구독이 남아 있으면 파괴된 View를 계속 갱신하려 할 수 있으므로 반드시 호출해야 합니다.
    public void Dispose()
    {
        for (int i = 0; i < subscriptions.Count; i++)
            subscriptions[i]?.Dispose();

        subscriptions.Clear();
    }
}
