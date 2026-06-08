using System;

// 낮 준비 단계의 성채/전투 슬롯 성장 규칙을 담당합니다.
public sealed class DayGrowthService
{
    private readonly IDayGrowthDataSource dataSource; // 낮 성장 테이블 조회 계약
    private readonly GameContext context; // 현재 진행 모델 묶음

    // 낮 성장 규칙에 필요한 데이터와 진행 모델을 받습니다.
    public DayGrowthService(IDayGrowthDataSource dataSource, GameContext context)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    // 다음 성채 층 해금을 시도합니다.
    public DayGrowthResult TryUnlockNextCitadelFloor()
    {
        int nextFloorId = context.DayProgress.CitadelFloorCount.Value + 1;

        if (!dataSource.TryGetCitadelFloor(nextFloorId, out CitadelFloorDataRow floorRow))
            return DayGrowthResult.Fail(DayGrowthFailureReason.FloorNotFound);

        if (context.GameProgress.HighestClearedDefenseSessionId.Value < floorRow.RequiredSessionId)
            return DayGrowthResult.Fail(DayGrowthFailureReason.RequiredSessionNotCleared);

        if (!TrySpendCurrency(floorRow.UnlockCostCurrencyId, floorRow.UnlockCostAmount))
            return DayGrowthResult.Fail(DayGrowthFailureReason.NotEnoughCurrency);

        context.DayProgress.AddCitadelFloor();
        ApplyFloorUnlockFeature(floorRow);

        return DayGrowthResult.Success();
    }

    // 전투 슬롯 업그레이드를 시도합니다.
    public DayGrowthResult TryUpgradeCombatSlot(int slotIndex, int upgradeGroupId)
    {
        if (!context.CombatSlotProgress.SlotsByIndex.TryGetValue(slotIndex, out CombatSlotRuntimeState slot))
            return DayGrowthResult.Fail(DayGrowthFailureReason.SlotNotFound);

        if (!slot.IsUnlocked.Value)
            return DayGrowthResult.Fail(DayGrowthFailureReason.SlotLocked);

        int nextLevel = slot.Level.Value + 1;

        if (!dataSource.TryGetCombatSlotUpgrade(upgradeGroupId, nextLevel, out CombatSlotUpgradeDataRow upgradeRow))
            return DayGrowthResult.Fail(DayGrowthFailureReason.UpgradeNotFound);

        if (!TrySpendCurrency(upgradeRow.CostCurrencyId, upgradeRow.CostAmount))
            return DayGrowthResult.Fail(DayGrowthFailureReason.NotEnoughCurrency);

        context.CombatSlotProgress.UpgradeSlot(slotIndex);
        return DayGrowthResult.Success();
    }

    // 성채 층이 해금하는 기능을 현재 Progress에 반영합니다.
    private void ApplyFloorUnlockFeature(CitadelFloorDataRow floorRow)
    {
        if (floorRow.UnlockFeatureType == UnlockFeatureType.CombatSlot &&
            dataSource.TryGetCombatSlot(floorRow.UnlockFeatureId, out CombatSlotDataRow slotRow))
        {
            context.CombatSlotProgress.UnlockSlot(slotRow.SlotIndex);
        }

        if (floorRow.UnlockFeatureType == UnlockFeatureType.DraftPool)
            context.DayProgress.UnlockMagicLibrary();
    }

    // 현재 지원하는 재화 ID 기준으로 비용 지불을 시도합니다.
    private bool TrySpendCurrency(int currencyId, long amount)
    {
        if (amount <= 0)
            return true;

        if (currencyId == 1)
            return context.CurrencyProgress.TrySpendGold(amount);

        if (currencyId == 2)
            return context.CurrencyProgress.TrySpendGem(amount);

        return false;
    }
}
