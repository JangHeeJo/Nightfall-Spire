using Cysharp.Threading.Tasks;
using UnityEngine;

// 게임 전체의 시작점입니다.
// BootScene의 GameObject 이름도 GameRoot로 맞춥니다.
public sealed class GameRoot : MonoBehaviour
{
    private const float MinimumBootLoadingSeconds = 2.6f; // 로딩이 빨라도 부트 화면 진행과 배경 전환을 보여줄 최소 시간

    public static GameRoot Instance { get; private set; } // 전역 접근용 인스턴스

    public GameContext Context { get; private set; } // 현재 게임 진행 데이터 묶음
    public SaveManager SaveManager { get; private set; } // 저장 매니저
    public SceneLoadManager SceneLoadManager { get; private set; } // 씬 로드 매니저
    public DataTableManager DataTableManager { get; private set; } // 테이블 데이터 매니저
    public ServiceRegistry ServiceRegistry { get; private set; } // 외부 서비스 등록소
    public PopupManager PopupManager { get; private set; } // 현재 씬 팝업 레이어 관리자
    public ScreenFadeManager ScreenFadeManager { get; private set; } // 현재 씬 화면 페이드 관리자
    public GameFlowController GameFlowController { get; private set; } // 낮/밤 흐름 제어자

    private BootLoadingPresenter bootLoadingPresenter; // BootScene 로딩 화면 표시 담당

    // 앱 시작 시 GameRoot 단일 인스턴스를 만들고 전체 초기화 흐름을 시작합니다.
    private void Awake()
    {
        // 중복 GameRoot 생성을 방지합니다.
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // GameRoot는 씬 전환 후에도 유지합니다.
        DontDestroyOnLoad(gameObject);

        // Unity 생명주기 함수에서 async 흐름을 시작하기 위해 Forget을 사용합니다.
        InitializeAsync().Forget();
    }

    // 게임 시작에 필요한 핵심 시스템을 순서대로 초기화합니다.
    private async UniTask InitializeAsync()
    {
        float bootStartTime = Time.realtimeSinceStartup;

        Debug.Log("[GameRoot] 초기화 시작");

        CreateCoreSystems();
        CreateBootLoadingPresenter();

        // 밸런스 테이블을 먼저 읽어야 저장 데이터와 런타임 시스템이 같은 기준 데이터를 참조할 수 있습니다.
        bootLoadingPresenter?.Report(0.15f, "Loading data...");
        await DataTableManager.LoadAllAsync();

        // 저장 데이터를 먼저 불러옵니다.
        bootLoadingPresenter?.Report(0.45f, "Loading save...");
        SaveData saveData = await SaveManager.LoadAsync();

        // 저장 데이터와 테이블 조회 어댑터를 기반으로 현재 게임 진행 Context와 도메인 서비스를 생성합니다.
        bootLoadingPresenter?.Report(0.65f, "Preparing systems...");
        GameContentDataSource contentDataSource = new GameContentDataSource(DataTableManager);
        Context = new GameContext(saveData, contentDataSource);
        GameFlowController = new GameFlowController(Context, SceneLoadManager);
        PopupManager.SetContext(Context);

        // 현재 상태를 Boot로 설정합니다.
        Context.GameProgress.ChangeState(GameState.Boot);

        // 낮 준비 씬 로딩 상태로 변경합니다.
        Context.GameProgress.ChangeState(GameState.DayPreparationLoading);

        // BootScene에서 LobbyScene으로 이동합니다. LobbyScene은 낮 준비 화면 역할을 먼저 맡습니다.
        bootLoadingPresenter?.Report(0.85f, "Loading lobby...");
        await WaitForMinimumBootLoadingTimeAsync(bootStartTime);
        bootLoadingPresenter?.Complete();
        await WaitRealtimeSecondsAsync(0.25f);
        await SceneLoadManager.LoadLobbySceneAsync();

        // 로비 씬 로드 완료 후 낮 준비 상태로 진입합니다.
        GameFlowController.EnterDayPreparation();

        Debug.Log("[GameRoot] 초기화 완료");
    }

    // 실제 초기화가 빨리 끝나도 로딩바와 낮/밤 배경 전환을 볼 수 있게 최소 표시 시간을 보장합니다.
    private static async UniTask WaitForMinimumBootLoadingTimeAsync(float bootStartTime)
    {
        while (Time.realtimeSinceStartup - bootStartTime < MinimumBootLoadingSeconds)
            await UniTask.Yield();
    }

    // 시간 배율과 상관없이 짧은 UI 연출 시간을 기다립니다.
    private static async UniTask WaitRealtimeSecondsAsync(float seconds)
    {
        float waitStartTime = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - waitStartTime < seconds)
            await UniTask.Yield();
    }

    // Core 시스템들을 생성합니다.
    // MonoBehaviour가 필요 없는 시스템은 순수 C# 클래스로 유지합니다.
    private void CreateCoreSystems()
    {
        SaveManager = new SaveManager();
        SceneLoadManager = new SceneLoadManager();
        DataTableManager = new DataTableManager();
        ServiceRegistry = new ServiceRegistry();
        PopupManager = new PopupManager();
        ScreenFadeManager = new ScreenFadeManager();
    }

    // BootScene에 로딩 View가 있으면 Presenter를 연결합니다.
    private void CreateBootLoadingPresenter()
    {
        BootLoadingView bootLoadingView = FindFirstObjectByType<BootLoadingView>();

        if (bootLoadingView == null)
            return;

        bootLoadingPresenter = new BootLoadingPresenter(bootLoadingView);
        bootLoadingPresenter.Initialize($"v{Application.version}");
    }

    // 앱이 백그라운드로 내려가거나 돌아올 때 저장과 상태 복구를 처리합니다.
    private void OnApplicationPause(bool pauseStatus)
    {
        if (Context == null)
            return;

        if (pauseStatus)
        {
            // 앱이 백그라운드로 내려갈 때 상태를 변경하고 저장합니다.
            Context.GameProgress.ChangeState(GameState.AppBackground);
            SaveManager.SaveCurrentAsync().Forget();
            return;
        }

        // 앱 복귀 시에는 이전 상태로 돌아갑니다.
        // 밤 방어전 중 복귀 정책은 추후 일시정지 팝업과 세션 복구 규칙이 생기면 여기서 확장합니다.
        Context.GameProgress.RestorePreviousState();
    }

    // 앱 종료 직전에 현재 저장 데이터를 즉시 파일로 남깁니다.
    private void OnApplicationQuit()
    {
        // 앱 종료 시점에는 비동기 저장이 끝나기 전에 앱이 닫힐 수 있으므로 즉시 저장을 사용합니다.
        SaveManager?.SaveCurrentImmediate();
    }
}
