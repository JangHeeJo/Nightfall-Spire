using System.Collections.Generic;
using System.Linq;
using R3;

// 영웅 해금과 개별 레벨을 런타임에서 관리하는 모델입니다.
// SaveData는 저장 포맷만 담당하고, UI와 서비스는 이 모델을 통해 영웅 상태를 조회하거나 변경합니다.
public sealed class HeroCollectionProgress
{
    private readonly SaveData saveData; // 실제 저장 데이터 참조
    private readonly Dictionary<int, HeroRuntimeState> heroesById = new(); // HeroId별 런타임 상태

    public IReadOnlyDictionary<int, HeroRuntimeState> HeroesById => heroesById; // 외부 조회용 영웅 상태 목록

    // 저장된 영웅 목록을 런타임 상태로 변환합니다.
    public HeroCollectionProgress(SaveData saveData)
    {
        this.saveData = saveData;
        EnsureSaveContainer();
        BuildRuntimeHeroes();
    }

    // 오래된 저장 파일에서 HeroCollection이 비어 있어도 새 구조로 보정합니다.
    private void EnsureSaveContainer()
    {
        if (saveData == null)
            return;

        saveData.HeroCollection ??= new HeroCollectionSaveData();
        saveData.HeroCollection.Heroes ??= new List<HeroSaveData>();
    }

    // SaveData의 영웅 목록을 빠르게 조회할 수 있는 Dictionary로 구성합니다.
    private void BuildRuntimeHeroes()
    {
        heroesById.Clear();

        if (saveData?.HeroCollection?.Heroes == null)
            return;

        foreach (HeroSaveData heroSaveData in saveData.HeroCollection.Heroes.OrderBy(hero => hero.HeroId))
            heroesById[heroSaveData.HeroId] = new HeroRuntimeState(heroSaveData);
    }

    // 해당 영웅 상태를 가져오고, 저장 데이터에 없으면 기본 상태를 만들어 등록합니다.
    public HeroRuntimeState EnsureHero(int heroId, int startLevel = 1)
    {
        if (heroId <= 0)
            return null;

        if (heroesById.TryGetValue(heroId, out HeroRuntimeState existingState))
            return existingState;

        EnsureSaveContainer();

        if (saveData?.HeroCollection?.Heroes == null)
            return null;

        HeroSaveData heroSaveData = new HeroSaveData
        {
            HeroId = heroId,
            Level = startLevel <= 0 ? 1 : startLevel,
            IsUnlocked = false
        };

        saveData.HeroCollection.Heroes.Add(heroSaveData);
        HeroRuntimeState runtimeState = new HeroRuntimeState(heroSaveData);
        heroesById[heroId] = runtimeState;
        return runtimeState;
    }

    // 저장 데이터에 있는 영웅 상태를 조회합니다.
    public bool TryGetHero(int heroId, out HeroRuntimeState heroState)
    {
        return heroesById.TryGetValue(heroId, out heroState);
    }

    // 영웅 레벨을 반환하고, 저장 데이터가 없으면 테이블 시작 레벨을 사용합니다.
    public int GetLevel(int heroId, int startLevel)
    {
        if (heroesById.TryGetValue(heroId, out HeroRuntimeState heroState))
            return heroState.Level.Value;

        return startLevel <= 0 ? 1 : startLevel;
    }

    // 영웅이 수동으로 해금되어 있는지 확인합니다.
    public bool IsManuallyUnlocked(int heroId)
    {
        return heroesById.TryGetValue(heroId, out HeroRuntimeState heroState) && heroState.IsUnlocked.Value;
    }

    // 영웅을 수동 해금 상태로 변경합니다.
    public void UnlockHero(int heroId, int startLevel = 1)
    {
        HeroRuntimeState heroState = EnsureHero(heroId, startLevel);
        if (heroState == null)
            return;

        heroState.IsUnlocked.Value = true;
    }

    // 영웅 레벨을 지정한 값으로 갱신합니다.
    public void SetLevel(int heroId, int level, int startLevel = 1)
    {
        HeroRuntimeState heroState = EnsureHero(heroId, startLevel);
        if (heroState == null)
            return;

        heroState.Level.Value = level <= 0 ? 1 : level;
    }
}

// 영웅 하나의 런타임 상태입니다.
// ReactiveProperty 값이 바뀌면 원본 SaveData에도 즉시 반영됩니다.
public sealed class HeroRuntimeState
{
    private readonly HeroSaveData saveData; // 이 영웅의 저장 데이터 참조

    public int HeroId => saveData.HeroId; // HeroData 테이블의 HeroId
    public ReactiveProperty<int> Level { get; } // 영웅 개별 레벨
    public ReactiveProperty<bool> IsUnlocked { get; } // 수동 해금 여부

    // 저장 영웅 하나를 구독 가능한 런타임 상태로 감쌉니다.
    public HeroRuntimeState(HeroSaveData saveData)
    {
        this.saveData = saveData;

        Level = new ReactiveProperty<int>(saveData.Level <= 0 ? 1 : saveData.Level);
        IsUnlocked = new ReactiveProperty<bool>(saveData.IsUnlocked);

        Level.Subscribe(value => this.saveData.Level = value <= 0 ? 1 : value);
        IsUnlocked.Subscribe(value => this.saveData.IsUnlocked = value);
    }
}
