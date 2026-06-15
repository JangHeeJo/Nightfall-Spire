using System;
using System.Collections.Generic;

// 저장 파일로 남는 전체 데이터입니다.
// 런타임 모델은 이 데이터를 감싸서 UI와 시스템이 구독하기 쉬운 형태로 바꿉니다.
[Serializable]
public sealed class SaveData
{
    private const int CurrentVersion = 6; // 현재 코드가 기대하는 저장 데이터 버전
    private const int FirstDefenseSessionId = 101; // DefenseSessionData.tsv의 첫 테스트 세션
    private const int StarterHeroId = 1001; // 첫 전투 슬롯에 기본 배치할 영웅
    private const int StarterRangedHeroId = 1002; // 두 번째 전투 슬롯에 기본 배치할 원거리 영웅

    public int Version = CurrentVersion; // 저장 데이터 버전. 구조 변경이나 마이그레이션 판단에 사용합니다.

    public CurrencySaveData Currency = new(); // 골드, 젬 같은 공통 재화 저장 데이터
    public PlayerProgressSaveData Progress = new(); // 현재 방어 세션과 낮/밤 진행 저장 데이터
    public DayCycleSaveData DayCycle = new(); // 낮 준비 단계에서 성장시키는 스파이어/성채 저장 데이터
    public CombatSlotSaveDataContainer CombatSlot = new(); // 영웅 개별 성장보다 우선되는 전투 슬롯 저장 데이터
    public HeroCollectionSaveData HeroCollection = new(); // 영웅 해금과 개별 레벨 저장 데이터

    // 새 게임을 시작할 때 사용할 기본 저장 데이터를 만듭니다.
    public static SaveData CreateDefault()
    {
        SaveData saveData = new SaveData();

        saveData.Currency.Gold = 0;
        saveData.Currency.Gem = 0;

        saveData.Progress.CurrentDefenseSessionId = 101;
        saveData.Progress.HighestClearedDefenseSessionId = 0;
        saveData.Progress.CompletedDayCount = 0;

        saveData.DayCycle.SpireLevel = 1;
        saveData.DayCycle.CitadelFloorCount = 1;
        saveData.DayCycle.MiningDepth = 0;
        saveData.DayCycle.MagicLibraryUnlocked = false;

        // 기본 전투 슬롯 2개를 열어 첫 밤 방어전에서 근접과 원거리 영웅이 함께 공격하게 합니다.
        saveData.CombatSlot.Slots.Add(new CombatSlotSaveData
        {
            SlotIndex = 0,
            Level = 1,
            EquippedHeroId = 1001,
            IsUnlocked = true
        });
        saveData.CombatSlot.Slots.Add(new CombatSlotSaveData
        {
            SlotIndex = 1,
            Level = 1,
            EquippedHeroId = StarterRangedHeroId,
            IsUnlocked = true
        });

        saveData.HeroCollection.Heroes.Add(new HeroSaveData { HeroId = 1001, Level = 1, IsUnlocked = true });
        saveData.HeroCollection.Heroes.Add(new HeroSaveData { HeroId = 1002, Level = 1, IsUnlocked = true });
        saveData.HeroCollection.Heroes.Add(new HeroSaveData { HeroId = 1003, Level = 1, IsUnlocked = false });
        saveData.HeroCollection.Heroes.Add(new HeroSaveData { HeroId = 1004, Level = 1, IsUnlocked = false });

        return saveData;
    }

    // 오래된 저장 파일을 현재 전투/영웅 구조가 기대하는 최소 상태로 보정합니다.
    public bool RepairForCurrentVersion()
    {
        bool repaired = false;

        if (Version < CurrentVersion)
        {
            Version = CurrentVersion;
            repaired = true;
        }

        Currency ??= new CurrencySaveData();
        Progress ??= new PlayerProgressSaveData();
        DayCycle ??= new DayCycleSaveData();
        CombatSlot ??= new CombatSlotSaveDataContainer();
        HeroCollection ??= new HeroCollectionSaveData();
        CombatSlot.Slots ??= new List<CombatSlotSaveData>();
        HeroCollection.Heroes ??= new List<HeroSaveData>();

        if (Progress.CurrentDefenseSessionId <= 0)
        {
            Progress.CurrentDefenseSessionId = FirstDefenseSessionId;
            repaired = true;
        }

        if (DayCycle.SpireLevel <= 0)
        {
            DayCycle.SpireLevel = 1;
            repaired = true;
        }

        if (DayCycle.CitadelFloorCount <= 0)
        {
            DayCycle.CitadelFloorCount = 1;
            repaired = true;
        }

        repaired |= EnsureStarterCombatSlot(0, StarterHeroId);
        repaired |= EnsureStarterCombatSlot(1, StarterRangedHeroId);
        repaired |= EnsureStarterHero(1001, true);
        repaired |= EnsureStarterHero(1002, true);
        repaired |= EnsureStarterHero(1003, false);
        repaired |= EnsureStarterHero(1004, false);

        return repaired;
    }

