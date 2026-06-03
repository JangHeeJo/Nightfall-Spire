using R3;

// 현재 재화 상태를 관리하는 진행 모델입니다.
// UI는 이 모델을 구독해서 골드, 젬 표시를 자동으로 갱신합니다.
public sealed class CurrencyProgress
{
    private readonly SaveData saveData; // 실제 저장 데이터 참조

    public ReactiveProperty<long> Gold { get; } // 현재 골드
    public ReactiveProperty<long> Gem { get; } // 현재 젬

    public CurrencyProgress(SaveData saveData)
    {
        this.saveData = saveData;

        // SaveData 값을 기준으로 런타임 재화 상태를 생성합니다.
        Gold = new ReactiveProperty<long>(saveData.Currency.Gold);
        Gem = new ReactiveProperty<long>(saveData.Currency.Gem);

        // ReactiveProperty 값이 바뀌면 SaveData도 같이 갱신합니다.
        Gold.Subscribe(value => this.saveData.Currency.Gold = value);
        Gem.Subscribe(value => this.saveData.Currency.Gem = value);
    }

    // 골드를 추가합니다.
    public void AddGold(long amount)
    {
        if (amount <= 0)
            return;

        Gold.Value += amount;
    }

    // 골드 사용을 시도합니다. 부족하면 false를 반환합니다.
    public bool TrySpendGold(long amount)
    {
        if (amount <= 0)
            return true;

        if (Gold.Value < amount)
            return false;

        Gold.Value -= amount;
        return true;
    }

    // 젬을 추가합니다.
    public void AddGem(long amount)
    {
        if (amount <= 0)
            return;

        Gem.Value += amount;
    }

    // 젬 사용을 시도합니다. 부족하면 false를 반환합니다.
    public bool TrySpendGem(long amount)
    {
        if (amount <= 0)
            return true;

        if (Gem.Value < amount)
            return false;

        Gem.Value -= amount;
        return true;
    }
}