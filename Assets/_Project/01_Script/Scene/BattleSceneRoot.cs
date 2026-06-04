using Cysharp.Threading.Tasks;
using UnityEngine;

// BattleScene의 진입점입니다.
// 전투 UI와 전투 컨트롤러가 GameContext를 기준으로 초기화되도록 순서를 잡습니다.
public sealed class BattleSceneRoot : MonoBehaviour
{
    [SerializeField] private BattleStaticUIRoot staticUIRoot; // 전투 고정 UI 묶음
    [SerializeField] private BattleDynamicUIRoot dynamicUIRoot; // 전투 동적 UI 묶음

    private void Start()
    {
        InitializeAsync().Forget();
    }

    // 씬 진입 후 GameContext를 기다리고 UI Root를 초기화합니다.
    private async UniTask InitializeAsync()
    {
        GameContext context = await WaitForContextAsync();

        context.BattleProgress.BeginBattle();
        staticUIRoot?.Initialize(context);
        dynamicUIRoot?.Initialize(context);
    }

    private void OnDestroy()
    {
        GameRoot.Instance?.Context?.BattleProgress.EndBattle();
    }

    // Boot/Lobby에서 BattleScene으로 넘어오는 타이밍 차이를 흡수합니다.
    private async UniTask<GameContext> WaitForContextAsync()
    {
        await UniTask.WaitUntil(() => GameRoot.Instance != null && GameRoot.Instance.Context != null);
        return GameRoot.Instance.Context;
    }
}
