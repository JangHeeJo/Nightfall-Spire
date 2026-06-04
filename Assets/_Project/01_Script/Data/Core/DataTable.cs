using System.Collections.Generic;

// 특정 Row 타입 하나를 담는 읽기 전용 데이터 테이블입니다.
// 로딩 완료 후에는 Dictionary 기반 조회만 제공해서 런타임 로직이 TSV 원본을 몰라도 되게 합니다.
public sealed class DataTable<TRow> where TRow : ITableRow
{
    private readonly Dictionary<int, TRow> rowsById = new(); // Id별 Row 저장소
    private readonly List<TRow> rows = new(); // 순회가 필요할 때 사용할 Row 목록

    public IReadOnlyList<TRow> Rows => rows; // 전체 Row 목록
    public int Count => rows.Count; // 로드된 Row 수

    // Row를 테이블에 추가합니다.
    // 같은 Id가 두 번 들어오면 데이터 오류이므로 예외를 던집니다.
    public void Add(TRow row)
    {
        if (rowsById.ContainsKey(row.Id))
            throw new System.InvalidOperationException($"{typeof(TRow).Name} 중복 Id입니다. Id: {row.Id}");

        rowsById.Add(row.Id, row);
        rows.Add(row);
    }

    // Id로 Row를 조회합니다.
    // 반드시 존재해야 하는 데이터가 없으면 호출자가 바로 알 수 있게 예외를 던집니다.
    public TRow Get(int id)
    {
        if (!rowsById.TryGetValue(id, out TRow row))
            throw new KeyNotFoundException($"{typeof(TRow).Name}에서 Id를 찾을 수 없습니다. Id: {id}");

        return row;
    }

    // Id로 Row 조회를 시도합니다.
    // 선택 데이터처럼 없을 수 있는 참조는 이 메서드로 처리합니다.
    public bool TryGet(int id, out TRow row)
    {
        return rowsById.TryGetValue(id, out row);
    }
}
