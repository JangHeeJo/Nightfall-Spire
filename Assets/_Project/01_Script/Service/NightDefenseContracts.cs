using System;
using System.Collections.Generic;

// 밤 방어 세션, 런타임, 스폰 흐름에서 공유하는 작은 요청/결과 계약입니다.
// WavePlan처럼 자체 규칙이 큰 타입은 별도 파일로 유지하고, 작은 값 타입만 이 파일에 모읍니다.
public enum DefenseOutcome
{
    None, // 아직 결과가 정해지지 않은 상태
    Victory, // 밤 방어 성공
    Defeat, // 방어 실패
    Abandoned // 사용자가 중간에 포기하거나 세션이 중단된 상태
}

public enum NightDefenseFailureReason
{
    None, // 실패 없음
    SessionNotFound, // 방어 세션 데이터를 찾지 못함
    RequiredFloorLocked, // 세션 입장에 필요한 성채 층이 잠겨 있음
    WaveGroupNotFound, // 세션에 연결된 웨이브 그룹을 찾지 못함
    SessionNotActive, // 밤 방어 세션이 진행 중이 아님
    WaveOutOfRange, // 다음 웨이브 번호가 웨이브 그룹 범위를 벗어남
    WaveRowsNotFound, // 해당 웨이브의 스폰 Row가 없음
    InvalidWaveData // 웨이브 Row를 유효한 스폰 계획으로 만들 수 없음
}

public enum NightDefenseCompletionFailureReason
{
    None, // 실패 없음
    SessionNotFound, // 종료하려는 방어 세션 데이터를 찾지 못함
    RewardServiceMissing, // 보상 계산 서비스가 연결되지 않음
    RewardBuildFailed // 보상 그룹 계산에 실패함
}

// 밤 방어 세션 시작 결과입니다.
public readonly struct NightDefenseStartResult
{
    public bool IsSuccess { get; } // 시작 성공 여부
    public NightDefenseFailureReason FailureReason { get; } // 실패 이유
    public DefenseSessionDataRow SessionRow { get; } // 시작한 세션 Row
    public WaveGroupDataRow WaveGroupRow { get; } // 세션의 웨이브 그룹 Row

    // 성공 여부, 실패 이유, 세션 Row, 웨이브 그룹 Row를 보관합니다.
    private NightDefenseStartResult(bool isSuccess, NightDefenseFailureReason failureReason, DefenseSessionDataRow sessionRow, WaveGroupDataRow waveGroupRow)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        SessionRow = sessionRow;
        WaveGroupRow = waveGroupRow;
    }

    // 성공한 밤 방어 세션 시작 결과를 만듭니다.
    public static NightDefenseStartResult Success(DefenseSessionDataRow sessionRow, WaveGroupDataRow waveGroupRow)
    {
        return new NightDefenseStartResult(true, NightDefenseFailureReason.None, sessionRow, waveGroupRow);
    }

    // 실패한 밤 방어 세션 시작 결과를 만듭니다.
    public static NightDefenseStartResult Fail(NightDefenseFailureReason failureReason)
    {
        return new NightDefenseStartResult(false, failureReason, null, null);
    }
}

// 밤 방어전 종료 보상 계산 결과입니다.
public readonly struct NightDefenseCompletionRewardResult
{
    public bool IsSuccess { get; } // 종료 보상 계산 성공 여부
    public NightDefenseCompletionFailureReason FailureReason { get; } // 종료 보상 계산 실패 이유
    public RewardFailureReason RewardFailureReason { get; } // 보상 서비스 내부 실패 이유
    public long Gold { get; } // 수령 대기 상태에 넣을 골드
    public long Gem { get; } // 수령 대기 상태에 넣을 젬

    // 보상 계산 성공 여부와 지급 재화 값을 보관합니다.
    private NightDefenseCompletionRewardResult(bool isSuccess, NightDefenseCompletionFailureReason failureReason, RewardFailureReason rewardFailureReason, long gold, long gem)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        RewardFailureReason = rewardFailureReason;
        Gold = gold;
        Gem = gem;
    }

    // 성공한 종료 보상 계산 결과를 만듭니다.
    public static NightDefenseCompletionRewardResult Success(long gold, long gem)
    {
        return new NightDefenseCompletionRewardResult(true, NightDefenseCompletionFailureReason.None, RewardFailureReason.None, gold, gem);
    }

    // 실패한 종료 보상 계산 결과를 만듭니다.
    public static NightDefenseCompletionRewardResult Fail(NightDefenseCompletionFailureReason failureReason, RewardFailureReason rewardFailureReason = RewardFailureReason.None)
    {
        return new NightDefenseCompletionRewardResult(false, failureReason, rewardFailureReason, 0, 0);
    }
}

