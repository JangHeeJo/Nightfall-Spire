// 상태 전환 규칙이 복잡해질 때 확장할 순수 C# 상태 전환기입니다.
// 현재는 GameProgress.ChangeState가 상태 변경의 단일 진입점이므로 별도 동작은 두지 않습니다.
public sealed class GameStateMachine
{
    public bool CanChange(GameState currentState, GameState nextState)
    {
        return currentState != nextState;
    }
}
