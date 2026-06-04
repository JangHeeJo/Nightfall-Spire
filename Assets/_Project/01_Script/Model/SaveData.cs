using System;
using System.Collections.Generic;

// 저장 파일로 남는 전체 데이터입니다.
// 런타임 모델은 이 데이터를 감싸서 UI와 시스템이 구독하기 쉬운 형태로 바꿉니다.
[Serializable]
public sealed class SaveData
{
    public int Version = 2; // 저장 데이터 버전. 구조 변경이나 마이그레이션 판단에 사용합니다.

    public CurrencySaveData Currency = new(); // 골드, 젬 같은 공통 재화 저장 데이터
    public PlayerProgressSaveData Progress = new(); // 현재 방어 세션과 낮/밤 진행 저장 데이터
    public DayCycleSaveData DayCycle = new(); // 낮 준비 단계에서 성장시키는 스파이어/성채 저장 데이터
    public CombatSlotSaveDataContainer CombatSlot = new(); // 영웅 개별 성장보다 우선되는 전투 슬롯 저장 데이터

    // 새 게임을 시작할 때 사용할 기본 저장 데이터를 만듭니다.
    public static SaveData CreateDefault()
    {
        SaveData saveData = new SaveData();

        saveData.Currency.Gold = 0;
        saveData.Currency.Gem = 0;

        saveData.Progress.CurrentDefenseSessionId = 1;
        saveData.Progress.HighestClearedDefenseSessionId = 0;
        saveData.Progress.CompletedDayCount = 0;

        saveData.DayCycle.SpireLevel = 1;
        saveData.DayCycle.CitadelFloorCount = 1;
        saveData.DayCycle.MiningDepth = 0;
        saveData.DayCycle.MagicLibraryUnlocked = false;

        // 기본 전투 슬롯 1개를 열어 첫 밤 방어전을 시작할 수 있게 합니다.
        saveData.CombatSlot.Slots.Add(new CombatSlotSaveData
        {
            SlotIndex = 0,
            Level = 1,
            EquippedHeroId = 1,
            IsUnlocked = true
        });

        return saveData;
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
    public int CurrentDefenseSessionId = 1; // 다음에 도전할 밤 방어 세션 ID
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