    // 전투 런타임이 시작 슬롯을 항상 공격 가능한 상태로 만들 수 있게 보정합니다.
    private bool EnsureStarterCombatSlot(int slotIndex, int heroId)
    {
        for (int i = 0; i < CombatSlot.Slots.Count; i++)
        {
            CombatSlotSaveData slot = CombatSlot.Slots[i];
            if (slot.SlotIndex != slotIndex)
                continue;

            bool repaired = false;

            if (slot.Level <= 0)
            {
                slot.Level = 1;
                repaired = true;
            }

            if (slot.EquippedHeroId <= 0)
            {
                slot.EquippedHeroId = heroId;
                repaired = true;
            }

            if (!slot.IsUnlocked)
            {
                slot.IsUnlocked = true;
                repaired = true;
            }

            return repaired;
        }

        CombatSlot.Slots.Add(new CombatSlotSaveData
        {
            SlotIndex = slotIndex,
            Level = 1,
            EquippedHeroId = heroId,
            IsUnlocked = true
        });
        return true;
    }

    // 시작 영웅들이 저장 파일에 없으면 현재 테이블 기준 최소 상태를 추가합니다.
    private bool EnsureStarterHero(int heroId, bool isUnlocked)
    {
        for (int i = 0; i < HeroCollection.Heroes.Count; i++)
        {
            HeroSaveData hero = HeroCollection.Heroes[i];
            if (hero.HeroId != heroId)
                continue;

            bool repaired = false;

            if (hero.Level <= 0)
            {
                hero.Level = 1;
                repaired = true;
            }

            if (isUnlocked && !hero.IsUnlocked)
            {
                hero.IsUnlocked = true;
                repaired = true;
            }

            return repaired;
        }

        HeroCollection.Heroes.Add(new HeroSaveData
        {
            HeroId = heroId,
            Level = 1,
            IsUnlocked = isUnlocked
        });
        return true;
    }
}

// 저장되는 재화 데이터입니다.
[Serializable]
public sealed class CurrencySaveData
{
    public long Gold; // 낮 성장과 보상 지급에 쓰는 기본 재화
    public long Gem; // 프리미엄 또는 특수 해금 재화
}

// 저장되는 플레이어 진행 데이터입니다.
[Serializable]
public sealed class PlayerProgressSaveData
{
    public int CurrentDefenseSessionId = 101; // 다음에 도전할 밤 방어 세션 ID
    public int HighestClearedDefenseSessionId = 0; // 가장 멀리 클리어한 밤 방어 세션 ID
    public int CompletedDayCount = 0; // 낮/밤 루프를 몇 번 완료했는지 추적합니다.
}

// 낮 준비 단계에서 성장시키는 스파이어/성채 저장 데이터입니다.
[Serializable]
public sealed class DayCycleSaveData
{
    public int SpireLevel = 1; // 스파이어 전체 성장 레벨
    public int CitadelFloorCount = 1; // 건설된 성채 층 수
    public int MiningDepth = 0; // 채굴 진행 깊이 또는 구역 단계
    public bool MagicLibraryUnlocked; // 마법 도서관 기능 해금 여부
}

// 전투 슬롯 저장 데이터 묶음입니다.
[Serializable]
public sealed class CombatSlotSaveDataContainer
{
    public List<CombatSlotSaveData> Slots = new(); // 보유 중인 전투 슬롯 목록
}

// 전투 슬롯 하나의 저장 데이터입니다.
// 영웅이 아니라 슬롯을 성장시키면 새 영웅도 즉시 전력화될 수 있습니다.
[Serializable]
public sealed class CombatSlotSaveData
{
    public int SlotIndex; // 슬롯 번호
    public int Level; // 슬롯 성장 레벨
    public int EquippedHeroId; // 이 슬롯에 배치된 영웅 ID
    public bool IsUnlocked; // 슬롯 사용 가능 여부
}

// 영웅 저장 데이터 묶음입니다.
// 해금 여부와 개별 레벨은 전투 슬롯 성장과 별도로 관리합니다.
[Serializable]
public sealed class HeroCollectionSaveData
{
    public List<HeroSaveData> Heroes = new(); // 보유하거나 해금 후보로 등록된 영웅 목록
}

// 영웅 하나의 저장 데이터입니다.
// 레벨업과 수동 해금이 들어오면 이 값이 갱신됩니다.
[Serializable]
public sealed class HeroSaveData
{
    public int HeroId; // HeroData 테이블의 HeroId
    public int Level = 1; // 영웅 개별 레벨
    public bool IsUnlocked; // 플레이어가 실제로 사용할 수 있는지 여부
}
