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

        // 저장 데이터를 기반으로 현재 게임 진행 Context를 생성합니다.
        Context = new GameContext(saveData);
        PopupManager.SetContext(Context);

        // 현재 상태를 Boot로 설정합니다.
        Context.GameProgress.ChangeState(GameState.Boot);

        // 로비 씬 로딩 상태로 변경합니다.
        Context.GameProgress.ChangeState(GameState.LobbyLoading);

        // BootScene에서 LobbyScene으로 이동합니다.
        await SceneLoadManager.LoadLobbySceneAsync();

        // 로비 씬 로드 완료 후 상태를 Lobby로 변경합니다.
        Context.GameProgress.ChangeState(GameState.Lobby);

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
    }

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
        // 전투 중 복귀 정책은 추후 BattlePaused나 ResumePopup이 생기면 여기서 확장합니다.
        Context.GameProgress.RestorePreviousState();
    }

    private void OnApplicationQuit()
    {
        // 앱 종료 시점에는 비동기 저장이 끝나기 전에 앱이 닫힐 수 있으므로 즉시 저장을 사용합니다.
        SaveManager?.SaveCurrentImmediate();
    }
}
