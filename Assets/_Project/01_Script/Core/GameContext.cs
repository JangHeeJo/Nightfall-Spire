// 게임 전체에서 공유하는 현재 데이터 묶음입니다.
// 낮 준비, 밤 방어전, UI, 저장 시스템은 이 Context를 통해 현재 진행 상태를 참조합니다.
public sealed class GameContext
{
    public SaveData SaveData { get; } // 실제 파일로 저장되는 원본 데이터

    public GameProgress GameProgress { get; } // 게임 전체 진행 상태
    public CurrencyProgress CurrencyProgress { get; } // 재화 진행 상태
    public DayProgress DayProgress { get; } // 낮 준비 단계 성장 상태
    public CombatSlotProgress CombatSlotProgress { get; } // 전투 슬롯 성장과 배치 상태
    public NightDefenseProgress NightDefenseProgress { get; } // 밤 방어 세션 런타임 상태
    public DraftProgress DraftProgress { get; } // 전투 중 로그라이트 카드 선택 상태
    public RewardProgress RewardProgress { get; } // 보상 런타임 상태
    public PopupProgress PopupProgress { get; } // 팝업 런타임 상태
    public NightDefenseSessionService NightDefenseSessionService { get; } // 밤 방어 세션 규칙 서비스
    public DraftService DraftService { get; } // 드래프트 후보 생성 서비스
    public RewardService RewardService { get; } // 보상 계산 서비스
    public DayGrowthService DayGrowthService { get; } // 낮 성장 규칙 서비스

    // 저장 데이터를 기반으로 게임 전체에서 공유할 런타임 모델 묶음을 만듭니다.
    public GameContext(SaveData saveData, GameContentDataSource contentDataSource = null)
    {
        SaveData = saveData;

        // SaveData를 기반으로 런타임 진행 모델들을 생성합니다.
        GameProgress = new GameProgress(saveData);
        CurrencyProgress = new CurrencyProgress(saveData);
        DayProgress = new DayProgress(saveData);
        CombatSlotProgress = new CombatSlotProgress(saveData);
        NightDefenseProgress = new NightDefenseProgress();
        DraftProgress = new DraftProgress();
        RewardProgress = new RewardProgress();
        PopupProgress = new PopupProgress();

        if (contentDataSource != null)
        {
            NightDefenseSessionService = new NightDefenseSessionService(contentDataSource, this);
            DraftService = new DraftService(contentDataSource, DraftProgress);
            RewardService = new RewardService(contentDataSource, RewardProgress);
            DayGrowthService = new DayGrowthService(contentDataSource, this);
        }
    }
}
