using Cysharp.Threading.Tasks;
using UnityEngine;

// BattleScene의 진입점입니다.
// 씬 이름은 기존 연결을 유지하지만, 실제 런타임 모델은 GameCycleDirector가 시작하는 밤 방어 세션입니다.
public sealed class BattleSceneRoot : MonoBehaviour
{
    [SerializeField] private BattleStaticUIRoot staticUIRoot; // 밤 방어전 고정 UI 묶음
    [SerializeField] private BattleDynamicUIRoot dynamicUIRoot; // 밤 방어전 동적 UI 묶음

    private void Start()
    {
        InitializeAsync().Forget();
    }

    // 씬 진입 후 GameContext를 기다리고 밤 방어 세션과 UI Root를 초기화합니다.
    private async UniTask InitializeAsync()
    {
        GameContext context = await WaitForContextAsync();

        GameRoot.Instance.GameCycleDirector.BeginLoadedNightDefenseSession();

        staticUIRoot?.Initialize(context);
        dynamicUIRoot?.Initialize(context);
    }

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
        await UniTask.WaitUntil(() => GameRoot.Instance != null && GameRoot.Instance.Context != null && GameRoot.Instance.GameCycleDirector != null);
        return GameRoot.Instance.Context;
    }
}
