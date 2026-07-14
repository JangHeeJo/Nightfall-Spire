using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// BattleScene의 진입점입니다.
// Unity 씬 생명주기와 씬 오브젝트 참조만 담당하고, 밤 방어 흐름은 BattleSceneController가 처리합니다.
public sealed class BattleSceneRoot : MonoBehaviour
{
    [SerializeField] private BattleStaticUIRoot staticUIRoot; // 밤 방어전 고정 UI 묶음
    [SerializeField] private BattleDynamicUIRoot dynamicUIRoot; // 밤 방어전 동적 UI 묶음
    [SerializeField] private NightDefenseBattleRuntime battleRuntime; // 밤 방어전 런타임 Tick 실행기
    [SerializeField] private UnityNightDefenseSpawnSink spawnSink; // Unity 씬 스폰 요청 수신자
    [SerializeField] private Camera battleCamera; // 전투 시작 카메라 연출 대상
    [SerializeField] private bool playBattleCameraIntroOnStart = true; // 씬 시작 때 카메라 인트로를 재생할지 여부
    [SerializeField] private Vector3 cameraStartPosition = new(0f, 0.55f, -10f); // 전장 전체를 먼저 보여주는 시작 위치
    [SerializeField] private Vector3 cameraRightLanePosition = new(2.35f, -0.2f, -10f); // 오른쪽 라인을 확인하는 위치
    [SerializeField] private Vector3 cameraLeftLanePosition = new(-2.2f, -0.15f, -10f); // 왼쪽 라인을 확인하는 위치
    [SerializeField] private Vector2 cameraCastleCenterPosition = new(0f, -0.8f); // 성채 반쪽 고정 기준 위치
    [SerializeField] private Vector2 cameraFocusTargetPosition = new(0.75f, -1.45f); // 최종 포커싱이 바라볼 몬스터 접전 지점
    [SerializeField] private float cameraStartOrthographicSize = 6.4f; // 시작 와이드 카메라 크기
    [SerializeField] private float cameraFocusOrthographicSize = 5.2f; // 최종 포커스 카메라 크기
    [SerializeField] private float cameraSettleSeconds = 0.35f; // 시작 위치에서 잠깐 머무는 시간
    [SerializeField] private float cameraLaneMoveSeconds = 0.9f; // 좌우 라인을 훑는 이동 시간
    [SerializeField] private float cameraFocusMoveSeconds = 0.75f; // 최종 포커스로 이동하는 시간

    private BattleSceneController controller; // BattleScene 흐름 Controller
    private Tween battleCameraIntroTween; // 진행 중인 전투 카메라 인트로 Tween

    // 씬 오브젝트가 준비되면 Controller를 만들고 전투 씬 초기화를 맡깁니다.
    private void Start()
    {
        PlayBattleCameraIntro();

        controller = new BattleSceneController(
            staticUIRoot,
            dynamicUIRoot,
            battleRuntime,
            spawnSink,
            () => GameRoot.Instance != null ? GameRoot.Instance.Context : null,
            () => GameRoot.Instance != null ? GameRoot.Instance.GameFlowController : null);
        controller.InitializeAsync().Forget();
    }

    // 전투 시작 카메라 연출은 TweenManager에 위임하고, 씬 루트는 대상과 설정값만 넘깁니다.
    private void PlayBattleCameraIntro()
    {
        if (!playBattleCameraIntroOnStart)
            return;

        battleCameraIntroTween = PopupTweenManager.PlayBattleCameraIntro(
            battleCamera,
            cameraStartPosition,
            cameraRightLanePosition,
            cameraLeftLanePosition,
            cameraCastleCenterPosition,
            cameraFocusTargetPosition,
            cameraStartOrthographicSize,
            cameraFocusOrthographicSize,
            cameraSettleSeconds,
            cameraLaneMoveSeconds,
            cameraFocusMoveSeconds);
    }

    // 씬이 사라질 때 Controller가 진행 중인 전투 상태를 정리합니다.
    private void OnDestroy()
    {
        battleCameraIntroTween?.Kill();
        battleCameraIntroTween = null;
        controller?.Dispose();
        controller = null;
    }
}
