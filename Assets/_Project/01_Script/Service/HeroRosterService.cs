using System.Collections.Generic;

// HeroData 테이블과 HeroCollectionProgress를 합쳐 UI가 바로 그릴 수 있는 영웅 목록을 만듭니다.
// 팝업은 이 서비스를 통해 결과만 받고, 해금 조건이나 저장 구조는 직접 알지 않습니다.
public sealed class HeroRosterService
{
    private readonly IHeroCatalogDataSource heroCatalogDataSource; // 영웅 테이블 조회 계약
    private readonly GameContext context; // 현재 진행 상태와 영웅 보유 모델

    // 영웅 목록 생성에 필요한 테이블 조회와 현재 게임 상태를 받습니다.
    public HeroRosterService(IHeroCatalogDataSource heroCatalogDataSource, GameContext context)
    {
        this.heroCatalogDataSource = heroCatalogDataSource;
        this.context = context;
    }

    // 현재 저장 데이터 기준으로 해금/미해금이 반영된 영웅 목록을 만듭니다.
    public IReadOnlyList<HeroRosterEntry> BuildRoster()
    {
        IReadOnlyList<HeroDataRow> heroes = heroCatalogDataSource?.GetHeroes() ?? System.Array.Empty<HeroDataRow>();
        List<HeroRosterEntry> entries = new List<HeroRosterEntry>(heroes.Count);

        for (int i = 0; i < heroes.Count; i++)
        {
            HeroDataRow hero = heroes[i];
            HeroRuntimeState heroState = context.HeroCollectionProgress.EnsureHero(hero.HeroId, hero.StartLevel);
            int level = heroState?.Level.Value ?? GetStartLevel(hero);
            bool isUnlocked = IsUnlocked(hero, heroState);

            entries.Add(new HeroRosterEntry(hero, level, isUnlocked));
        }

        return entries;
    }

    // 수동 해금 또는 성채 층 조건으로 영웅 사용 가능 여부를 판단합니다.
    private bool IsUnlocked(HeroDataRow hero, HeroRuntimeState heroState)
    {
        if (heroState?.IsUnlocked.Value == true)
            return true;

        int floorCount = context.DayProgress.CitadelFloorCount.Value;
        return hero.UnlockFloorId <= floorCount;
    }

    // 테이블 시작 레벨이 잘못 들어와도 최소 1레벨로 보정합니다.
    private static int GetStartLevel(HeroDataRow hero)
    {
        return hero.StartLevel <= 0 ? 1 : hero.StartLevel;
    }
}

// 히어로 목록 UI가 카드 하나를 그릴 때 필요한 읽기 전용 데이터입니다.
public readonly struct HeroRosterEntry
{
    public HeroDataRow Hero { get; } // 원본 HeroData 테이블 Row
    public int Level { get; } // 저장 데이터가 반영된 현재 레벨
    public bool IsUnlocked { get; } // 현재 조건에서 사용 가능한지 여부

    // 히어로 카드에 필요한 값을 한 번에 보관합니다.
    public HeroRosterEntry(HeroDataRow hero, int level, bool isUnlocked)
    {
        Hero = hero;
        Level = level;
        IsUnlocked = isUnlocked;
    }
}
