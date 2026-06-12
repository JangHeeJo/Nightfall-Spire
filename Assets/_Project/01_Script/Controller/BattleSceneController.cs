using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

// BattleScene의 진입, UI 조립, 밤 방어 런타임 시작과 종료를 제어하는 Controller입니다.
// BattleSceneRoot는 Unity 생명주기만 전달하고, 실제 세션 흐름 판단은 이 클래스가 담당합니다.
public sealed class BattleSceneController : IDisposable
{
    private readonly BattleStaticUIRoot staticUIRoot; // 전투 고정 UI 조립 지점
    private readonly BattleDynamicUIRoot dynamicUIRoot; // 전투 동적 UI 조립 지점
    private readonly NightDefenseBattleRuntime battleRuntime; // 밤 방어 런타임 Tick 실행기
    private readonly UnityNightDefenseSpawnSink spawnSink; // Unity 씬 스폰 요청 수신자
    private readonly Func<GameContext> contextProvider; // GameRoot가 준비한 현재 Context 조회 함수
    private readonly Func<GameFlowController> gameFlowControllerProvider; // 현재 게임 흐름 Controller 조회 함수

    private bool isDisposed; // 씬 파괴 이후 비동기 초기화를 막습니다.
    private bool hasStartedSession; // 세션 시작 후 파괴될 때 중단 정리가 필요한지 확인합니다.
    private GameContext activeContext; // 이 전투 씬이 시작한 세션을 정리할 때 사용할 Context

    // 전투 씬에서 필요한 Unity View/Adapter 참조를 받습니다.
    public BattleSceneController(
        BattleStaticUIRoot staticUIRoot,
        BattleDynamicUIRoot dynamicUIRoot,
        NightDefenseBattleRuntime battleRuntime,
        UnityNightDefenseSpawnSink spawnSink,
        Func<GameContext> contextProvider,
        Func<GameFlowController> gameFlowControllerProvider)
    {
        this.staticUIRoot = staticUIRoot;
        this.dynamicUIRoot = dynamicUIRoot;
        this.battleRuntime = battleRuntime;
        this.spawnSink = spawnSink;
        this.contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        this.gameFlowControllerProvider = gameFlowControllerProvider ?? throw new ArgumentNullException(nameof(gameFlowControllerProvider));
    }

    // GameRoot와 GameContext가 준비될 때까지 기다린 뒤 밤 방어 씬을 시작합니다.
    public async UniTask InitializeAsync()
    {
        GameContext context = await WaitForContextAsync();

        if (isDisposed || context == null)
            return;

        if (!ValidateRuntimeReferences())
            return;

        GameFlowController gameFlowController = gameFlowControllerProvider();
        if (gameFlowController == null)
        {
            Debug.LogError("[BattleSceneController] GameFlowController가 준비되지 않아 밤 방어 세션을 시작할 수 없습니다.");
            return;
        }

        if (!gameFlowController.BeginLoadedNightDefenseSession())
        {
            Debug.LogError("[BattleSceneController] 밤 방어 세션 시작에 실패했습니다.");
            return;
        }

        activeContext = context;
        hasStartedSession = true;
        InitializeSceneUi(context);
        battleRuntime.Initialize(context, spawnSink);
    }

    // 전투 씬에 배치된 UI Root에 현재 Context를 전달합니다.
    private void InitializeSceneUi(GameContext context)
    {
        staticUIRoot?.Initialize(context);
        dynamicUIRoot?.Initialize(context);
    }

    // Boot/DayPreparation에서 BattleScene으로 넘어오는 타이밍 차이를 흡수합니다.
    private async UniTask<GameContext> WaitForContextAsync()
    {
        await UniTask.WaitUntil(() => isDisposed
                                    || contextProvider() != null
                                    && gameFlowControllerProvider() != null);

        return isDisposed ? null : contextProvider();
    }

    // 전투 런타임 연결은 씬에서 명시적으로 세팅되어야 합니다.
    private bool ValidateRuntimeReferences()
    {
        if (battleRuntime == null)
        {
            Debug.LogError("[BattleSceneController] NightDefenseBattleRuntime 참조가 비어 있습니다. BattleSceneRoot에 런타임 컴포넌트를 직접 연결해야 합니다.");
            return false;
        }

        if (spawnSink == null)
        {
            Debug.LogError("[BattleSceneController] UnityNightDefenseSpawnSink 참조가 비어 있습니다. 전투 씬의 스폰 수신자를 직접 연결해야 합니다.");
            return false;
        }

        return true;
    }

    // 씬이 사라질 때 진행 중인 밤 방어 세션을 중단 상태로 정리합니다.
    public void Dispose()
    {
        if (isDisposed)
            return;

        isDisposed = true;

        if (!hasStartedSession || activeContext == null)
            return;

        if (activeContext.NightDefenseProgress.IsDefenseActive.Value)
            activeContext.NightDefenseProgress.EndDefenseSession(DefenseOutcome.Abandoned);
    }
}
