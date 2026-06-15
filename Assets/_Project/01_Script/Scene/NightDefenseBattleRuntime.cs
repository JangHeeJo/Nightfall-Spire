using UnityEngine;

// BattleScene에서 순수 C# 전투 세션 런타임을 매 프레임 실행하는 Unity 연결 계층입니다.
public sealed class NightDefenseBattleRuntime : MonoBehaviour
{
    private BattleSessionRuntimeController sessionRuntime; // 웨이브, 전투 계산, 승패를 묶은 실제 전투 런타임
    private bool isTicking; // Update에서 런타임 Tick을 돌릴지 여부

    // BattleSceneController가 GameContext와 표시 계층을 준비한 뒤 전투 런타임을 시작합니다.
    public bool Initialize(GameContext gameContext, GameFlowController gameFlowController, IBattleCombatViewSink viewSink)
    {
        sessionRuntime = new BattleSessionRuntimeController(gameContext, gameFlowController, viewSink);
        BattleRuntimeStartResult startResult = sessionRuntime.Start();

        if (!startResult.IsSuccess)
        {
            Debug.LogError($"[NightDefenseBattleRuntime] 전투 런타임 시작 실패: {startResult.FailureReason}, Combat:{startResult.CombatFailureReason}, Wave:{startResult.NightDefenseFailureReason}");
            isTicking = false;
            return false;
        }

        isTicking = true;
        Debug.Log("[NightDefenseBattleRuntime] 전투 런타임 시작 완료");
        return true;
    }

    // Unity 프레임 시간에 맞춰 밤 방어 세션 런타임을 진행합니다.
    private void Update()
    {
        if (!isTicking || sessionRuntime == null)
            return;

        BattleRuntimeTickResult result = sessionRuntime.Tick(Time.deltaTime);

        if (result.State == BattleRuntimeState.Victory
            || result.State == BattleRuntimeState.Defeat
            || result.State == BattleRuntimeState.Abandoned)
        {
            isTicking = false;
        }
    }

    // 씬이 내려갈 때 진행 중인 런타임을 중단 상태로 정리합니다.
    private void OnDestroy()
    {
        sessionRuntime?.StopAsAbandoned();
        sessionRuntime = null;
        isTicking = false;
    }
}
