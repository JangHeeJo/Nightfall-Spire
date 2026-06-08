using NUnit.Framework;

// GameStateMachine과 GameProgress의 상태 전환 계약을 검증합니다.
public sealed class GameStateMachineTests
{
    // 부팅부터 낮 준비까지의 기본 앱 시작 흐름은 허용되어야 합니다.
    [Test]
    public void BootFlow_AllowsStartupToDayPreparation()
    {
        GameProgress progress = new GameProgress(SaveData.CreateDefault());

        Assert.That(progress.ChangeState(GameState.Boot), Is.True);
        Assert.That(progress.ChangeState(GameState.DayPreparationLoading), Is.True);
        Assert.That(progress.ChangeState(GameState.DayPreparation), Is.True);
        Assert.That(progress.CurrentState.Value, Is.EqualTo(GameState.DayPreparation));
    }

    // 낮 준비에서 드래프트 선택으로 직접 뛰어넘는 전환은 막아야 합니다.
    [Test]
    public void InvalidTransition_DoesNotChangeState()
    {
        GameProgress progress = new GameProgress(SaveData.CreateDefault());
        progress.ChangeState(GameState.Boot);
        progress.ChangeState(GameState.DayPreparationLoading);
        progress.ChangeState(GameState.DayPreparation);

        bool changed = progress.ChangeState(GameState.DraftSelection);

        Assert.That(changed, Is.False);
        Assert.That(progress.CurrentState.Value, Is.EqualTo(GameState.DayPreparation));
    }

    // 밤 방어 진행 중에는 드래프트 선택으로 들어갔다가 다시 전투 진행으로 돌아올 수 있어야 합니다.
    [Test]
    public void NightDefenseFlow_AllowsDraftRoundTrip()
    {
        GameProgress progress = CreateNightDefensePlayingProgress();

        Assert.That(progress.ChangeState(GameState.DraftSelection), Is.True);
        Assert.That(progress.ChangeState(GameState.NightDefensePlaying), Is.True);
        Assert.That(progress.CurrentState.Value, Is.EqualTo(GameState.NightDefensePlaying));
    }

    // 백그라운드 진입 시 이전 상태를 저장하고, 복귀 시 이전 상태로 돌아와야 합니다.
    [Test]
    public void BackgroundFlow_RestoresPreviousState()
    {
        GameProgress progress = CreateNightDefensePlayingProgress();

        Assert.That(progress.ChangeState(GameState.AppBackground), Is.True);
        Assert.That(progress.CurrentState.Value, Is.EqualTo(GameState.AppBackground));
        Assert.That(progress.PreviousState.Value, Is.EqualTo(GameState.NightDefensePlaying));

        Assert.That(progress.RestorePreviousState(), Is.True);
        Assert.That(progress.CurrentState.Value, Is.EqualTo(GameState.NightDefensePlaying));
        Assert.That(progress.PreviousState.Value, Is.EqualTo(GameState.None));
    }

    // 백그라운드에서 Boot나 None으로 복귀하는 전환은 허용하지 않습니다.
    [Test]
    public void BackgroundFlow_BlocksInvalidRestoreTargets()
    {
        GameStateMachine stateMachine = new GameStateMachine();

        Assert.That(stateMachine.CanChange(GameState.AppBackground, GameState.Boot), Is.False);
        Assert.That(stateMachine.CanChange(GameState.AppBackground, GameState.None), Is.False);
        Assert.That(stateMachine.CanChange(GameState.AppBackground, GameState.DayPreparation), Is.True);
    }

    // 테스트용으로 밤 방어 진행 상태까지 정상 흐름을 밟아 만듭니다.
    private static GameProgress CreateNightDefensePlayingProgress()
    {
        GameProgress progress = new GameProgress(SaveData.CreateDefault());
        progress.ChangeState(GameState.Boot);
        progress.ChangeState(GameState.DayPreparationLoading);
        progress.ChangeState(GameState.DayPreparation);
        progress.ChangeState(GameState.NightDefenseLoading);
        progress.ChangeState(GameState.NightDefensePlaying);
        return progress;
    }
}
