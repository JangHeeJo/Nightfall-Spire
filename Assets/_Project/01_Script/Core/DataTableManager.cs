using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 게임 밸런스 테이블을 로드하고 조회하는 매니저입니다.
// TSV 원본 파일과 런타임 로직 사이의 경계 역할을 맡습니다.
public sealed class DataTableManager
{
    private const string TableFolderRelativePath = "_Project/05_Data/Tables"; // Assets 폴더 안의 테이블 위치

    private readonly Dictionary<string, CombatSlotUpgradeDataRow> combatSlotUpgradesByGroupAndLevel = new(); // 슬롯 성장 복합 키 조회
    private readonly Dictionary<int, List<WaveDataRow>> waveRowsByGroupId = new(); // 웨이브 그룹별 Row 목록
    private readonly Dictionary<string, List<WaveDataRow>> waveRowsByGroupAndIndex = new(); // 웨이브 그룹/번호별 Row 목록
    private readonly Dictionary<int, List<RewardDataRow>> rewardRowsByGroupId = new(); // 보상 그룹별 Row 목록
    private readonly Dictionary<int, List<DraftCardEffectDataRow>> draftCardEffectsByCardId = new(); // 카드별 효과 목록

    public bool IsLoaded { get; private set; } // 테이블 로드 완료 여부

    public DataTable<CurrencyDataRow> CurrencyData { get; private set; } // 재화 테이블
    public DataTable<HeroDataRow> HeroData { get; private set; } // 영웅 테이블
    public DataTable<CombatSlotDataRow> CombatSlotData { get; private set; } // 전투 슬롯 테이블
    public DataTable<CombatSlotUpgradeDataRow> CombatSlotUpgradeData { get; private set; } // 슬롯 업그레이드 테이블
    public DataTable<DefenseSessionDataRow> DefenseSessionData { get; private set; } // 밤 방어 세션 테이블
    public DataTable<WaveGroupDataRow> WaveGroupData { get; private set; } // 웨이브 그룹 테이블
    public DataTable<WaveDataRow> WaveData { get; private set; } // 웨이브 상세 테이블
    public DataTable<EnemyDataRow> EnemyData { get; private set; } // 적 테이블
    public DataTable<DraftPoolDataRow> DraftPoolData { get; private set; } // 드래프트 풀 테이블
    public DataTable<DraftCardDataRow> DraftCardData { get; private set; } // 드래프트 카드 테이블
    public DataTable<DraftCardEffectDataRow> DraftCardEffectData { get; private set; } // 드래프트 카드 효과 테이블
    public DataTable<SynergyDataRow> SynergyData { get; private set; } // 시너지 테이블
    public DataTable<SkillDataRow> SkillData { get; private set; } // 스킬 테이블
    public DataTable<AttackPatternDataRow> AttackPatternData { get; private set; } // 공격 패턴 테이블
    public DataTable<ProjectileDataRow> ProjectileData { get; private set; } // 투사체 테이블
    public DataTable<RewardDataRow> RewardData { get; private set; } // 보상 테이블
    public DataTable<MaterialDataRow> MaterialData { get; private set; } // 재료 테이블
    public DataTable<BuildingModuleDataRow> BuildingModuleData { get; private set; } // 건물 모듈 테이블
    public DataTable<CitadelFloorDataRow> CitadelFloorData { get; private set; } // 성채 층 테이블
    public DataTable<SpireUpgradeDataRow> SpireUpgradeData { get; private set; } // 스파이어 업그레이드 테이블
    public DataTable<MiningNodeDataRow> MiningNodeData { get; private set; } // 채굴 노드 테이블
    public DataTable<CraftRecipeDataRow> CraftRecipeData { get; private set; } // 제작 레시피 테이블
    public DataTable<GearDataRow> GearData { get; private set; } // 장비 테이블