// 웨이브 진행 결과입니다.
public readonly struct NightDefenseWaveResult
{
    public bool IsSuccess { get; } // 웨이브 진행 성공 여부
    public NightDefenseFailureReason FailureReason { get; } // 실패 이유
    public int WaveIndex { get; } // 진행된 웨이브 번호
    public IReadOnlyList<WaveDataRow> WaveRows { get; } // 해당 웨이브 스폰 Row
    public NightDefenseWavePlan WavePlan { get; } // 스폰러가 사용할 시간표
    public bool IsBossWave { get; } // 보스 웨이브 여부
    public bool DraftAfterWave { get; } // 웨이브 종료 후 드래프트 여부
    public bool IsLastWave { get; } // 마지막 웨이브 여부

    // 웨이브 진행 결과에 필요한 모든 상태 값을 보관합니다.
    private NightDefenseWaveResult(bool isSuccess, NightDefenseFailureReason failureReason, int waveIndex, IReadOnlyList<WaveDataRow> waveRows, NightDefenseWavePlan wavePlan, bool isBossWave, bool draftAfterWave, bool isLastWave)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        WaveIndex = waveIndex;
        WaveRows = waveRows;
        WavePlan = wavePlan;
        IsBossWave = isBossWave;
        DraftAfterWave = draftAfterWave;
        IsLastWave = isLastWave;
    }

    // 성공한 웨이브 진행 결과를 만듭니다.
    public static NightDefenseWaveResult Success(int waveIndex, IReadOnlyList<WaveDataRow> waveRows, NightDefenseWavePlan wavePlan, bool isBossWave, bool draftAfterWave, bool isLastWave)
    {
        return new NightDefenseWaveResult(true, NightDefenseFailureReason.None, waveIndex, waveRows, wavePlan, isBossWave, draftAfterWave, isLastWave);
    }

    // 실패한 웨이브 진행 결과를 만듭니다.
    public static NightDefenseWaveResult Fail(NightDefenseFailureReason failureReason)
    {
        return new NightDefenseWaveResult(false, failureReason, 0, Array.Empty<WaveDataRow>(), NightDefenseWavePlan.Empty(), false, false, false);
    }
}

// 밤 방어 런타임에서 새 웨이브 시작을 시도한 결과입니다.
public readonly struct NightDefenseRuntimeStartResult
{
    public bool IsSuccess { get; } // 웨이브 시작 성공 여부
    public NightDefenseFailureReason FailureReason { get; } // 세션 서비스 실패 이유
    public NightDefenseWavePlan WavePlan { get; } // 시작된 웨이브 스폰 계획

    // 런타임 웨이브 시작 성공 여부와 시작된 웨이브 계획을 보관합니다.
    private NightDefenseRuntimeStartResult(bool isSuccess, NightDefenseFailureReason failureReason, NightDefenseWavePlan wavePlan)
    {
        IsSuccess = isSuccess;
        FailureReason = failureReason;
        WavePlan = wavePlan;
    }

    // 성공한 웨이브 시작 결과를 만듭니다.
    public static NightDefenseRuntimeStartResult Success(NightDefenseWavePlan wavePlan)
    {
        return new NightDefenseRuntimeStartResult(true, NightDefenseFailureReason.None, wavePlan);
    }

    // 실패한 웨이브 시작 결과를 만듭니다.
    public static NightDefenseRuntimeStartResult Fail(NightDefenseFailureReason failureReason)
    {
        return new NightDefenseRuntimeStartResult(false, failureReason, NightDefenseWavePlan.Empty());
    }
}

