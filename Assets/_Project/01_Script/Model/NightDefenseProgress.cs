using R3;

// 밤 방어전 한 판의 런타임 진행 상태를 관리합니다.
// 이 모델은 일반 Battle이 아니라 웨이브, 보스, 드래프트 선택이 섞인 Defense Session을 표현합니다.
public sealed class NightDefenseProgress
{
    public ReactiveProperty<bool> IsDefenseActive { get; } = new(false); // 현재 밤 방어전 진행 중 여부
    public ReactiveProperty<int> CurrentDefenseSessionId { get; } = new(0); // 현재 진행 중인 방어 세션 ID
    public ReactiveProperty<int> CurrentWaveIndex { get; } = new(0); // 현재 웨이브 번호
    public ReactiveProperty<bool> IsBossWave { get; } = new(false); // 현재 웨이브가 보스 웨이브인지 여부
    public ReactiveProperty<float> ElapsedSeconds { get; } = new(0f); // 방어 세션 경과 시간

    // 밤 방어 세션을 시작합니다.
    public void BeginDefenseSession(int defenseSessionId)
    {
        if (defenseSessionId <= 0)
            return;

        CurrentDefenseSessionId.Value = defenseSessionId;
        CurrentWaveIndex.Value = 0;
        IsBossWave.Value = false;
        ElapsedSeconds.Value = 0f;
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
    public void EndDefenseSession()
    {
        IsDefenseActive.Value = false;
        IsBossWave.Value = false;
    }
}
