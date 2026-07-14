using System;
using System.Collections.Generic;

// 한 번의 밤 방어전을 실제 플레이 가능한 흐름으로 묶는 순수 런타임 컨트롤러입니다.
// 웨이브 스폰, 전투 계산, 성채 피해, 드래프트 대기, 승패 확정을 이 클래스가 한곳에서 조율합니다.
public sealed class BattleSessionRuntimeController
{
    private const int TestLevelUpKillThreshold = 3; // 레벨업 시스템 완성 전 테스트용으로 3킬마다 드래프트를 엽니다.
    private const float TestLevelUpDraftDelaySeconds = 0.45f; // 처치 모션이 보인 뒤 테스트 드래프트가 뜨도록 기다리는 시간입니다.

    private readonly GameContext context; // 현재 저장/진행/테이블 서비스 묶음
    private readonly GameFlowController gameFlowController; // 게임 전체 상태 전환 담당
    private readonly IBattleCombatViewSink viewSink; // Unity 표시 계층으로 결과를 전달하는 계약
    private readonly bool enableDraftSelections; // 전투 테스트용으로 카드 드래프트를 잠시 끌 수 있는 옵션

    private CombatRuntimeController combatRuntime; // 영웅 슬롯 공격과 적 상태 계산기
    private NightDefenseRuntimeController waveRuntime; // 웨이브 시간표 실행기
    private CombatSpawnRouter spawnRouter; // 웨이브 스폰 요청을 전투 런타임으로 연결하는 라우터
    private int levelUpKillCounter; // 다음 테스트 레벨업 드래프트까지 누적된 처치 수
    private float pendingLevelUpDraftSeconds; // 처치 모션을 보여준 뒤 드래프트를 열기까지 남은 시간입니다.
    private bool hasPendingLevelUpDraft; // 테스트 레벨업 드래프트가 예약되어 있는지 여부입니다.
    private bool shouldStartNextWaveAfterDraft; // 드래프트 종료 후 새 웨이브를 시작해야 하는 흐름인지 여부
    private bool isRunning; // Tick을 진행할 수 있는 상태인지 여부

    public BattleRuntimeState State => context?.BattleProgress.CurrentState.Value ?? BattleRuntimeState.Idle; // 현재 전투 상태
    public IReadOnlyList<CombatEnemyRuntimeState> Enemies => combatRuntime?.Enemies ?? Array.Empty<CombatEnemyRuntimeState>(); // 현재 살아 있는 적 목록

    // 전투 세션 진행에 필요한 Context, 흐름 컨트롤러, 표시 계층 Sink를 받습니다.
    public BattleSessionRuntimeController(GameContext context, GameFlowController gameFlowController, IBattleCombatViewSink viewSink, bool enableDraftSelections = true)
    {
        this.context = context;
        this.gameFlowController = gameFlowController;
        this.viewSink = viewSink;
        this.enableDraftSelections = enableDraftSelections;
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
        viewSink?.ShowHeroSlots(combatRuntime.HeroSlots);
        context.BattleProgress.BeginBattle();

        isRunning = true;

        // 드래프트가 켜져 있으면 시작 영웅 모집 2회를 먼저 열고, 꺼져 있으면 바로 전투 루프를 확인합니다.
        if (enableDraftSelections && TryOpenOpeningHeroRecruitSelection())
            return BattleRuntimeStartResult.Success();

        NightDefenseRuntimeStartResult waveStartResult = waveRuntime.StartNextWave();

        if (!waveStartResult.IsSuccess)
        {
            context.BattleProgress.EndBattle(DefenseOutcome.Abandoned);
            return BattleRuntimeStartResult.FailWave(waveStartResult.FailureReason);
        }

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

        if (TryOpenPendingLevelUpDraft(deltaSeconds))
            return CreateTickResult(0, 0, 0, 0, Enemies.Count);

        NightDefenseRuntimeTickResult waveTick = waveRuntime.Tick(deltaSeconds);
        CombatRuntimeTickResult combatTick = combatRuntime.Tick(deltaSeconds);

        PushCombatViews();

        if (combatTick.CastleDamage > 0)
            ApplyCastleDamage(combatTick.CastleDamage);

        if (State == BattleRuntimeState.Defeat)
        {
            Complete(DefenseOutcome.Defeat);
        }
        else if (TryScheduleLevelUpDraftByKillCount(combatTick.DefeatedEnemyCount))
        {
            return CreateTickResult(
                waveTick.SpawnedCount,
                combatTick.AttackCount,
                combatTick.DefeatedEnemyCount,
                combatTick.CastleDamage,
                combatTick.AliveEnemyCount);
        }
        else if (waveTick.IsWaveSpawnComplete && combatTick.AliveEnemyCount == 0 && !HasPendingEnemyRemoval())
        {
            HandleWaveCleared(waveTick);
        }

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
        ClearPendingLevelUpDraft();
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

        // 현재는 레벨업 시스템이 없어서 웨이브 클리어를 레벨업 드래프트 트리거로 사용합니다.
        if (enableDraftSelections && TryOpenDraftSelection(startNextWaveAfterDraft: true))
            return;

        StartNextWaveOrFail();
    }

