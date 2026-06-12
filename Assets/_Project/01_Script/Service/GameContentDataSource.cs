using System.Collections.Generic;

// 밤 방어 세션 서비스가 필요한 테이블 조회 계약입니다.
public interface INightDefenseDataSource
{
    bool TryGetDefenseSession(int sessionId, out DefenseSessionDataRow row);
    bool TryGetWaveGroup(int waveGroupId, out WaveGroupDataRow row);
    IReadOnlyList<WaveDataRow> GetWaveRows(int waveGroupId, int waveIndex);
}

// 드래프트 서비스가 필요한 테이블 조회 계약입니다.
public interface IDraftDataSource
{
    bool TryGetDraftPool(int draftPoolId, out DraftPoolDataRow row);
    IReadOnlyList<DraftCardDataRow> GetDraftCards();
}

// 드래프트 카드 효과 적용기가 필요한 테이블 조회 계약입니다.
public interface IDraftEffectDataSource
{
    IReadOnlyList<DraftCardEffectDataRow> GetDraftCardEffects(int cardId);
}

// 보상 서비스가 필요한 테이블 조회 계약입니다.
public interface IRewardDataSource
{
    IReadOnlyList<RewardDataRow> GetRewardRows(int rewardGroupId);
}

// 낮 성장 서비스가 필요한 테이블 조회 계약입니다.
public interface IDayGrowthDataSource
{
    bool TryGetCitadelFloor(int floorId, out CitadelFloorDataRow row);
    bool TryGetCombatSlot(int slotId, out CombatSlotDataRow row);
    bool TryGetCombatSlotUpgrade(int upgradeGroupId, int level, out CombatSlotUpgradeDataRow row);
}

// 전투 런타임이 필요한 테이블 조회 계약입니다.
public interface ICombatDataSource
{
    bool TryGetHero(int heroId, out HeroDataRow row);
    bool TryGetEnemy(int enemyId, out EnemyDataRow row);
    bool TryGetCombatSlotByIndex(int slotIndex, out CombatSlotDataRow row);
    bool TryGetCombatSlotUpgrade(int upgradeGroupId, int level, out CombatSlotUpgradeDataRow row);
}

// 로비와 컬렉션 UI가 필요한 영웅 테이블 조회 계약입니다.
public interface IHeroCatalogDataSource
{
    IReadOnlyList<HeroDataRow> GetHeroes();
    bool TryGetHero(int heroId, out HeroDataRow row);
}

// 해금 서비스가 필요한 테이블 조회 계약입니다.
public interface IUnlockDataSource
{
    IReadOnlyList<FeatureUnlockDataRow> GetFeatureUnlocks();
    IReadOnlyList<SpireContentDataRow> GetSpireContents();
    bool TryGetFeatureUnlock(string featureKey, out FeatureUnlockDataRow row);
    bool TryGetSpireContent(string contentKey, out SpireContentDataRow row);
}

// DataTableManager를 도메인 서비스가 쓰는 조회 계약으로 감싸는 어댑터입니다.
public sealed class GameContentDataSource : INightDefenseDataSource, IDraftDataSource, IDraftEffectDataSource, IRewardDataSource, IDayGrowthDataSource, ICombatDataSource, IHeroCatalogDataSource, IUnlockDataSource
{
    private readonly DataTableManager dataTableManager; // 실제 테이블 보관소

    // 로드가 끝난 DataTableManager를 받아 서비스용 조회 계약을 제공합니다.
    public GameContentDataSource(DataTableManager dataTableManager)
    {
        this.dataTableManager = dataTableManager;
    }

    // 방어 세션 Row를 조회합니다.
    public bool TryGetDefenseSession(int sessionId, out DefenseSessionDataRow row)
    {
        row = null;
        return dataTableManager?.DefenseSessionData?.TryGet(sessionId, out row) == true;
    }

    // 웨이브 그룹 Row를 조회합니다.
    public bool TryGetWaveGroup(int waveGroupId, out WaveGroupDataRow row)
    {
        row = null;
        return dataTableManager?.WaveGroupData?.TryGet(waveGroupId, out row) == true;
    }

    // 웨이브 그룹과 웨이브 번호에 해당하는 스폰 Row를 조회합니다.
    public IReadOnlyList<WaveDataRow> GetWaveRows(int waveGroupId, int waveIndex)
    {
        return dataTableManager?.GetWaveRows(waveGroupId, waveIndex) ?? System.Array.Empty<WaveDataRow>();
    }

    // 드래프트 풀 Row를 조회합니다.
    public bool TryGetDraftPool(int draftPoolId, out DraftPoolDataRow row)
    {
        row = null;
        return dataTableManager?.DraftPoolData?.TryGet(draftPoolId, out row) == true;
    }

