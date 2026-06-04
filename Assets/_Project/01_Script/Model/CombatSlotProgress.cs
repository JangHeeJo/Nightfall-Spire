using System.Collections.Generic;
using System.Linq;
using R3;

// 전투 슬롯 성장과 영웅 배치를 관리합니다.
// 영웅 자체 레벨보다 슬롯 성장을 우선하면 새 영웅도 즉시 전력화되는 구조를 만들 수 있습니다.
public sealed class CombatSlotProgress
{
    private readonly SaveData saveData; // 실제 저장 데이터 참조
    private readonly Dictionary<int, CombatSlotRuntimeState> slotsByIndex = new(); // 슬롯 번호별 런타임 상태

    public IReadOnlyDictionary<int, CombatSlotRuntimeState> SlotsByIndex => slotsByIndex; // 외부 조회용 슬롯 목록

    public CombatSlotProgress(SaveData saveData)
    {
        this.saveData = saveData;
        BuildRuntimeSlots();
    }

    // SaveData의 슬롯 목록을 R3 기반 런타임 상태로 변환합니다.
    private void BuildRuntimeSlots()
    {
        slotsByIndex.Clear();

        foreach (CombatSlotSaveData slotSaveData in saveData.CombatSlot.Slots.OrderBy(slot => slot.SlotIndex))
        {
            CombatSlotRuntimeState runtimeState = new CombatSlotRuntimeState(slotSaveData);
            slotsByIndex[slotSaveData.SlotIndex] = runtimeState;
        }
    }

    // 사용할 수 있는 슬롯 수를 반환합니다.
    public int GetUnlockedSlotCount()
    {
        return slotsByIndex.Values.Count(slot => slot.IsUnlocked.Value);
    }

    // 새 슬롯을 해금합니다.
    // 이미 존재하는 슬롯이면 잠금만 해제하고, 없으면 저장 데이터와 런타임 상태를 함께 만듭니다.
    public void UnlockSlot(int slotIndex)
    {
        if (slotIndex < 0)
            return;

        if (slotsByIndex.TryGetValue(slotIndex, out CombatSlotRuntimeState existingSlot))
        {
            existingSlot.IsUnlocked.Value = true;
            return;
        }

        CombatSlotSaveData slotSaveData = new CombatSlotSaveData
        {
            SlotIndex = slotIndex,
            Level = 1,
            EquippedHeroId = 0,
            IsUnlocked = true
        };

        saveData.CombatSlot.Slots.Add(slotSaveData);
        slotsByIndex[slotIndex] = new CombatSlotRuntimeState(slotSaveData);
    }

    // 슬롯 레벨을 올립니다.
    // 실제 비용과 해금 조건은 데이터 테이블 기반 서비스가 검증한 뒤 이 메서드를 호출합니다.
    public void UpgradeSlot(int slotIndex)
    {
        if (!slotsByIndex.TryGetValue(slotIndex, out CombatSlotRuntimeState slot))
            return;

        if (!slot.IsUnlocked.Value)
            return;

        slot.Level.Value += 1;
    }

    // 특정 슬롯에 영웅을 배치합니다.
    public void EquipHero(int slotIndex, int heroId)
    {
        if (heroId < 0)
            return;

        if (!slotsByIndex.TryGetValue(slotIndex, out CombatSlotRuntimeState slot))
            return;

        if (!slot.IsUnlocked.Value)
            return;

        slot.EquippedHeroId.Value = heroId;
    }
}

// 전투 슬롯 하나의 런타임 상태입니다.
// 값이 바뀌면 원본 SaveData에도 바로 반영됩니다.
public sealed class CombatSlotRuntimeState
{
    private readonly CombatSlotSaveData saveData; // 이 슬롯의 저장 데이터 참조

    public int SlotIndex => saveData.SlotIndex; // 슬롯 번호
    public ReactiveProperty<int> Level { get; } // 슬롯 성장 레벨
    public ReactiveProperty<int> EquippedHeroId { get; } // 배치된 영웅 ID
    public ReactiveProperty<bool> IsUnlocked { get; } // 슬롯 해금 여부

    public CombatSlotRuntimeState(CombatSlotSaveData saveData)
    {
        this.saveData = saveData;

        Level = new ReactiveProperty<int>(saveData.Level);
        EquippedHeroId = new ReactiveProperty<int>(saveData.EquippedHeroId);
        IsUnlocked = new ReactiveProperty<bool>(saveData.IsUnlocked);

        Level.Subscribe(value => this.saveData.Level = value);
        EquippedHeroId.Subscribe(value => this.saveData.EquippedHeroId = value);
        IsUnlocked.Subscribe(value => this.saveData.IsUnlocked = value);
    }
}
