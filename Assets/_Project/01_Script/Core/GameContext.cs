// 게임 전체에서 공유하는 현재 데이터 묶음입니다.
// Popup, HUD, Lobby, Battle 쪽 시스템들은 이 Context를 통해 현재 진행 상태를 참조합니다.
public sealed class GameContext
{
    public SaveData SaveData { get; } // 실제 파일로 저장되는 원본 데이터

    public GameProgress GameProgress { get; } // 게임 전체 진행 상태
    public CurrencyProgress CurrencyProgress { get; } // 재화 진행 상태

    public GameContext(SaveData saveData)
    {
        SaveData = saveData;

        // SaveData를 기반으로 런타임 진행 모델들을 생성합니다.
        GameProgress = new GameProgress(saveData);
        CurrencyProgress = new CurrencyProgress(saveData);
    }
}