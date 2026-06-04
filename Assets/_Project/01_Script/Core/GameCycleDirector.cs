using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// 낮 준비와 밤 방어전 사이의 큰 흐름을 지휘하는 클래스입니다.
// UI 버튼, 전투 컨트롤러, 보상 팝업이 Progress를 제각각 만지지 않도록 상태 변경의 진입점을 여기에 모읍니다.
public sealed class GameCycleDirector
{
    private readonly GameContext context; // 현재 게임 진행 데이터 묶음
    private readonly SceneLoadManager sceneLoadManager; // 씬 전환 담당

    public GameCycleDirector(GameContext context, SceneLoadManager sceneLoadManager)
    {
        this.context = context;
        this.sceneLoadManager = sceneLoadManager;
    }

    // 낮 준비 상태로 진입합니다.
    // 낮 화면에서는 스파이어, 성채, 전투 슬롯, 채굴, 제작 같은 영구 성장을 처리합니다.
    public void EnterDayPreparation()
    {
        context.NightDefenseProgress.EndDefenseSession(DefenseOutcome.None);
        context.DraftProgress.ResetForNewDefenseSession();
        context.GameProgress.ChangeState(GameState.DayPreparation);
    }

    // 현재 선택된 밤 방어 세션으로 이동합니다.
    // 실제 세션 시작은 씬 로드 후 BeginLoadedNightDefenseSession에서 처리합니다.
    public async UniTask LoadNightDefenseAsync()
    {
        context.GameProgress.ChangeState(GameState.NightDefenseLoading);
        await sceneLoadManager.LoadBattleSceneAsync();
    }

    // 밤 방어전 씬 로드가 끝났을 때 세션을 시작합니다.
    public void BeginLoadedNightDefenseSession()
    {
        int defenseSessionId = context.GameProgress.CurrentDefenseSessionId.Value;

        context.DraftProgress.ResetForNewDefenseSession();
        context.NightDefenseProgress.BeginDefenseSession(defenseSessionId);
        context.GameProgress.ChangeState(GameState.NightDefensePlaying);
    }

    // 전투 중 로그라이트 카드 선택지를 엽니다.
    // 후보 카드 추첨 자체는 DraftPool 데이터가 붙은 뒤 별도 시스템에서 처리하고, 여기서는 상태 전환만 보장합니다.
    public void OpenDraftSelection(IReadOnlyList<int> offeredCardIds)
    {
        if (!context.NightDefenseProgress.IsDefenseActive.Value)
            return;

        context.DraftProgress.OpenDraft(offeredCardIds);

        if (context.DraftProgress.IsDraftOpen.Value)
            context.GameProgress.ChangeState(GameState.DraftSelection);
    }

    // 드래프트 카드 선택을 확정하고 다시 밤 방어 진행 상태로 돌아갑니다.
    public bool SelectDraftCard(int cardId)
    {
        bool selected = context.DraftProgress.SelectCard(cardId);

        if (!selected)
            return false;

        if (context.NightDefenseProgress.IsDefenseActive.Value)
            context.GameProgress.ChangeState(GameState.NightDefensePlaying);

        return true;
    }

    // 밤 방어전 결과를 확정합니다.
    // 성공 시 다음 방어 세션과 낮/밤 루프 진행도를 갱신하고, 보상은 수령 대기 상태로 둡니다.
    public void CompleteNightDefense(DefenseOutcome outcome, long rewardGold, long rewardGem)
    {
        int completedSessionId = context.NightDefenseProgress.CurrentDefenseSessionId.Value;

        context.NightDefenseProgress.EndDefenseSession(outcome);
        context.DraftProgress.CloseDraft();

        if (outcome == DefenseOutcome.Victory)
        {
            context.GameProgress.CompleteDefenseSession(completedSessionId);
            context.GameProgress.CompleteDayCycle();
            context.RewardProgress.SetPendingReward(rewardGold, rewardGem);
        }
        else
        {
            context.RewardProgress.SetPendingReward(0, 0);
        }

        context.GameProgress.ChangeState(GameState.NightDefenseResult);
    }

    // 결과 확인 후 낮 준비 화면으로 돌아갑니다.
    public async UniTask ReturnToDayPreparationAsync()
    {
        context.RewardProgress.ClearPendingReward();
        context.GameProgress.ChangeState(GameState.DayPreparationLoading);
        await sceneLoadManager.LoadLobbySceneAsync();
        EnterDayPreparation();
    }
}
