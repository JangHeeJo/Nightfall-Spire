using System;
using System.Collections.Generic;

// 한 번의 밤 방어전을 실제 플레이 가능한 흐름으로 묶는 순수 런타임 컨트롤러입니다.
// 웨이브 스폰, 전투 계산, 성채 피해, 드래프트 대기, 승패 확정을 이 클래스가 한곳에서 조율합니다.
public sealed class BattleSessionRuntimeController
{
    private readonly GameContext context; // 현재 저장/진행/테이블 서비스 묶음
    private readonly GameFlowController gameFlowController; // 게임 전체 상태 전환 담당
    private readonly IBattleCombatViewSink viewSink; // Unity 표시 계층으로 결과를 전달하는 계약

    private CombatRuntimeController combatRuntime; // 영웅 슬롯 공격과 적 상태 계산기
    private NightDefenseRuntimeController waveRuntime; // 웨이브 시간표 실행기
    private CombatSpawnRouter spawnRouter; // 웨이브 스폰 요청을 전투 런타임으로 연결하는 라우터
    private bool isRunning; // Tick을 진행할 수 있는 상태인지 여부

    public BattleRuntimeState State => context?.BattleProgress.CurrentState.Value ?? BattleRuntimeState.Idle; // 현재 전투 상태
    public IReadOnlyList<CombatEnemyRuntimeState> Enemies => combatRuntime?.Enemies ?? Array.Empty<CombatEnemyRuntimeState>(); // 현재 살아 있는 적 목록

    // 전투 세션 진행에 필요한 Context, 흐름 컨트롤러, 표시 계층 Sink를 받습니다.
    public BattleSessionRuntimeController(GameContext context, GameFlowController gameFlowController, IBattleCombatViewSink viewSink)
    {
        this.context = context;
        this.gameFlowController = gameFlowController;
        this.viewSink = viewSink;
    }

    // 전투 계산기와 웨이브 실행기를 만들고 첫 웨이브를 시작합니다.
    public BattleRuntimeStartResult Start()
    {
        if (context == null)
            return BattleRuntimeStartResult.Fail(BattleRuntimeFailureReason.ContextMissing);

        if (gameFlowController == null)
            return BattleRuntimeStartResult.Fail(BattleRuntimeFailureReason.GameFlowControllerMissing);

        if (context.ContentDataSource == null)
            return BattleRuntimeStartResult.Fail(BattleRuntimeFailureReason.ContentDataSourceMissing);

        if (context.NightDefenseSessionService == null)
            return BattleRuntimeStartResult.Fail(BattleRuntimeFailureReason.SessionServiceMissing);

        combatRuntime = new CombatRuntimeController(
            context.ContentDataSource,
            context.CombatSlotProgress,
            modifierSet: context.CombatRuntimeModifierSet);

        CombatRuntimeFailureReason combatFailureReason = combatRuntime.RebuildHeroSlots();

        if (combatFailureReason != CombatRuntimeFailureReason.None)
            return BattleRuntimeStartResult.FailCombat(combatFailureReason);

        spawnRouter = new CombatSpawnRouter(combatRuntime, viewSink);
        waveRuntime = new NightDefenseRuntimeController(
            context.NightDefenseSessionService,
            context.NightDefenseProgress,
            spawnRouter);

        viewSink?.ClearBattleViews();
        context.BattleProgress.BeginBattle();

        NightDefenseRuntimeStartResult waveStartResult = waveRuntime.StartNextWave();

        if (!waveStartResult.IsSuccess)
        {
            context.BattleProgress.EndBattle(DefenseOutcome.Abandoned);
            return BattleRuntimeStartResult.FailWave(waveStartResult.FailureReason);
        }

        isRunning = true;
        return BattleRuntimeStartResult.Success();
    }

    // 전투 시간을 진행하고 이번 프레임의 웨이브/전투/승패 결과를 반환합니다.
    public BattleRuntimeTickResult Tick(float deltaSeconds)
    {
        if (!isRunning || deltaSeconds <= 0f)
            return CreateTickResult(0, 0, 0, 0, Enemies.Count);

        if (State == BattleRuntimeState.WaitingForDraft)
        {
            TryResumeAfterDraftSelection();
            return CreateTickResult(0, 0, 0, 0, Enemies.Count);
        }

        NightDefenseRuntimeTickResult waveTick = waveRuntime.Tick(deltaSeconds);
        CombatRuntimeTickResult combatTick = combatRuntime.Tick(deltaSeconds);

        PushCombatViews();

        if (combatTick.CastleDamage > 0)
            ApplyCastleDamage(combatTick.CastleDamage);

        if (State == BattleRuntimeState.Defeat)
            Complete(DefenseOutcome.Defeat);
        else if (waveTick.IsWaveSpawnComplete && combatTick.AliveEnemyCount == 0)
            HandleWaveCleared(waveTick);

        return CreateTickResult(
            waveTick.SpawnedCount,
            combatTick.AttackCount,
            combatTick.DefeatedEnemyCount,
            combatTick.CastleDamage,
            combatTick.AliveEnemyCount);
    }