// 밤 방어 런타임이 한 번의 Tick에서 처리한 결과입니다.
public readonly struct NightDefenseRuntimeTickResult
{
    public int SpawnedCount { get; } // 이번 Tick에서 발생한 스폰 요청 수
    public bool IsWaveSpawnComplete { get; } // 현재 웨이브의 스폰 요청이 모두 발생했는지 여부
    public bool ShouldOpenDraft { get; } // 웨이브 스폰 완료 후 드래프트를 열어야 하는지 여부
    public bool IsLastWave { get; } // 현재 웨이브가 마지막 웨이브인지 여부
    public float SessionElapsedSec { get; } // 세션 전체 경과 시간
    public float WaveElapsedSec { get; } // 현재 웨이브 경과 시간

    // Tick 결과 값을 만듭니다.
    public NightDefenseRuntimeTickResult(int spawnedCount, bool isWaveSpawnComplete, bool shouldOpenDraft, bool isLastWave, float sessionElapsedSec, float waveElapsedSec)
    {
        SpawnedCount = spawnedCount;
        IsWaveSpawnComplete = isWaveSpawnComplete;
        ShouldOpenDraft = shouldOpenDraft;
        IsLastWave = isLastWave;
        SessionElapsedSec = sessionElapsedSec;
        WaveElapsedSec = waveElapsedSec;
    }
}

// 스폰 컨트롤러가 한 번의 Tick에서 처리한 결과입니다.
public readonly struct NightDefenseSpawnTickResult
{
    public int SpawnedCount { get; } // 이번 Tick에서 발생한 스폰 요청 수
    public bool IsWaveSpawnComplete { get; } // 현재 웨이브의 모든 스폰 요청을 발생시켰는지 여부
    public float WaveElapsedSec { get; } // 현재 웨이브 기준 경과 시간

    // Tick 결과 값을 만듭니다.
    public NightDefenseSpawnTickResult(int spawnedCount, bool isWaveSpawnComplete, float waveElapsedSec)
    {
        SpawnedCount = spawnedCount;
        IsWaveSpawnComplete = isWaveSpawnComplete;
        WaveElapsedSec = waveElapsedSec;
    }
}

// 한 마리의 적을 언제 어느 라인에 만들지 나타내는 값입니다.
public readonly struct NightDefenseSpawnEvent
{
    public int WaveRowId { get; } // 이 스폰 이벤트를 만든 WaveData Row ID
    public int EnemyId { get; } // 생성할 적 ID
    public int LaneId { get; } // 생성할 라인 ID
    public int SpawnOrder { get; } // 웨이브 안에서의 생성 순서
    public float SpawnTimeSec { get; } // 웨이브 시작 후 생성 시간

    // 스폰러가 바로 사용할 수 있는 단위 이벤트 값을 만듭니다.
    public NightDefenseSpawnEvent(int waveRowId, int enemyId, int laneId, int spawnOrder, float spawnTimeSec)
    {
        WaveRowId = waveRowId;
        EnemyId = enemyId;
        LaneId = laneId;
        SpawnOrder = spawnOrder;
        SpawnTimeSec = spawnTimeSec;
    }
}

// 전투 씬 스폰러에 전달할 적 생성 요청입니다.
public readonly struct NightDefenseSpawnRequest
{
    public int WaveIndex { get; } // 요청이 발생한 웨이브 번호
    public int WaveRowId { get; } // 요청의 원본 WaveData Row ID
    public int EnemyId { get; } // 생성할 적 ID
    public int LaneId { get; } // 생성할 라인 ID
    public int SpawnOrder { get; } // 웨이브 안에서의 생성 순서
    public float SpawnTimeSec { get; } // 웨이브 시작 기준 스폰 예정 시간
    public float RuntimeElapsedSec { get; } // 실제 런타임에서 요청이 발생한 시간

    // 스폰 시간표 이벤트를 실제 생성 요청으로 변환합니다.
    public NightDefenseSpawnRequest(int waveIndex, NightDefenseSpawnEvent spawnEvent, float runtimeElapsedSec)
    {
        WaveIndex = waveIndex;
        WaveRowId = spawnEvent.WaveRowId;
        EnemyId = spawnEvent.EnemyId;
        LaneId = spawnEvent.LaneId;
        SpawnOrder = spawnEvent.SpawnOrder;
        SpawnTimeSec = spawnEvent.SpawnTimeSec;
        RuntimeElapsedSec = runtimeElapsedSec;
    }
}
