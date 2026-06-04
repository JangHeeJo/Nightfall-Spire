// 모든 데이터 테이블 Row가 공통으로 구현하는 인터페이스입니다.
// DataTable은 이 Id를 기준으로 행을 빠르게 조회합니다.
public interface ITableRow
{
    int Id { get; } // 테이블의 기본 키

    // TSV 한 줄을 파싱한 TsvRow에서 자기 필드 값을 읽어옵니다.
    void Load(TsvRow row);
}
