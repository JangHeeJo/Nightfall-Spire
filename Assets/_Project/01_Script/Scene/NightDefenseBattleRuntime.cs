using UnityEngine;

// BattleScene에서 밤 방어 순수 C# 런타임을 매 프레임 실행하는 Unity 연결 계층입니다.
public sealed class NightDefenseBattleRuntime : MonoBehaviour
{
    private GameContext context; // 현재 밤 방어전이 참조하는 게임 상태 묶음
    private NightDefenseRuntimeController runtimeController; // 순수 C# 밤 방어 런타임 컨트롤러
    private bool isInitialized; // Initialize가 끝났는지 여부
    private bool isTicking; // Update에서 런타임 Tick을 돌릴지 여부
    private bool hasReportedWaveSpawnComplete; // 같은 웨이브 완료 로그가 반복되지 않도록 막는 플래그

    // BattleSceneRoot가 GameContext 준비 이후 런타임을 연결합니다.
    public bool Initialize(GameContext gameContext, INightDefenseSpawnSink spawnSink)
    {
        if (gameContext == null || spawnSink == null)
        {
            Debug.LogError("[NightDefenseBattleRuntime] 초기화에 필요한 값이 비어 있습니다.");
            return false;
        }

        if (gameContext.NightDefenseSessionService == null)
        {
            Debug.LogError("[NightDefenseBattleRuntime] NightDefenseSessionService가 없어 웨이브를 시작할 수 없습니다.");
            return false;
        }

        context = gameContext;
        runtimeController = new NightDefenseRuntimeController(
            context.NightDefenseSessionService,
            context.NightDefenseProgress,
            spawnSink);

        isInitialized = true;
        return StartNextWave();
    }

    // 다음 웨이브 스폰 계획을 시작합니다.
    public bool StartNextWave()
    {
        if (!isInitialized || runtimeController == null)
            return false;

        NightDefenseRuntimeStartResult result = runtimeController.StartNextWave();

        if (!result.IsSuccess)
        {
            Debug.LogWarning($"[NightDefenseBattleRuntime] 웨이브 시작 실패: {result.FailureReason}");
            isTicking = false;
            return false;
        }

        hasReportedWaveSpawnComplete = false;
        isTicking = true;
        Debug.Log($"[NightDefenseBattleRuntime] Wave {result.WavePlan.WaveIndex} 시작, SpawnCount: {result.WavePlan.TotalSpawnCount}");
        return true;
    }

    // Unity 프레임 시간에 맞춰 밤 방어 런타임 Tick을 실행합니다.
    private void Update()
    {
        if (!isTicking || runtimeController == null)
            return;

        NightDefenseRuntimeTickResult result = runtimeController.Tick(Time.deltaTime);

        if (!result.IsWaveSpawnComplete || hasReportedWaveSpawnComplete)
            return;

        hasReportedWaveSpawnComplete = true;
        Debug.Log($"[NightDefenseBattleRuntime] Wave {context.NightDefenseProgress.CurrentWaveIndex.Value} 스폰 완료");

        if (result.ShouldOpenDraft)
            Debug.Log("[NightDefenseBattleRuntime] 드래프트 선택 진입 대기");
    }

    // 씬이 내려갈 때 런타임 Tick과 스폰 실행을 중단합니다.
    private void OnDestroy()
    {
        runtimeController?.Stop();
        isTicking = false;
        isInitialized = false;
    }
}
