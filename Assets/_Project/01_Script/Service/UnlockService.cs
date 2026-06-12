using System.Collections.Generic;

// Static 버튼, 영웅, Spire 컨텐츠의 해금 조건을 같은 규칙으로 판정합니다.
// UI는 이 서비스를 통해 결과만 받고, 층/밤/스파이어 레벨 같은 세부 조건은 직접 알지 않습니다.
public sealed class UnlockService
{
    private readonly IUnlockDataSource unlockDataSource; // 기능/Spire 해금 테이블 조회 계약
    private readonly GameContext context; // 현재 진행 상태

    // 해금 테이블과 현재 진행 상태를 받습니다.
    public UnlockService(IUnlockDataSource unlockDataSource, GameContext context)
    {
        this.unlockDataSource = unlockDataSource;
        this.context = context;
    }

    // Static UI 버튼이나 기능 키가 현재 사용 가능한지 판단합니다.
    public bool IsFeatureUnlocked(string featureKey)
    {
        if (string.IsNullOrWhiteSpace(featureKey))
            return false;

        if (unlockDataSource == null || !unlockDataSource.TryGetFeatureUnlock(featureKey, out FeatureUnlockDataRow row))
            return true;

        return IsUnlocked(row.IsDefaultUnlocked, row.UnlockConditionType, row.UnlockValue);
    }

    // HeroData 한 줄이 현재 사용 가능한지 판단합니다.
    public bool IsHeroUnlocked(HeroDataRow hero)
    {
        if (hero == null)
            return false;

        return IsUnlocked(hero.IsDefaultUnlocked, hero.UnlockConditionType, hero.UnlockValue);
    }

    // Spire 내부 컨텐츠 키가 현재 사용 가능한지 판단합니다.
    public bool IsSpireContentUnlocked(string contentKey)
    {
        if (string.IsNullOrWhiteSpace(contentKey))
            return false;

        if (unlockDataSource == null || !unlockDataSource.TryGetSpireContent(contentKey, out SpireContentDataRow row))
            return false;

        return IsSpireContentUnlocked(row);
    }

    // Spire 내부 컨텐츠 Row가 현재 사용 가능한지 판단합니다.
    public bool IsSpireContentUnlocked(SpireContentDataRow row)
    {
        if (row == null)
            return false;

        return IsUnlocked(row.IsDefaultUnlocked, row.UnlockConditionType, row.UnlockValue);
    }

    // 모든 Spire 컨텐츠를 현재 해금 상태와 함께 반환합니다.
    public IReadOnlyList<SpireContentUnlockState> BuildSpireContentStates()
    {
        IReadOnlyList<SpireContentDataRow> rows = unlockDataSource?.GetSpireContents() ?? System.Array.Empty<SpireContentDataRow>();
        List<SpireContentUnlockState> result = new(rows.Count);

        for (int i = 0; i < rows.Count; i++)
        {
            SpireContentDataRow row = rows[i];
            result.Add(new SpireContentUnlockState(row, IsSpireContentUnlocked(row)));
        }

        return result;
    }

    // 공통 해금 조건을 현재 진행 상태 기준으로 판정합니다.
    private bool IsUnlocked(bool isDefaultUnlocked, UnlockConditionType conditionType, int unlockValue)
    {
        if (isDefaultUnlocked || conditionType == UnlockConditionType.Default)
            return true;

        if (context == null)
            return false;

        switch (conditionType)
        {
            case UnlockConditionType.CitadelFloor:
                return context.DayProgress.CitadelFloorCount.Value >= unlockValue;

            case UnlockConditionType.ClearedNight:
                return context.GameProgress.HighestClearedDefenseSessionId.Value >= unlockValue;

            case UnlockConditionType.SpireLevel:
                return context.DayProgress.SpireLevel.Value >= unlockValue;

            case UnlockConditionType.MagicLibrary:
                return context.DayProgress.MagicLibraryUnlocked.Value;

            default:
                return false;
        }
    }
}

// Spire 컨텐츠 Row와 현재 해금 여부를 함께 전달하는 읽기 전용 값입니다.
public readonly struct SpireContentUnlockState
{
    public SpireContentDataRow Content { get; } // 원본 Spire 컨텐츠 테이블 Row
    public bool IsUnlocked { get; } // 현재 진행 기준 해금 여부

    // UI가 바로 표시할 수 있게 Row와 해금 상태를 묶습니다.
    public SpireContentUnlockState(SpireContentDataRow content, bool isUnlocked)
    {
        Content = content;
        IsUnlocked = isUnlocked;
    }
}
