using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 전환을 담당하는 매니저입니다.
// 코루틴을 사용하지 않고 UniTask 기반으로 씬 로딩을 처리합니다.
public sealed class SceneLoadManager
{
    public const string LobbySceneName = "LobbyScene"; // 로비 씬 이름
    public const string BattleSceneName = "BattleScene"; // 전투 씬 이름

    // 지정한 씬을 비동기로 로드합니다.
    public async UniTask LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneLoadManager] 씬 이름이 비어 있습니다.");
            return;
        }

        Debug.Log($"[SceneLoadManager] 씬 로드 시작: {sceneName}");

        // Unity AsyncOperation을 UniTask로 변환해서 대기합니다.
        await SceneManager.LoadSceneAsync(sceneName).ToUniTask();

        Debug.Log($"[SceneLoadManager] 씬 로드 완료: {sceneName}");
    }

    // 로비 씬으로 이동합니다.
    public UniTask LoadLobbySceneAsync()
    {
        return LoadSceneAsync(LobbySceneName);
    }

    // 전투 씬으로 이동합니다.
    public UniTask LoadBattleSceneAsync()
    {
        return LoadSceneAsync(BattleSceneName);
    }
}