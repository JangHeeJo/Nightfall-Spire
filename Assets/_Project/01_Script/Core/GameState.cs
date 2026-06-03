// 게임 전체 상태를 나타내는 enum입니다.
// 씬 전환, UI 표시, 입력 가능 여부, 전투 흐름을 이 상태 기준으로 관리합니다.
public enum GameState
{
    None, // 아직 상태가 정해지지 않은 기본 상태

    Boot, // 게임이 처음 실행되어 초기화 중인 상태

    LobbyLoading, // 로비 씬을 불러오는 중인 상태
    Lobby, // 로비에서 성장, 상점, 스테이지 선택 등을 하는 상태

    BattleLoading, // 전투 씬을 불러오는 중인 상태
    BattleReady, // 전투 시작 전 영웅 배치와 데이터 세팅을 준비하는 상태
    BattlePlaying, // 실제 전투가 진행 중인 상태

    CardSelect, // 전투 중 카드 선택 팝업이 떠 있는 상태
    BattleResult, // 전투 결과와 보상을 보여주는 상태

    AppBackground // 앱이 백그라운드로 내려간 상태
}