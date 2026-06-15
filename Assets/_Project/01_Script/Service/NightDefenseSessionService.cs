using System;
using System.Collections.Generic;
using UnityEngine;

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
        {
            if (!TryRepairMissingCurrentSession(sessionId, out sessionRow))
                return NightDefenseStartResult.Fail(NightDefenseFailureReason.SessionNotFound);
        }

        if (context.DayProgress.CitadelFloorCount.Value < sessionRow.RequiredFloorId)
            return NightDefenseStartResult.Fail(NightDefenseFailureReason.RequiredFloorLocked);

        if (!dataSource.TryGetWaveGroup(sessionRow.WaveGroupId, out WaveGroupDataRow waveGroupRow))
            return NightDefenseStartResult.Fail(NightDefenseFailureReason.WaveGroupNotFound);

        context.DraftProgress.ResetForNewDefenseSession();
        context.NightDefenseProgress.BeginDefenseSession(sessionRow.SessionId);

        return NightDefenseStartResult.Success(sessionRow, waveGroupRow);
    }

    // 예전 저장 데이터가 현재 테이블에 없는 세션 ID를 들고 있으면 첫 세션으로 되돌립니다.
    private bool TryRepairMissingCurrentSession(int missingSessionId, out DefenseSessionDataRow sessionRow)
    {
        sessionRow = null;

        if (!dataSource.TryGetFirstDefenseSession(out DefenseSessionDataRow firstSessionRow) || firstSessionRow == null)
            return false;

        context.GameProgress.SetCurrentDefenseSession(firstSessionRow.SessionId);
        sessionRow = firstSessionRow;
        Debug.LogWarning($"[NightDefenseSessionService] 저장된 방어 세션 {missingSessionId}를 테이블에서 찾지 못해 첫 세션 {firstSessionRow.SessionId}로 보정했습니다.");
        return true;
    }

    // 현재 방어 세션의 전투 타임라인을 하나의 스폰 계획으로 만들고 시작합니다.
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

        if (nextWaveIndex > 1)
            return NightDefenseWaveResult.Fail(NightDefenseFailureReason.WaveOutOfRange);

        IReadOnlyList<WaveDataRow> waveRows = GetBattleTimelineRows(sessionRow.WaveGroupId, waveGroupRow.MaxWaveIndex);

        if (waveRows.Count == 0)
            return NightDefenseWaveResult.Fail(NightDefenseFailureReason.WaveRowsNotFound);

        bool isBossWave = HasBossWaveRows(waveRows);
        bool isLastWave = true;

        if (!wavePlanBuilder.TryBuild(nextWaveIndex, waveRows, isBossWave, isLastWave, out NightDefenseWavePlan wavePlan))
            return NightDefenseWaveResult.Fail(NightDefenseFailureReason.InvalidWaveData);

        context.NightDefenseProgress.AdvanceWave(isBossWave);

        return NightDefenseWaveResult.Success(nextWaveIndex, waveRows, wavePlan, isBossWave, isLastWave);
    }

    // WaveGroupData.MaxWaveIndex까지의 모든 WaveData Row를 모아 하나의 2분 전투 타임라인으로 사용합니다.
    private IReadOnlyList<WaveDataRow> GetBattleTimelineRows(int waveGroupId, int maxWaveIndex)
    {
        List<WaveDataRow> rows = new List<WaveDataRow>();

        for (int waveIndex = 1; waveIndex <= maxWaveIndex; waveIndex++)
        {
            IReadOnlyList<WaveDataRow> waveRows = dataSource.GetWaveRows(waveGroupId, waveIndex);

            for (int rowIndex = 0; rowIndex < waveRows.Count; rowIndex++)
            {
                rows.Add(waveRows[rowIndex]);
            }
        }

        return rows;
    }

    // 전투 타임라인 안에 보스 스폰 Row가 포함되어 있는지 확인합니다.
    private static bool HasBossWaveRows(IReadOnlyList<WaveDataRow> waveRows)
    {
        for (int i = 0; i < waveRows.Count; i++)
        {
            if (waveRows[i].IsBossWave)
                return true;
        }

        return false;
    }
}
