using NUnit.Framework;

// GameFlowController가 상태 머신 규칙을 지키며 Progress를 변경하는지 검증합니다.
public sealed class GameFlowControllerTests
{
    // 정상적인 낮 준비에서 밤 방어전 시작까지의 흐름을 검증합니다.
    [Test]
    public void NightDefenseFlow_StartsOnlyAfterLoadingState()
    {
        GameContext context = CreateContextAtDayPreparation();
        GameFlowController controller = new GameFlowController(context, null);

        Assert.That(context.GameProgress.ChangeState(GameState.NightDefenseLoading), Is.True);
        Assert.That(controller.BeginLoadedNightDefenseSession(), Is.True);
        Assert.That(context.GameProgress.CurrentState.Value, Is.EqualTo(GameState.NightDefensePlaying));
        Assert.That(context.NightDefenseProgress.IsDefenseActive.Value, Is.True);
    }

    // 밤 방어 로딩 상태가 아니면 씬 로드 완료 콜백이 세션을 시작하지 않아야 합니다.
    [Test]
    public void NightDefenseFlow_BlocksLoadedSessionFromWrongState()
    {
        GameContext context = CreateContextAtDayPreparation();
        GameFlowController controller = new GameFlowController(context, null);

        Assert.That(controller.BeginLoadedNightDefenseSession(), Is.False);
        Assert.That(context.GameProgress.CurrentState.Value, Is.EqualTo(GameState.DayPreparation));
        Assert.That(context.NightDefenseProgress.IsDefenseActive.Value, Is.False);
    }

    // 드래프트는 밤 방어 진행 중에만 열리고, 선택 후 다시 밤 방어 진행 상태로 돌아와야 합니다.
    [Test]
    public void DraftFlow_OpensAndReturnsToNightDefense()
    {
        GameContext context = CreateContextAtNightDefensePlaying();
        GameFlowController controller = new GameFlowController(context, null);

        Assert.That(controller.OpenDraftSelection(new[] { 101, 102, 103 }), Is.True);
        Assert.That(context.GameProgress.CurrentState.Value, Is.EqualTo(GameState.DraftSelection));
        Assert.That(context.DraftProgress.IsDraftOpen.Value, Is.True);

        Assert.That(controller.SelectDraftCard(102), Is.True);
        Assert.That(context.GameProgress.CurrentState.Value, Is.EqualTo(GameState.NightDefensePlaying));
        Assert.That(context.DraftProgress.IsDraftOpen.Value, Is.False);
        Assert.That(context.DraftProgress.SelectedCardIds, Does.Contain(102));
    }

    // 방어전 결과 확정은 활성화된 밤 방어전에서만 진행되어야 합니다.
    [Test]
    public void ResultFlow_BlocksResultWhenDefenseIsNotActive()
    {
        GameContext context = CreateContextAtDayPreparation();
        GameFlowController controller = new GameFlowController(context, null);

        Assert.That(controller.CompleteNightDefense(DefenseOutcome.Victory, 100, 1), Is.False);
        Assert.That(context.GameProgress.CurrentState.Value, Is.EqualTo(GameState.DayPreparation));
        Assert.That(context.RewardProgress.PendingGold, Is.EqualTo(0));
    }

    // 승리 결과는 진행도, 낮 루프, 수령 대기 보상을 함께 반영해야 합니다.
    [Test]
    public void ResultFlow_VictoryUpdatesProgressAndReward()
    {
        GameContext context = CreateContextAtNightDefensePlaying();
        GameFlowController controller = new GameFlowController(context, null);

        Assert.That(controller.CompleteNightDefense(DefenseOutcome.Victory, 100, 1), Is.True);
        Assert.That(context.GameProgress.CurrentState.Value, Is.EqualTo(GameState.NightDefenseResult));
        Assert.That(context.NightDefenseProgress.IsDefenseActive.Value, Is.False);
        Assert.That(context.GameProgress.HighestClearedDefenseSessionId.Value, Is.EqualTo(101));
        Assert.That(context.GameProgress.CurrentDefenseSessionId.Value, Is.EqualTo(102));
        Assert.That(context.GameProgress.CompletedDayCount.Value, Is.EqualTo(1));
        Assert.That(context.RewardProgress.PendingGold, Is.EqualTo(100));
        Assert.That(context.RewardProgress.PendingGem, Is.EqualTo(1));
    }

    // 테스트용으로 낮 준비 상태까지 정상 흐름을 밟아 만듭니다.
    private static GameContext CreateContextAtDayPreparation()
    {
        GameContext context = new GameContext(SaveData.CreateDefault());
        context.GameProgress.ChangeState(GameState.Boot);
        context.GameProgress.ChangeState(GameState.DayPreparationLoading);

        GameFlowController controller = new GameFlowController(context, null);
        Assert.That(controller.EnterDayPreparation(), Is.True);

        return context;
    }

    // 테스트용으로 밤 방어 진행 상태까지 정상 흐름을 밟아 만듭니다.
    private static GameContext CreateContextAtNightDefensePlaying()
    {
        GameContext context = CreateContextAtDayPreparation();
        GameFlowController controller = new GameFlowController(context, null);
        context.GameProgress.ChangeState(GameState.NightDefenseLoading);
        controller.BeginLoadedNightDefenseSession();
        return context;
    }
}
