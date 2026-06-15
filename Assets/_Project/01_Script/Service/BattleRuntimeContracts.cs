using System.Collections.Generic;

// 전투 씬 전체 런타임 상태입니다.
// 게임 전체 GameState보다 더 좁은 범위에서, 한 밤 방어전 안의 진행 단계를 표현합니다.
public enum BattleRuntimeState
{
    Idle, // 전투 런타임이 아직 시작되지 않음
    Playing, // 웨이브 스폰과 전투 계산이 진행 중
    WaitingForDraft, // 웨이브 종료 후 드래프트 선택을 기다리는 중
    Victory, // 모든 웨이브와 남은 적을 정리해 승리함
    Defeat, // 성채 체력이 0이 되어 패배함
    Abandoned // 씬 이탈이나 중단으로 전투가 종료됨
}

// 전투 세션 런타임 시작 실패 이유입니다.
public enum BattleRuntimeFailureReason
{
    None, // 실패 없음
    ContextMissing, // GameContext가 비어 있음
    GameFlowControllerMissing, // 게임 흐름 컨트롤러가 비어 있음
    ContentDataSourceMissing, // 전투 데이터 소스가 비어 있음
    SessionServiceMissing, // 밤 방어 세션 서비스가 비어 있음
    CombatSlotBuildFailed, // 전투 슬롯 구성에 실패함
    WaveStartFailed // 첫 웨이브 시작에 실패함
}

// 전투 세션 런타임 시작 결과입니다.
public readonly struct BattleRuntimeStartResult
{
    public bool IsSuccess { get; } // 시작 성공 여부
    public BattleRuntimeFailureReason FailureReason { get; } // 실패 이유
    public CombatRuntimeFailureReason CombatFailureReason { get; } // 전투 슬롯 구성 실패 상세
    public NightDefenseFailureReason NightDefenseFailureReason { get; } // 웨이브 시작 실패 상세

    // 시작 성공 여부와 실패 상세 값을 보관합니다.
    private BattleRuntimeStartResult(bool isSuccess, BattleRuntimeFailureReason failureReason, CombatRuntimeFailureReason combatFailureReason, NightDefenseFailureReason nightDefenseFailureReason)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        CombatFailureReason = combatFailureReason;
        NightDefenseFailureReason = nightDefenseFailureReason;
    }

    // 성공한 전투 런타임 시작 결과를 만듭니다.
    public static BattleRuntimeStartResult Success()
    {
        return new BattleRuntimeStartResult(true, BattleRuntimeFailureReason.None, CombatRuntimeFailureReason.None, NightDefenseFailureReason.None);
    }

    // 공통 실패 결과를 만듭니다.
    public static BattleRuntimeStartResult Fail(BattleRuntimeFailureReason failureReason)
    {
        return new BattleRuntimeStartResult(false, failureReason, CombatRuntimeFailureReason.None, NightDefenseFailureReason.None);
    }

    // 전투 슬롯 구성 실패 결과를 만듭니다.
    public static BattleRuntimeStartResult FailCombat(CombatRuntimeFailureReason failureReason)
    {
        return new BattleRuntimeStartResult(false, BattleRuntimeFailureReason.CombatSlotBuildFailed, failureReason, NightDefenseFailureReason.None);
    }

    // 웨이브 시작 실패 결과를 만듭니다.
    public static BattleRuntimeStartResult FailWave(NightDefenseFailureReason failureReason)
    {
        return new BattleRuntimeStartResult(false, BattleRuntimeFailureReason.WaveStartFailed, CombatRuntimeFailureReason.None, failureReason);
    }
}

// 전투 세션 런타임 한 Tick 결과입니다.
public readonly struct BattleRuntimeTickResult
{
    public BattleRuntimeState State { get; } // Tick 이후 전투 상태
    public int SpawnedEnemyCount { get; } // 이번 Tick에서 스폰된 적 수
    public int AttackCount { get; } // 이번 Tick에서 발생한 공격 횟수
    public int DefeatedEnemyCount { get; } // 이번 Tick에서 처치된 적 수
    public int CastleDamage { get; } // 이번 Tick에서 성채 공격으로 받은 피해량
    public int AliveEnemyCount { get; } // Tick 이후 남은 적 수

    // Tick 요약 값을 보관합니다.
    public BattleRuntimeTickResult(BattleRuntimeState state, int spawnedEnemyCount, int attackCount, int defeatedEnemyCount, int castleDamage, int aliveEnemyCount)
    {
        State = state;
        SpawnedEnemyCount = spawnedEnemyCount;
        AttackCount = attackCount;
        DefeatedEnemyCount = defeatedEnemyCount;
        CastleDamage = castleDamage;
        AliveEnemyCount = aliveEnemyCount;
    }
}

// 순수 전투 런타임이 만든 결과를 Unity 표시 계층으로 전달하는 계약입니다.
// 표시 계층은 이 계약을 통해 프리팹 생성, 위치 갱신, 피해 연출, 풀 반납을 처리합니다.
public interface IBattleCombatViewSink
{
    // 새 전투 세션을 시작할 때 기존 표시물을 정리합니다.
    void ClearBattleViews();

    // 스폰된 적 런타임 상태에 대응하는 Unity 표시물을 만듭니다.
    void SpawnEnemyView(CombatEnemyRuntimeState enemy, NightDefenseSpawnRequest request);

    // 살아 있는 적들의 위치, 체력바, 상태 표시를 갱신합니다.
    void SyncEnemyViews(IReadOnlyList<CombatEnemyRuntimeState> enemies);

    // 공격 피해 표시나 피격 이펙트를 요청합니다.
    void ShowDamageResults(IReadOnlyList<CombatDamageResult> damageResults);

    // 적 표시물을 제거하거나 풀로 반납합니다.
    void DespawnEnemyView(CombatEnemyRuntimeState enemy, CombatEnemyDespawnReason reason);
}
