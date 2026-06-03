using System;
using System.Collections.Generic;

// 실제 파일로 저장되는 영구 데이터입니다.
// 앱을 껐다 켜도 유지되어야 하는 값들이 여기에 들어갑니다.
[Serializable]
public sealed class SaveData
{
    public int Version = 1; // 저장 데이터 버전. 나중에 마이그레이션 기준으로 사용

    public CurrencySaveData Currency = new(); // 골드, 젬 같은 재화 저장 데이터
    public PlayerProgressSaveData Progress = new(); // 현재 스테이지, 클리어 진행도 저장 데이터
    public BattleSlotSaveDataContainer BattleSlot = new(); // 전투 슬롯 저장 데이터

    // 새 게임을 시작할 때 사용할 기본 저장 데이터를 생성합니다.
    public static SaveData CreateDefault()
    {
        SaveData saveData = new SaveData();

        saveData.Currency.Gold = 0;
        saveData.Currency.Gem = 0;

        saveData.Progress.CurrentStageId = 1;
        saveData.Progress.HighestClearedStageId = 0;

        // 기본 전투 슬롯 1개 생성
        saveData.BattleSlot.Slots.Add(new BattleSlotSaveData
        {
            SlotIndex = 0,
            Level = 1,
            EquippedHeroId = 1
        });

        return saveData;
    }
}

// 저장되는 재화 데이터입니다.
[Serializable]
public sealed class CurrencySaveData
{
    public long Gold; // 골드
    public long Gem; // 젬
}

// 저장되는 플레이어 진행 데이터입니다.
[Serializable]
public sealed class PlayerProgressSaveData
{
    public int CurrentStageId = 1; // 현재 진행 중인 스테이지
    public int HighestClearedStageId = 0; // 가장 높게 클리어한 스테이지
}

// 전투 슬롯 저장 데이터 묶음입니다.
[Serializable]
public sealed class BattleSlotSaveDataContainer
{
    public List<BattleSlotSaveData> Slots = new(); // 슬롯 목록
}

// 개별 전투 슬롯 저장 데이터입니다.
// 영웅 성장과 슬롯 성장을 분리하기 위해 슬롯 레벨과 장착 영웅을 따로 저장합니다.
[Serializable]
public sealed class BattleSlotSaveData
{
    public int SlotIndex; // 슬롯 번호
    public int Level; // 슬롯 레벨
    public int EquippedHeroId; // 장착된 영웅 ID
}