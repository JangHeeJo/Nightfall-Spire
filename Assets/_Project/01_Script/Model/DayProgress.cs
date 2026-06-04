using R3;

// 낮 준비 단계에서 성장시키는 스파이어/성채/채굴 상태를 관리합니다.
// 이 게임의 성장 축은 밤 전투 전에 무엇을 강화했는지에 크게 좌우됩니다.
public sealed class DayProgress
{
    private readonly SaveData saveData; // 실제 저장 데이터 참조

    public ReactiveProperty<int> SpireLevel { get; } // 스파이어 전체 성장 레벨
    public ReactiveProperty<int> CitadelFloorCount { get; } // 성채에 건설된 층 수
    public ReactiveProperty<int> MiningDepth { get; } // 채굴 진행 단계
    public ReactiveProperty<bool> MagicLibraryUnlocked { get; } // 마법 도서관 해금 여부

    // 저장된 낮 성장 값을 런타임에서 구독 가능한 상태로 변환합니다.
    public DayProgress(SaveData saveData)
    {
        this.saveData = saveData;

        SpireLevel = new ReactiveProperty<int>(saveData.DayCycle.SpireLevel);
        CitadelFloorCount = new ReactiveProperty<int>(saveData.DayCycle.CitadelFloorCount);
        MiningDepth = new ReactiveProperty<int>(saveData.DayCycle.MiningDepth);
        MagicLibraryUnlocked = new ReactiveProperty<bool>(saveData.DayCycle.MagicLibraryUnlocked);

        // 낮 성장 값이 바뀌면 SaveData에도 즉시 반영합니다.
        SpireLevel.Subscribe(value => this.saveData.DayCycle.SpireLevel = value);
        CitadelFloorCount.Subscribe(value => this.saveData.DayCycle.CitadelFloorCount = value);
        MiningDepth.Subscribe(value => this.saveData.DayCycle.MiningDepth = value);
        MagicLibraryUnlocked.Subscribe(value => this.saveData.DayCycle.MagicLibraryUnlocked = value);
    }

    // 스파이어 레벨을 올립니다.
    // 비용 계산은 DataTableManager가 붙은 뒤 별도 서비스에서 처리하고, 모델은 결과만 반영합니다.
    public void UpgradeSpire()
    {
        SpireLevel.Value += 1;
    }

    // 새 성채 층을 추가합니다.
    // 층이 늘어나면 장기적으로 모듈, 슬롯, 시설 해금 조건에 사용됩니다.
    public void AddCitadelFloor()
    {
        CitadelFloorCount.Value += 1;
    }

    // 채굴 진행 단계를 갱신합니다.
    public void SetMiningDepth(int depth)
    {
        if (depth < 0)
            return;

        MiningDepth.Value = depth;
    }

    // 마법 도서관 기능을 해금합니다.
    public void UnlockMagicLibrary()
    {
        MagicLibraryUnlocked.Value = true;
    }
}