    // 모든 TSV 테이블을 읽고 런타임 조회용 인덱스를 구성합니다.
    public async UniTask LoadAllAsync()
    {
        string tableFolderPath = Path.Combine(Application.dataPath, TableFolderRelativePath);

        await UniTask.SwitchToThreadPool();

        CurrencyData = LoadTable<CurrencyDataRow>(tableFolderPath, "CurrencyData");
        HeroData = LoadTable<HeroDataRow>(tableFolderPath, "HeroData");
        CombatSlotData = LoadTable<CombatSlotDataRow>(tableFolderPath, "CombatSlotData");
        CombatSlotUpgradeData = LoadTable<CombatSlotUpgradeDataRow>(tableFolderPath, "CombatSlotUpgradeData");
        DefenseSessionData = LoadTable<DefenseSessionDataRow>(tableFolderPath, "DefenseSessionData");
        WaveGroupData = LoadTable<WaveGroupDataRow>(tableFolderPath, "WaveGroupData");
        WaveData = LoadTable<WaveDataRow>(tableFolderPath, "WaveData");
        EnemyData = LoadTable<EnemyDataRow>(tableFolderPath, "EnemyData");
        DraftPoolData = LoadTable<DraftPoolDataRow>(tableFolderPath, "DraftPoolData");
        DraftCardData = LoadTable<DraftCardDataRow>(tableFolderPath, "DraftCardData");
        DraftCardEffectData = LoadTable<DraftCardEffectDataRow>(tableFolderPath, "DraftCardEffectData");
        SynergyData = LoadTable<SynergyDataRow>(tableFolderPath, "SynergyData");
        SkillData = LoadTable<SkillDataRow>(tableFolderPath, "SkillData");
        AttackPatternData = LoadTable<AttackPatternDataRow>(tableFolderPath, "AttackPatternData");
        ProjectileData = LoadTable<ProjectileDataRow>(tableFolderPath, "ProjectileData");
        RewardData = LoadTable<RewardDataRow>(tableFolderPath, "RewardData");
        MaterialData = LoadTable<MaterialDataRow>(tableFolderPath, "MaterialData");
        BuildingModuleData = LoadTable<BuildingModuleDataRow>(tableFolderPath, "BuildingModuleData");
        CitadelFloorData = LoadTable<CitadelFloorDataRow>(tableFolderPath, "CitadelFloorData");
        SpireUpgradeData = LoadTable<SpireUpgradeDataRow>(tableFolderPath, "SpireUpgradeData");
        MiningNodeData = LoadTable<MiningNodeDataRow>(tableFolderPath, "MiningNodeData");
        CraftRecipeData = LoadTable<CraftRecipeDataRow>(tableFolderPath, "CraftRecipeData");
        GearData = LoadTable<GearDataRow>(tableFolderPath, "GearData");

        BuildLookupIndexes();

        await UniTask.SwitchToMainThread();

        IsLoaded = true;
        Debug.Log("[DataTableManager] 데이터 테이블 로드 완료");
    }

    // 슬롯 업그레이드 그룹과 레벨로 성장 Row를 조회합니다.
    public CombatSlotUpgradeDataRow GetCombatSlotUpgrade(int upgradeGroupId, int level)
    {
        string key = MakeCompositeKey(upgradeGroupId, level);

        if (!combatSlotUpgradesByGroupAndLevel.TryGetValue(key, out CombatSlotUpgradeDataRow row))
            throw new KeyNotFoundException($"CombatSlotUpgradeData를 찾을 수 없습니다. UpgradeGroupId: {upgradeGroupId}, Level: {level}");

        return row;
    }

    // 웨이브 그룹에 속한 모든 웨이브 Row를 조회합니다.
    public IReadOnlyList<WaveDataRow> GetWaveRows(int waveGroupId)
    {
        return waveRowsByGroupId.TryGetValue(waveGroupId, out List<WaveDataRow> rows) ? rows : System.Array.Empty<WaveDataRow>();
    }