    // 씬 이탈이나 강제 종료 때 세션을 중단 상태로 정리합니다.
    public void StopAsAbandoned()
    {
        if (!isRunning)
            return;

        isRunning = false;
        waveRuntime?.Stop();
        viewSink?.ClearBattleViews();
        context.BattleProgress.EndBattle(DefenseOutcome.Abandoned);
    }

    // 표시 계층으로 피해, 제거, 위치 갱신 결과를 전달합니다.
    private void PushCombatViews()
    {
        if (viewSink == null || combatRuntime == null)
            return;

        viewSink.ShowDamageResults(combatRuntime.LastDamageResults);

        for (int i = 0; i < combatRuntime.LastDespawnResults.Count; i++)
        {
            CombatEnemyDespawnResult result = combatRuntime.LastDespawnResults[i];
            viewSink.DespawnEnemyView(result.Enemy, result.Reason);
        }

        viewSink.SyncEnemyViews(combatRuntime.Enemies);
    }

    // 성채 피해를 BattleProgress에 반영합니다.
    private void ApplyCastleDamage(int damage)
    {
        context.BattleProgress.ApplyCastleDamage(damage);
    }

    // 웨이브의 모든 스폰과 남은 적 정리가 끝났을 때 다음 흐름을 결정합니다.
    private void HandleWaveCleared(NightDefenseRuntimeTickResult waveTick)
    {
        if (waveTick.IsLastWave)
        {
            Complete(DefenseOutcome.Victory);
            return;
        }

        if (waveTick.ShouldOpenDraft && TryOpenDraftSelection())
        {
            context.BattleProgress.ChangeState(BattleRuntimeState.WaitingForDraft);
            return;
        }

        StartNextWaveOrFail();
    }

    // 드래프트 UI가 닫히고 게임 상태가 다시 전투 진행으로 돌아오면 다음 웨이브를 시작합니다.
    private void TryResumeAfterDraftSelection()
    {
        if (context.DraftProgress.IsDraftOpen.Value)
            return;

        if (context.GameProgress.CurrentState.Value != GameState.NightDefensePlaying)
            return;

        context.BattleProgress.ChangeState(BattleRuntimeState.Playing);
        StartNextWaveOrFail();
    }

    // 현재 세션의 DraftPoolId로 카드 후보를 만들고 게임 흐름을 드래프트 선택 상태로 전환합니다.
    private bool TryOpenDraftSelection()
    {
        if (context.DraftService == null || context.ContentDataSource == null)
            return false;

        int sessionId = context.NightDefenseProgress.CurrentDefenseSessionId.Value;

        if (!context.ContentDataSource.TryGetDefenseSession(sessionId, out DefenseSessionDataRow sessionRow))
            return false;

        DraftOfferResult offerResult = context.DraftService.BuildOffer(sessionRow.DraftPoolId, context.DraftProgress.SelectedCardIds);

        if (!offerResult.IsSuccess)
            return false;

        return gameFlowController.OpenDraftSelection(offerResult.OfferedCardIds);
    }

    // 다음 웨이브 시작을 시도하고 실패하면 방어 실패로 종료합니다.
    private void StartNextWaveOrFail()
    {
        NightDefenseRuntimeStartResult startResult = waveRuntime.StartNextWave();

        if (startResult.IsSuccess)
            return;

        Complete(DefenseOutcome.Defeat);
    }

    // GameFlowController를 통해 밤 방어전 결과를 확정합니다.
    private void Complete(DefenseOutcome outcome)
    {
        if (!isRunning)
            return;

        isRunning = false;
        waveRuntime?.Stop();
        context.BattleProgress.EndBattle(outcome);
        gameFlowController.CompleteNightDefense(outcome);
    }

    // Tick 결과 값을 만듭니다.
    private BattleRuntimeTickResult CreateTickResult(int spawnedEnemyCount, int attackCount, int defeatedEnemyCount, int castleDamage, int aliveEnemyCount)
    {
        return new BattleRuntimeTickResult(State, spawnedEnemyCount, attackCount, defeatedEnemyCount, castleDamage, aliveEnemyCount);
    }

    // 웨이브 스폰 요청을 전투 계산 런타임으로 넣고, Unity 표시 계층에 생성 이벤트를 전달합니다.
    private sealed class CombatSpawnRouter : INightDefenseSpawnSink
    {
        private readonly CombatRuntimeController combatRuntime; // 적 런타임 상태 등록 대상
        private readonly IBattleCombatViewSink viewSink; // 표시 계층 생성 요청 대상

        // 전투 계산기와 표시 계층 Sink를 받습니다.
        public CombatSpawnRouter(CombatRuntimeController combatRuntime, IBattleCombatViewSink viewSink)
        {
            this.combatRuntime = combatRuntime ?? throw new ArgumentNullException(nameof(combatRuntime));
            this.viewSink = viewSink;
        }

        // 스폰 요청을 전투 런타임 상태로 등록하고 표시 계층에 생성 요청을 전달합니다.
        public void Spawn(NightDefenseSpawnRequest request)
        {
            CombatRuntimeFailureReason result = combatRuntime.SpawnEnemy(request, out CombatEnemyRuntimeState enemy);

            if (result != CombatRuntimeFailureReason.None || enemy == null)
                return;

            viewSink?.SpawnEnemyView(enemy, request);
        }
    }
}
