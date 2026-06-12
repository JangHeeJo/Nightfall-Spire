using Cysharp.Threading.Tasks;
using UnityEngine;

// LobbyScene의 진입점입니다.
// GameRoot의 초기화가 끝난 뒤 로비 Static UI와 Dynamic UI를 순서대로 초기화합니다.
public sealed class LobbySceneRoot : MonoBehaviour
{
    [SerializeField] private LobbyStaticUIRoot staticUIRoot; // 로비 고정 UI 묶음
    [SerializeField] private LobbyDynamicUIRoot dynamicUIRoot; // 로비 동적 UI 묶음

    private void Start()
    {
        InitializeAsync().Forget();
    }

    // 씬이 먼저 떠도 GameRoot.Context가 준비될 때까지 기다립니다.
    private async UniTask InitializeAsync()
    {
        GameContext context = await WaitForContextAsync();

        dynamicUIRoot?.Initialize(context);
        staticUIRoot?.Initialize(context);
    }

    // BootScene에서 LobbyScene으로 넘어오는 타이밍 차이를 흡수합니다.
    private async UniTask<GameContext> WaitForContextAsync()
    {
        await UniTask.WaitUntil(() => GameRoot.Instance != null && GameRoot.Instance.Context != null);
        return GameRoot.Instance.Context;
    }
}
