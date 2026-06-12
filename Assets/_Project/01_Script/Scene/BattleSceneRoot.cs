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

    // 씬 오브젝트가 준비되면 Controller를 만들고 전투 씬 초기화를 맡깁니다.
    private void Start()
    {
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
}
