using System;
using System.Collections.Generic;

// 밤 방어 세션 시작과 웨이브 진행 규칙을 담당합니다.
public sealed class NightDefenseSessionService
{
    private readonly INightDefenseDataSource dataSource; // 밤 방어 테이블 조회 계약
    private readonly GameContext context; // 현재 진행 모델 묶음
    private readonly NightDefenseWavePlanBuilder wavePlanBuilder; // 웨이브 Row를 스폰 시간표로 바꾸는 빌더

    // 세션 규칙에 필요한 데이터와 진행 모델을 받습니다.
    public NightDefenseSessionService(INightDefenseDataSource dataSource, GameContext context, NightDefenseWavePlanBuilder wavePlanBuilder = null)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.wavePlanBuilder = wavePlanBuilder ?? new NightDefenseWavePlanBuilder();
    }

    // 현재 선택된 밤 방어 세션을 시작할 수 있는지 검증하고 시작합니다.
    public NightDefenseStartResult TryStartCurrentSession()
    {
        int sessionId = context.GameProgress.CurrentDefenseSessionId.Value;

        if (!dataSource.TryGetDefenseSession(sessionId, out DefenseSessionDataRow sessionRow))
            return NightDefenseStartResult.Fail(NightDefenseFailureReason.SessionNotFound);

        if (context.DayProgress.CitadelFloorCount.Value < sessionRow.RequiredFloorId)
            return NightDefenseStartResult.Fail(NightDefenseFailureReason.RequiredFloorLocked);

        if (!dataSource.TryGetWaveGroup(sessionRow.WaveGroupId, out WaveGroupDataRow waveGroupRow))
            return NightDefenseStartResult.Fail(NightDefenseFailureReason.WaveGroupNotFound);

        context.DraftProgress.ResetForNewDefenseSession();
        context.NightDefenseProgress.BeginDefenseSession(sessionRow.SessionId);

        return NightDefenseStartResult.Success(sessionRow, waveGroupRow);
    }

    // 다음 웨이브로 진행하고 해당 웨이브의 스폰 Row를 반환합니다.
    public NightDefenseWaveResult TryAdvanceNextWave()
    {
        if (!context.NightDefenseProgress.IsDefenseActive.Value)
            return NightDefenseWaveResult.Fail(NightDefenseFailureReason.SessionNotActive);

        int sessionId = context.NightDefenseProgress.CurrentDefenseSessionId.Value;

        if (!dataSource.TryGetDefenseSession(sessionId, out DefenseSessionDataRow sessionRow))
            return NightDefenseWaveResult.Fail(NightDefenseFailureReason.SessionNotFound);

        if (!dataSource.TryGetWaveGroup(sessionRow.WaveGroupId, out WaveGroupDataRow waveGroupRow))
            return NightDefenseWaveResult.Fail(NightDefenseFailureReason.WaveGroupNotFound);

        int nextWaveIndex = context.NightDefenseProgress.CurrentWaveIndex.Value + 1;

        if (nextWaveIndex > waveGroupRow.MaxWaveIndex)
            return NightDefenseWaveResult.Fail(NightDefenseFailureReason.WaveOutOfRange);

        IReadOnlyList<WaveDataRow> waveRows = dataSource.GetWaveRows(sessionRow.WaveGroupId, nextWaveIndex);

        if (waveRows.Count == 0)
            return NightDefenseWaveResult.Fail(NightDefenseFailureReason.WaveRowsNotFound);

        bool isBossWave = waveGroupRow.BossWaveIndex > 0 && nextWaveIndex == waveGroupRow.BossWaveIndex;
        bool draftAfterWave = HasDraftAfterWave(waveRows);
        bool isLastWave = nextWaveIndex >= waveGroupRow.MaxWaveIndex;

        if (!wavePlanBuilder.TryBuild(nextWaveIndex, waveRows, isBossWave, draftAfterWave, isLastWave, out NightDefenseWavePlan wavePlan))
            return NightDefenseWaveResult.Fail(NightDefenseFailureReason.InvalidWaveData);

        context.NightDefenseProgress.AdvanceWave(isBossWave);

        return NightDefenseWaveResult.Success(nextWaveIndex, waveRows, wavePlan, isBossWave, draftAfterWave, isLastWave);
    }

    // 현재 웨이브 Row 중 드래프트 발생 플래그가 있는지 확인합니다.
    private static bool HasDraftAfterWave(IReadOnlyList<WaveDataRow> waveRows)
    {
        for (int i = 0; i < waveRows.Count; i++)
        {
            if (waveRows[i].DraftAfterWave)
                return true;
        }

        return false;
    }
}
