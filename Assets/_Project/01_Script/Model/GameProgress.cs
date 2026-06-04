using R3;

// 게임의 현재 진행 상태를 담는 모델입니다.
// 상태 변경, 이전 상태 복구, 현재 스테이지 같은 런타임 진행 값을 관리합니다.
public sealed class GameProgress
{
    private readonly SaveData saveData; // 실제 저장 데이터 참조

    public ReactiveProperty<GameState> CurrentState { get; } = new(GameState.None); // 현재 게임 상태
    public ReactiveProperty<GameState> PreviousState { get; } = new(GameState.None); // AppBackground 진입 전 상태
    public ReactiveProperty<int> CurrentStageId { get; } // 현재 진행 중인 스테이지 ID

    public GameProgress(SaveData saveData)
    {
        this.saveData = saveData;

        // SaveData에 저장된 현재 스테이지를 런타임 상태로 가져옵니다.
        CurrentStageId = new ReactiveProperty<int>(saveData.Progress.CurrentStageId);

        // CurrentStageId가 바뀌면 SaveData에도 반영합니다.
        CurrentStageId.Subscribe(stageId => this.saveData.Progress.CurrentStageId = stageId);
    }

    // 게임 상태를 변경합니다.
    // 외부에서 CurrentState.Value를 직접 바꾸지 않고 이 메서드로 통일합니다.
    public void ChangeState(GameState nextState)
    {
        if (CurrentState.Value == nextState)
            return;

        if (nextState == GameState.AppBackground)
            PreviousState.Value = CurrentState.Value;

        CurrentState.Value = nextState;
    }

    // AppBackground에서 돌아올 때 이전 상태로 복귀합니다.
    public void RestorePreviousState()
    {
        if (CurrentState.Value != GameState.AppBackground)
            return;

        GameState restoreState = PreviousState.Value == GameState.None
            ? GameState.Lobby
            : PreviousState.Value;

        ChangeState(restoreState);
        PreviousState.Value = GameState.None;
    }

    // 현재 진행 스테이지를 변경합니다.
    public void SetCurrentStage(int stageId)
    {
        if (stageId <= 0)
            return;

        CurrentStageId.Value = stageId;
    }
}
