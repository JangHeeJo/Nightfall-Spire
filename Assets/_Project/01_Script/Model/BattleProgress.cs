using R3;

// 밤 방어전 안에서만 쓰는 전투 상태 모델입니다.
// NightDefenseProgress는 세션/웨이브 번호를 담당하고, BattleProgress는 성채 체력과 승패 상태를 담당합니다.
public sealed class BattleProgress
{
    public const int DefaultCastleMaxHp = 100; // 초기 테스트 전투에서 사용할 기본 성채 체력

    public ReactiveProperty<BattleRuntimeState> CurrentState { get; } = new(BattleRuntimeState.Idle); // 현재 전투 런타임 상태
    public ReactiveProperty<int> CastleMaxHp { get; } = new(DefaultCastleMaxHp); // 성채 최대 체력
    public ReactiveProperty<int> CastleCurrentHp { get; } = new(DefaultCastleMaxHp); // 성채 현재 체력

    public bool IsBattleActive => CurrentState.Value == BattleRuntimeState.Playing
                                  || CurrentState.Value == BattleRuntimeState.WaitingForDraft; // 전투가 종료되지 않은 활성 상태인지 여부

    // 기본 체력으로 전투 상태를 시작합니다.
    public void BeginBattle()
    {
        BeginBattle(DefaultCastleMaxHp);
    }

    // 지정한 성채 체력으로 전투 상태를 시작합니다.
    public void BeginBattle(int castleMaxHp)
    {
        int safeMaxHp = castleMaxHp <= 0 ? DefaultCastleMaxHp : castleMaxHp;
        CastleMaxHp.Value = safeMaxHp;
        CastleCurrentHp.Value = safeMaxHp;
        CurrentState.Value = BattleRuntimeState.Playing;
    }

    // 전투 상태만 바꿉니다. 드래프트 대기처럼 승패가 아닌 중간 상태 전환에 사용합니다.
    public void ChangeState(BattleRuntimeState state)
    {
        CurrentState.Value = state;
    }

    // 적이 성채에 도달했을 때 체력을 깎고 남은 체력을 반환합니다.
    public int ApplyCastleDamage(int damage)
    {
        if (damage <= 0 || !IsBattleActive)
            return CastleCurrentHp.Value;

        int nextHp = CastleCurrentHp.Value - damage;

        if (nextHp < 0)
            nextHp = 0;

        CastleCurrentHp.Value = nextHp;

        if (nextHp <= 0)
            CurrentState.Value = BattleRuntimeState.Defeat;

        return nextHp;
    }

    // 이전 호환 경로에서 전투 종료만 표시해야 할 때 사용합니다.
    public void EndBattle()
    {
        CurrentState.Value = BattleRuntimeState.Idle;
    }

    // 승리, 패배, 중단 결과에 맞춰 전투 상태를 종료합니다.
    public void EndBattle(DefenseOutcome outcome)
    {
        CurrentState.Value = outcome switch
        {
            DefenseOutcome.Victory => BattleRuntimeState.Victory,
            DefenseOutcome.Defeat => BattleRuntimeState.Defeat,
            DefenseOutcome.Abandoned => BattleRuntimeState.Abandoned,
            _ => BattleRuntimeState.Idle
        };
    }
}
