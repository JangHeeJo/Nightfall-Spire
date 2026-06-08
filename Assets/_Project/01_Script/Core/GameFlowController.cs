using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// 낮 준비와 밤 방어전 사이의 큰 흐름을 제어하는 애플리케이션 계층 컨트롤러입니다.
// 전투 계산이나 보상 계산은 하지 않고, 이미 결정된 결과를 기준으로 상태 전환 순서만 보장합니다.
public sealed class GameFlowController
{
    private readonly GameContext context; // 현재 게임 진행 데이터 묶음
    private readonly SceneLoadManager sceneLoadManager; // 씬 전환 담당

    // 흐름 제어에 필요한 현재 게임 상태와 씬 전환 도구를 받습니다.
    public GameFlowController(GameContext context, SceneLoadManager sceneLoadManager)
    {
        this.context = context;
        this.sceneLoadManager = sceneLoadManager;
    }

    // 낮 준비 씬 로드가 끝난 뒤 낮 준비 상태를 확정합니다.
    // 진행 중인 밤 방어전이 있다면 중단으로 마무리하고, 이미 끝난 마지막 결과는 덮어쓰지 않습니다.
    public bool EnterDayPreparation()
    {
        if (context.GameProgress.CurrentState.Value == GameState.DayPreparation)
            return true;

        bool changed = context.GameProgress.ChangeState(GameState.DayPreparation);

        if (!changed)
            return false;

        if (context.NightDefenseProgress.IsDefenseActive.Value)
            context.NightDefenseProgress.EndDefenseSession(DefenseOutcome.Abandoned);

        context.DraftProgress.ResetForNewDefenseSession();
        return true;
    }

    // 현재 선택된 밤 방어 세션 씬으로 이동합니다.
    // 실제 세션 시작은 BattleSceneRoot가 로드된 뒤 BeginLoadedNightDefenseSession에서 처리합니다.
    public async UniTask<bool> LoadNightDefenseAsync()
    {
        bool changed = context.GameProgress.ChangeState(GameState.NightDefenseLoading);

        if (!changed)
            return false;

        await sceneLoadManager.LoadBattleSceneAsync();
        return true;
    }

    // 밤 방어전 씬 로드가 끝났을 때 세션을 시작합니다.
    // 씬 로드와 런타임 세션 시작을 분리해 UI Root가 준비되는 타이밍 문제를 줄입니다.
    public bool BeginLoadedNightDefenseSession()
    {
        bool ready = context.GameProgress.ChangeState(GameState.NightDefenseReady);

        if (!ready)
            return false;

        bool sessionStarted = TryStartNightDefenseSession();

        if (!sessionStarted)
        {
            context.GameProgress.ChangeState(GameState.DayPreparationLoading);
            return false;
        }

        bool playing = context.GameProgress.ChangeState(GameState.NightDefensePlaying);

        if (playing)
            return true;

        context.NightDefenseProgress.EndDefenseSession(DefenseOutcome.Abandoned);
        context.DraftProgress.ResetForNewDefenseSession();
        return false;
    }

    // 전투 중 로그라이트 카드 선택지를 엽니다.
    // 후보 카드 추첨은 DraftPool 데이터 기반 시스템이 맡고, 이 메서드는 선택 상태 진입만 보장합니다.
    public bool OpenDraftSelection(IReadOnlyList<int> offeredCardIds)
    {
        if (!context.NightDefenseProgress.IsDefenseActive.Value)
            return false;

        context.DraftProgress.OpenDraft(offeredCardIds);

        if (context.DraftProgress.IsDraftOpen.Value)
            return context.GameProgress.ChangeState(GameState.DraftSelection);

        return false;
    }

    // 드래프트 카드 선택을 확정하고 다시 밤 방어 진행 상태로 돌아갑니다.
    // 실제 카드 효과 적용은 이후 DraftEffectResolver가 붙으면 선택 성공 뒤 처리합니다.
    public bool SelectDraftCard(int cardId)
    {
        bool selected = context.DraftProgress.SelectCard(cardId);

        if (!selected)
            return false;

        if (context.NightDefenseProgress.IsDefenseActive.Value)
            return context.GameProgress.ChangeState(GameState.NightDefensePlaying);

        return true;
    }

    // 밤 방어전 결과를 확정합니다.
    // 보상 금액은 전투/보상 계산 시스템이 결정하고, 이 메서드는 결과 반영 순서만 담당합니다.
    public bool CompleteNightDefense(DefenseOutcome outcome, long rewardGold, long rewardGem)
    {
        if (!context.NightDefenseProgress.IsDefenseActive.Value)
            return false;

        int completedSessionId = context.NightDefenseProgress.CurrentDefenseSessionId.Value;

        bool changed = context.GameProgress.ChangeState(GameState.NightDefenseResult);

        if (!changed)
            return false;

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

        return true;
    }

    // 결과 확인 후 낮 준비 화면으로 돌아갑니다.
    // 결과 팝업이 닫히고 보상 처리가 끝난 뒤 호출되는 흐름을 기준으로 합니다.
    public async UniTask<bool> ReturnToDayPreparationAsync()
    {
        context.RewardProgress.ClearPendingReward();
        bool changed = context.GameProgress.ChangeState(GameState.DayPreparationLoading);

        if (!changed)
            return false;

        await sceneLoadManager.LoadLobbySceneAsync();
        return EnterDayPreparation();
    }

    // 테이블 서비스가 있으면 세션 입장 조건을 검증하고, 없으면 테스트와 초기 부트스트랩용 최소 시작만 수행합니다.
    private bool TryStartNightDefenseSession()
    {
        if (context.NightDefenseSessionService != null)
        {
            NightDefenseStartResult startResult = context.NightDefenseSessionService.TryStartCurrentSession();
            return startResult.IsSuccess;
        }

        int defenseSessionId = context.GameProgress.CurrentDefenseSessionId.Value;

        context.DraftProgress.ResetForNewDefenseSession();
        context.NightDefenseProgress.BeginDefenseSession(defenseSessionId);
        return context.NightDefenseProgress.IsDefenseActive.Value;
    }
}
