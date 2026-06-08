// 게임 전체 상태 전환 규칙을 검증하는 순수 C# 상태 머신입니다.
// UI나 서비스가 Progress를 직접 바꾸더라도 이 규칙을 통과해야 상태가 바뀝니다.
public sealed class GameStateMachine
{
    // 현재 상태에서 다음 상태로 이동할 수 있는지 확인합니다.
    public bool CanChange(GameState currentState, GameState nextState)
    {
        if (currentState == nextState)
            return false;

        if (nextState == GameState.AppBackground)
            return CanEnterBackground(currentState);

        if (currentState == GameState.AppBackground)
            return CanRestoreFromBackground(nextState);

        switch (currentState)
        {
            case GameState.None:
                return nextState == GameState.Boot;

            case GameState.Boot:
                return nextState == GameState.DayPreparationLoading;

            case GameState.DayPreparationLoading:
                return nextState == GameState.DayPreparation;

            case GameState.DayPreparation:
                return nextState == GameState.NightDefenseLoading;

            case GameState.NightDefenseLoading:
                return nextState == GameState.NightDefenseReady ||
                       nextState == GameState.NightDefensePlaying;

            case GameState.NightDefenseReady:
                return nextState == GameState.NightDefensePlaying ||
                       nextState == GameState.DayPreparationLoading;

            case GameState.NightDefensePlaying:
                return nextState == GameState.DraftSelection ||
                       nextState == GameState.NightDefenseResult ||
                       nextState == GameState.DayPreparationLoading;

            case GameState.DraftSelection:
                return nextState == GameState.NightDefensePlaying ||
                       nextState == GameState.NightDefenseResult;

            case GameState.NightDefenseResult:
                return nextState == GameState.DayPreparationLoading;

            default:
                return false;
        }
    }

    // 백그라운드는 실제 플레이 흐름 중인 상태에서만 진입할 수 있습니다.
    private static bool CanEnterBackground(GameState currentState)
    {
        return currentState != GameState.None &&
               currentState != GameState.AppBackground;
    }

    // 앱 복귀 시에는 이전에 저장해 둔 플레이 흐름 상태로만 돌아갑니다.
    private static bool CanRestoreFromBackground(GameState nextState)
    {
        return nextState != GameState.None &&
               nextState != GameState.Boot &&
               nextState != GameState.AppBackground;
    }
}
