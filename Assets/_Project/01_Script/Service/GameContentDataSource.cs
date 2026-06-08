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

// DataTableManager를 도메인 서비스가 쓰는 조회 계약으로 감싸는 어댑터입니다.
public sealed class GameContentDataSource : INightDefenseDataSource, IDraftDataSource, IRewardDataSource, IDayGrowthDataSource
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
}
