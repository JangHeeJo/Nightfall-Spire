using Cysharp.Threading.Tasks;
using UnityEngine;

// BattleScene의 진입점입니다.
// 씬 이름은 기존 연결을 유지하지만, 실제 런타임 모델은 GameFlowController가 시작하는 밤 방어 세션입니다.
public sealed class BattleSceneRoot : MonoBehaviour
{
    [SerializeField] private BattleStaticUIRoot staticUIRoot; // 밤 방어전 고정 UI 묶음
    [SerializeField] private BattleDynamicUIRoot dynamicUIRoot; // 밤 방어전 동적 UI 묶음
    [SerializeField] private NightDefenseBattleRuntime battleRuntime; // 밤 방어전 런타임 Tick 실행기
    [SerializeField] private UnityNightDefenseSpawnSink spawnSink; // Unity 씬 스폰 요청 수신자

    // 씬 오브젝트가 준비되면 GameRoot 초기화를 기다린 뒤 밤 방어 세션을 시작합니다.
    private void Start()
    {
        InitializeAsync().Forget();
    }

    // 씬 진입 후 GameContext를 기다리고 밤 방어 세션과 UI Root를 초기화합니다.
    private async UniTask InitializeAsync()
    {
        GameContext context = await WaitForContextAsync();

        bool sessionStarted = GameRoot.Instance.GameFlowController.BeginLoadedNightDefenseSession();

        if (!sessionStarted)
        {
            Debug.LogError("[BattleSceneRoot] 밤 방어 세션 시작에 실패했습니다.");
            return;
        }

        staticUIRoot?.Initialize(context);
        dynamicUIRoot?.Initialize(context);

        EnsureRuntimeComponents();
        battleRuntime.Initialize(context, spawnSink);
    }

    // 씬이 사라질 때 진행 중인 밤 방어 세션을 중단 상태로 정리합니다.
    private void OnDestroy()
    {
        GameContext context = GameRoot.Instance?.Context;

        if (context == null)
            return;

        if (context.NightDefenseProgress.IsDefenseActive.Value)
            context.NightDefenseProgress.EndDefenseSession(DefenseOutcome.Abandoned);
    }

    // Boot/DayPreparation에서 BattleScene으로 넘어오는 타이밍 차이를 흡수합니다.
    private async UniTask<GameContext> WaitForContextAsync()
    {
        await UniTask.WaitUntil(() => GameRoot.Instance != null && GameRoot.Instance.Context != null && GameRoot.Instance.GameFlowController != null);
        return GameRoot.Instance.Context;
    }

    // 씬에 런타임 컴포넌트가 없으면 같은 GameObject에 추가해 최소 연결을 보장합니다.
    private void EnsureRuntimeComponents()
    {
        if (battleRuntime == null)
            battleRuntime = GetComponent<NightDefenseBattleRuntime>();

        if (battleRuntime == null)
            battleRuntime = gameObject.AddComponent<NightDefenseBattleRuntime>();

        if (spawnSink == null)
            spawnSink = GetComponent<UnityNightDefenseSpawnSink>();

        if (spawnSink == null)
            spawnSink = gameObject.AddComponent<UnityNightDefenseSpawnSink>();
    }
}
