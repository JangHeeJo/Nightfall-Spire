using R3;

// 밤 방어전 결과입니다.
// 결과 보상, 다음 낮 진입, 재도전 정책은 이 값을 기준으로 결정합니다.
public enum DefenseOutcome
{
    None, // 아직 결과가 정해지지 않은 상태
    Victory, // 밤 방어 성공
    Defeat, // 방어 실패
    Abandoned // 사용자가 중간에 포기하거나 세션이 중단된 상태
}

// 밤 방어전 한 판의 런타임 진행 상태를 관리합니다.
// 이 모델은 일반 Battle이 아니라 웨이브, 보스, 드래프트 선택이 섞인 Defense Session을 표현합니다.
public sealed class NightDefenseProgress
{
    public ReactiveProperty<bool> IsDefenseActive { get; } = new(false); // 현재 밤 방어전 진행 중 여부
    public ReactiveProperty<int> CurrentDefenseSessionId { get; } = new(0); // 현재 진행 중인 방어 세션 ID
    public ReactiveProperty<int> CurrentWaveIndex { get; } = new(0); // 현재 웨이브 번호
    public ReactiveProperty<bool> IsBossWave { get; } = new(false); // 현재 웨이브가 보스 웨이브인지 여부
    public ReactiveProperty<float> ElapsedSeconds { get; } = new(0f); // 방어 세션 경과 시간
    public ReactiveProperty<DefenseOutcome> LastOutcome { get; } = new(DefenseOutcome.None); // 마지막 방어 결과

    // 밤 방어 세션을 시작합니다.
    public void BeginDefenseSession(int defenseSessionId)
    {
        if (defenseSessionId <= 0)
            return;

        CurrentDefenseSessionId.Value = defenseSessionId;
        CurrentWaveIndex.Value = 0;
        IsBossWave.Value = false;
        ElapsedSeconds.Value = 0f;
        LastOutcome.Value = DefenseOutcome.None;
        IsDefenseActive.Value = true;
    }

    // 다음 웨이브로 진행합니다.
    public void AdvanceWave(bool isBossWave)
    {
        if (!IsDefenseActive.Value)
            return;

        CurrentWaveIndex.Value += 1;
        IsBossWave.Value = isBossWave;
    }

    // 방어 세션 경과 시간을 갱신합니다.
    public void SetElapsedSeconds(float elapsedSeconds)
    {
        if (elapsedSeconds < 0f)
            return;

        ElapsedSeconds.Value = elapsedSeconds;
    }

    // 밤 방어 세션을 종료합니다.
    public void EndDefenseSession(DefenseOutcome outcome)
    {
        IsDefenseActive.Value = false;
        IsBossWave.Value = false;
        LastOutcome.Value = outcome;
    }
}
