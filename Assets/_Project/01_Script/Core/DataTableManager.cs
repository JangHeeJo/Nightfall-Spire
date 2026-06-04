// 게임 밸런스 테이블을 로드하고 조회하는 매니저입니다.
// 현재는 구조만 준비하고, 실제 TSV/CSV 로드는 데이터 폴더가 정해진 뒤 추가합니다.
public sealed class DataTableManager
{
    public bool IsLoaded { get; private set; } // 테이블 로드 완료 여부

    // 테이블 로더가 붙기 전까지는 초기화 상태만 기록합니다.
    public void MarkLoadedForBootstrap()
    {
        IsLoaded = true;
    }
}
