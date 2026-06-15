using Cysharp.Threading.Tasks;
using UnityEngine;

// BattleScene의 진입점입니다.
// Unity 씬 생명주기와 씬 오브젝트 참조만 담당하고, 밤 방어 흐름은 BattleSceneController가 처리합니다.
public sealed class BattleSceneRoot : MonoBehaviour
{
    [SerializeField] private BattleStaticUIRoot staticUIRoot; // 밤 방어전 고정 UI 묶음
    [SerializeField] private BattleDynamicUIRoot dynamicUIRoot; // 밤 방어전 동적 UI 묶음
    [SerializeField] private NightDefenseBattleRuntime battleRuntime; // 밤 방어전 런타임 Tick 실행기
    [SerializeField] private UnityNightDefenseSpawnSink spawnSink; // Unity 씬 스폰 요청 수신자

    private BattleSceneController controller; // BattleScene 흐름 Controller

    // 인스펙터 연결이 빠졌더라도 현재 씬 안에서 필요한 전투 구성요소를 복구합니다.
    private void Awake()
    {
        ResolveRuntimeReferences();
    }

    // 씬 오브젝트가 준비되면 Controller를 만들고 전투 씬 초기화를 맡깁니다.
    private void Start()
    {
        ResolveRuntimeReferences();

        controller = new BattleSceneController(
            staticUIRoot,
            dynamicUIRoot,
            battleRuntime,
            spawnSink,
            () => GameRoot.Instance != null ? GameRoot.Instance.Context : null,
            () => GameRoot.Instance != null ? GameRoot.Instance.GameFlowController : null);
        controller.InitializeAsync().Forget();
    }

    // 씬이 사라질 때 Controller가 진행 중인 전투 상태를 정리합니다.
    private void OnDestroy()
    {
        controller?.Dispose();
        controller = null;
    }

    // 씬 YAML이나 프리팹 수정 과정에서 참조가 비어도 전투 진입 자체가 막히지 않게 보강합니다.
    private void ResolveRuntimeReferences()
    {
        staticUIRoot ??= FindSceneComponent<BattleStaticUIRoot>();
        dynamicUIRoot ??= FindSceneComponent<BattleDynamicUIRoot>();
        battleRuntime ??= FindSceneComponent<NightDefenseBattleRuntime>();
        spawnSink ??= FindSceneComponent<UnityNightDefenseSpawnSink>();

        if (battleRuntime == null)
        {
            battleRuntime = gameObject.AddComponent<NightDefenseBattleRuntime>();
            Debug.LogWarning("[BattleSceneRoot] NightDefenseBattleRuntime 참조가 비어 있어 BattleSceneRoot에 런타임 컴포넌트를 자동 생성했습니다.");
        }

        if (spawnSink == null)
        {
            spawnSink = gameObject.AddComponent<UnityNightDefenseSpawnSink>();
            Debug.LogWarning("[BattleSceneRoot] UnityNightDefenseSpawnSink 참조가 비어 있어 BattleSceneRoot에 스폰 수신자를 자동 생성했습니다.");
        }
    }

    // DontDestroyOnLoad 쪽 오브젝트를 잘못 잡지 않도록 현재 BattleScene에 있는 컴포넌트만 찾습니다.
    private T FindSceneComponent<T>() where T : Component
    {
        T[] components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && component.gameObject.scene == gameObject.scene)
                return component;
        }

        return null;
    }
}