    // 모든 드래프트 카드 Row를 반환합니다.
    public IReadOnlyList<DraftCardDataRow> GetDraftCards()
    {
        return dataTableManager?.DraftCardData?.Rows ?? System.Array.Empty<DraftCardDataRow>();
    }

    // 선택된 카드 ID에 연결된 효과 Row 목록을 반환합니다.
    public IReadOnlyList<DraftCardEffectDataRow> GetDraftCardEffects(int cardId)
    {
        return dataTableManager?.GetDraftCardEffects(cardId) ?? System.Array.Empty<DraftCardEffectDataRow>();
    }

    // 보상 그룹 Row 목록을 조회합니다.
    public IReadOnlyList<RewardDataRow> GetRewardRows(int rewardGroupId)
    {
        return dataTableManager?.GetRewardRows(rewardGroupId) ?? System.Array.Empty<RewardDataRow>();
    }

    // 성채 층 Row를 조회합니다.
    public bool TryGetCitadelFloor(int floorId, out CitadelFloorDataRow row)
    {
        row = null;
        return dataTableManager?.CitadelFloorData?.TryGet(floorId, out row) == true;
    }

    // 전투 슬롯 Row를 조회합니다.
    public bool TryGetCombatSlot(int slotId, out CombatSlotDataRow row)
    {
        row = null;
        return dataTableManager?.CombatSlotData?.TryGet(slotId, out row) == true;
    }

    // 전투 슬롯 번호로 슬롯 Row를 조회합니다.
    public bool TryGetCombatSlotByIndex(int slotIndex, out CombatSlotDataRow row)
    {
        row = null;

        if (dataTableManager?.CombatSlotData?.Rows == null)
            return false;

        for (int i = 0; i < dataTableManager.CombatSlotData.Rows.Count; i++)
        {
            CombatSlotDataRow candidate = dataTableManager.CombatSlotData.Rows[i];

            if (candidate.SlotIndex != slotIndex)
                continue;

            row = candidate;
            return true;
        }

        return false;
    }

    // 전투 슬롯 성장 Row를 조회합니다.
    public bool TryGetCombatSlotUpgrade(int upgradeGroupId, int level, out CombatSlotUpgradeDataRow row)
    {
        row = null;

        try
        {
            row = dataTableManager?.GetCombatSlotUpgrade(upgradeGroupId, level);
            return row != null;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    // 영웅 Row를 조회합니다.
    public bool TryGetHero(int heroId, out HeroDataRow row)
    {
        row = null;
        return dataTableManager?.HeroData?.TryGet(heroId, out row) == true;
    }

    // 모든 영웅 Row를 테이블 순서 그대로 반환합니다.
    public IReadOnlyList<HeroDataRow> GetHeroes()
    {
        return dataTableManager?.HeroData?.Rows ?? System.Array.Empty<HeroDataRow>();
    }

    // 모든 기능 해금 Row를 반환합니다.
    public IReadOnlyList<FeatureUnlockDataRow> GetFeatureUnlocks()
    {
        return dataTableManager?.FeatureUnlockData?.Rows ?? System.Array.Empty<FeatureUnlockDataRow>();
    }

    // 모든 Spire 컨텐츠 Row를 반환합니다.
    public IReadOnlyList<SpireContentDataRow> GetSpireContents()
    {
        return dataTableManager?.SpireContentData?.Rows ?? System.Array.Empty<SpireContentDataRow>();
    }

    // 기능 키로 기능 해금 Row를 조회합니다.
    public bool TryGetFeatureUnlock(string featureKey, out FeatureUnlockDataRow row)
    {
        row = null;

        IReadOnlyList<FeatureUnlockDataRow> rows = GetFeatureUnlocks();
        for (int i = 0; i < rows.Count; i++)
        {
            FeatureUnlockDataRow candidate = rows[i];
            if (candidate.FeatureKey != featureKey)
                continue;

            row = candidate;
            return true;
        }

        return false;
    }

    // Spire 컨텐츠 키로 컨텐츠 Row를 조회합니다.
    public bool TryGetSpireContent(string contentKey, out SpireContentDataRow row)
    {
        row = null;

        IReadOnlyList<SpireContentDataRow> rows = GetSpireContents();
        for (int i = 0; i < rows.Count; i++)
        {
            SpireContentDataRow candidate = rows[i];
            if (candidate.ContentKey != contentKey)
                continue;

            row = candidate;
            return true;
        }

        return false;
    }

    // 적 Row를 조회합니다.
    public bool TryGetEnemy(int enemyId, out EnemyDataRow row)
    {
        row = null;
        return dataTableManager?.EnemyData?.TryGet(enemyId, out row) == true;
    }
}