    // 드래프트 UI가 닫히고 게임 상태가 다시 전투 진행으로 돌아오면 다음 웨이브를 시작합니다.
    private void TryResumeAfterDraftSelection()
    {
        if (context.DraftProgress.IsDraftOpen.Value)
            return;

        if (context.GameProgress.CurrentState.Value != GameState.NightDefensePlaying)
            return;

        // 시작 모집 2회는 전투 시작 보장분이고, 이후 추가 영웅 모집은 레벨업 드래프트가 맡습니다.
        if (enableDraftSelections
            && context.DraftProgress.OpeningHeroRecruitCount.Value < DraftService.RequiredOpeningHeroRecruitCount
            && TryOpenOpeningHeroRecruitSelection())
        {
            return;
        }

        RefreshHeroSlotsAfterDraftSelection();
        viewSink?.SetBattleViewPaused(false);
        context.BattleProgress.ChangeState(BattleRuntimeState.Playing);

        if (!shouldStartNextWaveAfterDraft)
            return;

        shouldStartNextWaveAfterDraft = false;
        StartNextWaveOrFail();
    }

    // 예약된 테스트 레벨업 드래프트를 죽는 모션 표시 시간 이후에 실제로 엽니다.
    private bool TryOpenPendingLevelUpDraft(float deltaSeconds)
    {
        if (!hasPendingLevelUpDraft)
            return false;

        if (HasPendingEnemyRemoval())
            return false;

        pendingLevelUpDraftSeconds -= deltaSeconds;

        if (pendingLevelUpDraftSeconds > 0f)
            return false;

        ClearPendingLevelUpDraft();
        return TryOpenDraftSelection(startNextWaveAfterDraft: false);
    }

    // 테스트용 레벨업 규칙입니다. 전투 중 몬스터 3마리를 잡으면 죽는 모션 뒤에 드래프트를 열도록 예약합니다.
    private bool TryScheduleLevelUpDraftByKillCount(int defeatedEnemyCount)
    {
        if (!enableDraftSelections || defeatedEnemyCount <= 0 || hasPendingLevelUpDraft)
            return false;

        levelUpKillCounter += defeatedEnemyCount;

        if (levelUpKillCounter < TestLevelUpKillThreshold)
            return false;

        levelUpKillCounter = 0;
        hasPendingLevelUpDraft = true;
        pendingLevelUpDraftSeconds = TestLevelUpDraftDelaySeconds;
        return true;
    }

    // 다른 드래프트나 전투 종료가 끼어들 때 예약된 테스트 드래프트를 정리합니다.
    private void ClearPendingLevelUpDraft()
    {
        hasPendingLevelUpDraft = false;
        pendingLevelUpDraftSeconds = 0f;
    }

    // 화면에서 죽는 애니메이션이 끝나기 전에는 드래프트나 다음 웨이브로 넘어가지 않습니다.
    private bool HasPendingEnemyRemoval()
    {
        return viewSink?.HasPendingEnemyRemoval == true;
    }

    // 드래프트 선택으로 모집/강화 상태가 바뀐 뒤 전투 슬롯 수치를 즉시 다시 계산합니다.
    private void RefreshHeroSlotsAfterDraftSelection()
    {
        if (combatRuntime == null)
            return;

        CombatRuntimeFailureReason rebuildResult = combatRuntime.RebuildHeroSlots();

        if (rebuildResult != CombatRuntimeFailureReason.None)
            return;

        // 새로 모집한 영웅과 방금 적용된 데미지/공속 보정이 표시 계층에도 바로 반영되게 합니다.
        viewSink?.ShowHeroSlots(combatRuntime.HeroSlots);
    }

    // 전투 시작 시 영웅 모집 드래프트를 열고 전투 Tick을 멈춥니다.
    private bool TryOpenOpeningHeroRecruitSelection()
    {
        if (context.DraftService == null)
            return false;

        if (context.DraftProgress.OpeningHeroRecruitCount.Value >= DraftService.RequiredOpeningHeroRecruitCount)
            return false;

        // 시작 드래프트는 항상 HeroRecruit 카드만 후보로 받습니다.
        DraftOfferResult offerResult = context.DraftService.TryOpenOpeningHeroRecruitOffer();

        if (!offerResult.IsSuccess)
            return false;

        ClearPendingLevelUpDraft();
        shouldStartNextWaveAfterDraft = true;
        viewSink?.SetBattleViewPaused(true);
        context.BattleProgress.ChangeState(BattleRuntimeState.WaitingForDraft);
        return gameFlowController.OpenDraftSelection(offerResult.OfferedCardIds);
    }

    // 현재 세션의 DraftPoolId로 카드 후보를 만들고 게임 흐름을 드래프트 선택 상태로 전환합니다.
    private bool TryOpenDraftSelection(bool startNextWaveAfterDraft)
    {
        if (context.DraftService == null || context.ContentDataSource == null)
            return false;

        int sessionId = context.NightDefenseProgress.CurrentDefenseSessionId.Value;

        if (!context.ContentDataSource.TryGetDefenseSession(sessionId, out DefenseSessionDataRow sessionRow))
            return false;

        // 레벨업 드래프트는 세션의 DraftPoolId와 카드 타입 규칙을 함께 사용합니다.
        DraftOfferResult offerResult = context.DraftService.TryOpenLevelUpOffer(sessionRow.DraftPoolId);

        if (!offerResult.IsSuccess)
            return false;

        ClearPendingLevelUpDraft();
        shouldStartNextWaveAfterDraft = startNextWaveAfterDraft;
        viewSink?.SetBattleViewPaused(true);
        context.BattleProgress.ChangeState(BattleRuntimeState.WaitingForDraft);
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
        ClearPendingLevelUpDraft();
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
