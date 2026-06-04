using Cysharp.Threading.Tasks;
using UnityEngine;

// 게임 전체의 시작점입니다.
// BootScene의 GameObject 이름도 GameRoot로 맞춥니다.
public sealed class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; } // 전역 접근용 인스턴스

    public GameContext Context { get; private set; } // 현재 게임 진행 데이터 묶음
    public SaveManager SaveManager { get; private set; } // 저장 매니저
    public SceneLoadManager SceneLoadManager { get; private set; } // 씬 로드 매니저
    public DataTableManager DataTableManager { get; private set; } // 테이블 데이터 매니저
    public ServiceRegistry ServiceRegistry { get; private set; } // 외부 서비스 등록소
    public PopupManager PopupManager { get; private set; } // 현재 씬 팝업 레이어 관리자
    public ScreenFadeManager ScreenFadeManager { get; private set; } // 현재 씬 화면 페이드 관리자
    public GameFlowController GameFlowController { get; private set; } // 낮/밤 흐름 제어자

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
        Debug.Log("[GameRoot] 초기화 시작");

        CreateCoreSystems();

        // 저장 데이터를 먼저 불러옵니다.
        SaveData saveData = await SaveManager.LoadAsync();

        // 저장 데이터를 기반으로 현재 게임 진행 Context와 루프 지휘자를 생성합니다.
        Context = new GameContext(saveData);
        GameFlowController = new GameFlowController(Context, SceneLoadManager);
        PopupManager.SetContext(Context);

        // 현재 상태를 Boot로 설정합니다.
        Context.GameProgress.ChangeState(GameState.Boot);

        // 낮 준비 씬 로딩 상태로 변경합니다.
        Context.GameProgress.ChangeState(GameState.DayPreparationLoading);

        // BootScene에서 LobbyScene으로 이동합니다. LobbyScene은 낮 준비 화면 역할을 먼저 맡습니다.
        await SceneLoadManager.LoadLobbySceneAsync();

        // 로비 씬 로드 완료 후 낮 준비 상태로 진입합니다.
        GameFlowController.EnterDayPreparation();

        Debug.Log("[GameRoot] 초기화 완료");
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
