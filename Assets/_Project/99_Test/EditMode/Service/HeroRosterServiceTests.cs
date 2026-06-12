using System.Collections.Generic;
using NUnit.Framework;

// HeroRosterService가 영웅 테이블과 저장 데이터를 합쳐 UI용 목록을 올바르게 만드는지 검증합니다.
public sealed class HeroRosterServiceTests
{
    // 기본 저장 데이터에서는 시작 영웅 2명이 사용 가능해야 합니다.
    [Test]
    public void BuildRoster_UsesFloorUnlockRules()
    {
        FakeHeroCatalogDataSource dataSource = FakeHeroCatalogDataSource.CreateDefault();
        GameContext context = new GameContext(SaveData.CreateDefault());
        HeroRosterService service = new HeroRosterService(dataSource, context);

        IReadOnlyList<HeroRosterEntry> roster = service.BuildRoster();

        Assert.That(roster.Count, Is.EqualTo(4));
        Assert.That(roster[0].Hero.HeroId, Is.EqualTo(1001));
        Assert.That(roster[0].IsUnlocked, Is.True);
        Assert.That(roster[1].Hero.HeroId, Is.EqualTo(1002));
        Assert.That(roster[1].IsUnlocked, Is.True);
        Assert.That(roster[2].IsUnlocked, Is.False);
    }

    // 저장 데이터에서 직접 해금한 영웅은 성채 층이 낮아도 사용 가능해야 합니다.
    [Test]
    public void BuildRoster_RespectsManualUnlock()
    {
        FakeHeroCatalogDataSource dataSource = FakeHeroCatalogDataSource.CreateDefault();
        SaveData saveData = SaveData.CreateDefault();
        saveData.HeroCollection.Heroes[2].IsUnlocked = true;
        GameContext context = new GameContext(saveData);
        HeroRosterService service = new HeroRosterService(dataSource, context);

        IReadOnlyList<HeroRosterEntry> roster = service.BuildRoster();

        Assert.That(roster[2].Hero.HeroId, Is.EqualTo(1003));
        Assert.That(roster[2].IsUnlocked, Is.True);
    }

    // 저장 데이터에 없는 신규 영웅도 로스터 생성 중 기본 상태로 등록되어야 합니다.
    [Test]
    public void BuildRoster_AddsMissingHeroSaveData()
    {
        FakeHeroCatalogDataSource dataSource = FakeHeroCatalogDataSource.CreateDefault();
        SaveData saveData = SaveData.CreateDefault();
        saveData.HeroCollection.Heroes.RemoveAll(hero => hero.HeroId == 1004);
        GameContext context = new GameContext(saveData);
        HeroRosterService service = new HeroRosterService(dataSource, context);

        service.BuildRoster();

        Assert.That(context.HeroCollectionProgress.TryGetHero(1004, out HeroRuntimeState heroState), Is.True);
        Assert.That(heroState.Level.Value, Is.EqualTo(1));
        Assert.That(heroState.IsUnlocked.Value, Is.False);
    }

    // 테스트용 영웅 테이블 데이터 소스입니다.
    private sealed class FakeHeroCatalogDataSource : IHeroCatalogDataSource
    {
        private DataTable<HeroDataRow> heroes;

        // HeroRosterService 테스트에 필요한 최소 HeroData 테이블을 구성합니다.
        public static FakeHeroCatalogDataSource CreateDefault()
        {
            return new FakeHeroCatalogDataSource
            {
                heroes = TsvParser.Parse<HeroDataRow>("HeroData", HeroTsv)
            };
        }

        // 모든 영웅 Row를 반환합니다.
        public IReadOnlyList<HeroDataRow> GetHeroes()
        {
            return heroes.Rows;
        }

        // HeroId로 영웅 Row를 조회합니다.
        public bool TryGetHero(int heroId, out HeroDataRow row)
        {
            return heroes.TryGet(heroId, out row);
        }

        private const string HeroTsv =
            "HeroId\tCharacterName\tHeroRole\tElementType\tRarity\tHeroTagList\tBaseHealth\tBaseDefense\tBaseAttack\tBaseAttackSpeed\tBaseAttackRange\tAttackPatternId\tSkillId\tTargetingType\tUnlockFloorId\tPrefabKey\tIconKey\tStartLevel\n" +
            "1001\tPlayer_Sword\tMelee\tNone\tCommon\tMelee|Starter\t120\t8\t12\t1.0\t1.2\t3001\t4001\tNearest\t1\tPlayer_Sword\tPlayer_Sword\t1\n" +
            "1002\tPlayer_Archer\tRanged\tAir\tRare\tRanged|Projectile\t80\t3\t9\t1.2\t5.5\t3002\t4002\tNearest\t1\tPlayer_Archer\tPlayer_Archer\t1\n" +
            "1003\tPlayer_Magician\tRanged\tIce\tEpic\tMagic|Control\t70\t2\t7\t0.9\t4.8\t3003\t4003\tHighestHp\t5\tPlayer_Magician\tPlayer_Magician\t1\n" +
            "1004\tPlayer_Axe\tMelee\tFire\tLegendary\tMelee|Guardian\t160\t12\t18\t0.7\t1.4\t3004\t4004\tNearest\t7\tPlayer_Axe\tPlayer_Axe\t1\n";
    }
}
