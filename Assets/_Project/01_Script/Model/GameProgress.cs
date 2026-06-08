using R3;

// 게임의 현재 진행 상태를 담는 모델입니다.
// 일반 스테이지가 아니라 다음 밤 방어 세션과 낮/밤 루프 진행 값을 관리합니다.
public sealed class GameProgress
{
    private readonly SaveData saveData; // 실제 저장 데이터 참조
    private readonly GameStateMachine stateMachine; // 게임 상태 전환 규칙

    public ReactiveProperty<GameState> CurrentState { get; } = new(GameState.None); // 현재 게임 상태
    public ReactiveProperty<GameState> PreviousState { get; } = new(GameState.None); // AppBackground 진입 전 상태
    public ReactiveProperty<int> CurrentDefenseSessionId { get; } // 다음에 도전할 밤 방어 세션 ID
    public ReactiveProperty<int> HighestClearedDefenseSessionId { get; } // 가장 멀리 클리어한 밤 방어 세션 ID
    public ReactiveProperty<int> CompletedDayCount { get; } // 완료한 낮/밤 루프 수

    // 저장된 전체 진행 값을 런타임에서 구독 가능한 상태로 변환합니다.
    public GameProgress(SaveData saveData, GameStateMachine stateMachine = null)
    {
        this.saveData = saveData;
        this.stateMachine = stateMachine ?? new GameStateMachine();

        // SaveData에 저장된 진행 값을 런타임 상태로 가져옵니다.
        CurrentDefenseSessionId = new ReactiveProperty<int>(saveData.Progress.CurrentDefenseSessionId);
        HighestClearedDefenseSessionId = new ReactiveProperty<int>(saveData.Progress.HighestClearedDefenseSessionId);
        CompletedDayCount = new ReactiveProperty<int>(saveData.Progress.CompletedDayCount);

        // 진행 값이 바뀌면 SaveData에도 반영합니다.
        CurrentDefenseSessionId.Subscribe(sessionId => this.saveData.Progress.CurrentDefenseSessionId = sessionId);
        HighestClearedDefenseSessionId.Subscribe(sessionId => this.saveData.Progress.HighestClearedDefenseSessionId = sessionId);
        CompletedDayCount.Subscribe(dayCount => this.saveData.Progress.CompletedDayCount = dayCount);
    }

    // 게임 상태를 변경합니다.
    // 외부에서 CurrentState.Value를 직접 바꾸지 않고 이 메서드로 통일합니다.
    public bool ChangeState(GameState nextState)
    {
        if (CurrentState.Value == nextState)
            return false;

        if (!stateMachine.CanChange(CurrentState.Value, nextState))
            return false;

        if (nextState == GameState.AppBackground)
            PreviousState.Value = CurrentState.Value;

        CurrentState.Value = nextState;
        return true;
    }

    // AppBackground에서 돌아올 때 이전 상태로 복귀합니다.
    public bool RestorePreviousState()
    {
        if (CurrentState.Value != GameState.AppBackground)
            return false;

        GameState restoreState = PreviousState.Value == GameState.None
            ? GameState.DayPreparation
            : PreviousState.Value;

        bool restored = ChangeState(restoreState);

        if (restored)
            PreviousState.Value = GameState.None;

        return restored;
    }

    // 다음에 도전할 밤 방어 세션을 변경합니다.
    public void SetCurrentDefenseSession(int sessionId)
    {
        if (sessionId <= 0)
            return;

        CurrentDefenseSessionId.Value = sessionId;
    }

    // 낮/밤 루프 1회를 완료 처리합니다.
    public void CompleteDayCycle()
    {
        CompletedDayCount.Value += 1;
    }

    // 방어 성공 시 최고 클리어 기록과 다음 세션을 갱신합니다.
    public void CompleteDefenseSession(int clearedSessionId)
    {
        if (clearedSessionId <= 0)
            return;

        if (HighestClearedDefenseSessionId.Value < clearedSessionId)
            HighestClearedDefenseSessionId.Value = clearedSessionId;

        if (CurrentDefenseSessionId.Value <= clearedSessionId)
            CurrentDefenseSessionId.Value = clearedSessionId + 1;
    }
}
