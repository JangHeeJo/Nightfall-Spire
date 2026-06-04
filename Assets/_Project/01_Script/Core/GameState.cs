// 게임 전체 상태를 나타내는 enum입니다.
// 낮 준비와 밤 방어전 루프가 게임의 중심이므로 상태 이름도 그 흐름에 맞춥니다.
public enum GameState
{
    None, // 아직 상태가 정해지지 않은 기본 상태

    Boot, // 게임이 처음 켜져 초기화 중인 상태

    DayPreparationLoading, // 낮 준비 씬을 불러오는 중인 상태
    DayPreparation, // 낮에 스파이어, 성채, 전투 슬롯, 채굴, 제작을 정비하는 상태

    NightDefenseLoading, // 밤 방어전 씬을 불러오는 중인 상태
    NightDefenseReady, // 밤 방어전 시작 전 배치와 데이터를 준비하는 상태
    NightDefensePlaying, // 웨이브 방어가 실제로 진행 중인 상태

    DraftSelection, // 전투 중 로그라이트 카드 1-of-3 선택지가 떠 있는 상태
    NightDefenseResult, // 밤 방어 결과와 보상을 보여주는 상태

    AppBackground // 앱이 백그라운드로 내려간 상태
}