    // 웨이브 그룹과 웨이브 번호에 해당하는 스폰 Row들을 조회합니다.
    public IReadOnlyList<WaveDataRow> GetWaveRows(int waveGroupId, int waveIndex)
    {
        string key = MakeCompositeKey(waveGroupId, waveIndex);
        return waveRowsByGroupAndIndex.TryGetValue(key, out List<WaveDataRow> rows) ? rows : System.Array.Empty<WaveDataRow>();
    }

    // 보상 그룹에 속한 보상 Row들을 조회합니다.
    public IReadOnlyList<RewardDataRow> GetRewardRows(int rewardGroupId)
    {
        return rewardRowsByGroupId.TryGetValue(rewardGroupId, out List<RewardDataRow> rows) ? rows : System.Array.Empty<RewardDataRow>();
    }

    // 카드 하나에 연결된 효과 Row들을 조회합니다.
    public IReadOnlyList<DraftCardEffectDataRow> GetDraftCardEffects(int cardId)
    {
        return draftCardEffectsByCardId.TryGetValue(cardId, out List<DraftCardEffectDataRow> rows) ? rows : System.Array.Empty<DraftCardEffectDataRow>();
    }

    // TSV 파일 하나를 읽어 지정한 Row 타입의 DataTable로 변환합니다.
    private static DataTable<TRow> LoadTable<TRow>(string tableFolderPath, string tableName) where TRow : ITableRow, new()
    {
        string path = Path.Combine(tableFolderPath, $"{tableName}.tsv");

        if (!File.Exists(path))
            throw new FileNotFoundException($"{tableName} 테이블 파일을 찾을 수 없습니다.", path);

        string text = File.ReadAllText(path);
        return TsvParser.Parse<TRow>(tableName, text);
    }

    // 자주 쓰는 복합 조회를 Dictionary로 미리 구성합니다.
    private void BuildLookupIndexes()
    {
        combatSlotUpgradesByGroupAndLevel.Clear();
        waveRowsByGroupId.Clear();
        waveRowsByGroupAndIndex.Clear();
        rewardRowsByGroupId.Clear();
        draftCardEffectsByCardId.Clear();

        for (int i = 0; i < CombatSlotUpgradeData.Rows.Count; i++)
        {
            CombatSlotUpgradeDataRow row = CombatSlotUpgradeData.Rows[i];
            combatSlotUpgradesByGroupAndLevel.Add(MakeCompositeKey(row.UpgradeGroupId, row.Level), row);
        }

        for (int i = 0; i < WaveData.Rows.Count; i++)
        {
            WaveDataRow row = WaveData.Rows[i];
            AddToList(waveRowsByGroupId, row.WaveGroupId, row);
            AddToList(waveRowsByGroupAndIndex, MakeCompositeKey(row.WaveGroupId, row.WaveIndex), row);
        }

        for (int i = 0; i < RewardData.Rows.Count; i++)
        {
            RewardDataRow row = RewardData.Rows[i];
            AddToList(rewardRowsByGroupId, row.RewardGroupId, row);
        }

        for (int i = 0; i < DraftCardEffectData.Rows.Count; i++)
        {
            DraftCardEffectDataRow row = DraftCardEffectData.Rows[i];
            AddToList(draftCardEffectsByCardId, row.CardId, row);
        }
    }

    // Dictionary 안의 List에 값을 추가하고, 첫 값이면 List를 새로 만듭니다.
    private static void AddToList<TKey, TValue>(Dictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
    {
        if (!dictionary.TryGetValue(key, out List<TValue> list))
        {
            list = new List<TValue>();
            dictionary.Add(key, list);
        }

        list.Add(value);
    }

    // 두 정수 값을 문자열 복합 키로 묶습니다.
    private static string MakeCompositeKey(int first, int second)
    {
        return $"{first}:{second}";
    }
}
