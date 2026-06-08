// NightDefenseSpawnController가 발생시킨 스폰 요청을 받는 계약입니다.
public interface INightDefenseSpawnSink
{
    // 실제 적 프리팹 생성이나 테스트 기록은 이 메서드 뒤에서 처리합니다.
    void Spawn(NightDefenseSpawnRequest request);
}
