using System;
using System.Collections.Generic;

// TSV 문자열을 DataTable Row로 변환하는 파서입니다.
// 첫 줄은 헤더, 빈 줄과 # 주석 줄은 무시합니다.
public static class TsvParser
{
    private static readonly char[] ColumnSeparator = { '\t' }; // TSV 컬럼 구분자
    private static readonly string[] LineSeparators = { "\r\n", "\n" }; // 줄바꿈 구분자

    // TSV 전체 문자열을 지정한 Row 타입의 DataTable로 변환합니다.
    public static DataTable<TRow> Parse<TRow>(string tsvText) where TRow : ITableRow, new()
    {
        return Parse<TRow>("UnknownTable", tsvText);
    }

    // 테이블 이름을 함께 받아 오류 메시지에 어느 테이블이 문제인지 포함합니다.
    public static DataTable<TRow> Parse<TRow>(string tableName, string tsvText) where TRow : ITableRow, new()
    {
        if (string.IsNullOrWhiteSpace(tsvText))
            throw new ArgumentException($"{tableName} TSV 내용이 비어 있습니다.");

        string[] lines = tsvText.Split(LineSeparators, StringSplitOptions.None);
        string[] headers = null;
        DataTable<TRow> table = new();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.TrimStart().StartsWith("#"))
                continue;

            if (headers == null)
            {
                headers = SplitColumns(line);
                continue;
            }

            TsvRow row = CreateRow(tableName, headers, SplitColumns(line), i + 1);
            TRow tableRow = new();
            tableRow.Load(row);
            table.Add(tableRow);
        }

        if (headers == null)
            throw new FormatException($"{tableName} TSV 헤더를 찾을 수 없습니다.");

        return table;
    }

    // 한 줄을 탭 기준 컬럼으로 분리합니다.
    private static string[] SplitColumns(string line)
    {
        return line.Split(ColumnSeparator, StringSplitOptions.None);
    }

    // 헤더와 값 배열을 컬럼명 Dictionary로 묶습니다.
    private static TsvRow CreateRow(string tableName, string[] headers, string[] columns, int lineNumber)
    {
        if (columns.Length > headers.Length)
            throw new FormatException($"{tableName} TSV {lineNumber}번째 줄의 컬럼 수가 헤더보다 많습니다.");

        Dictionary<string, string> values = new();

        for (int i = 0; i < headers.Length; i++)
        {
            string value = i < columns.Length ? columns[i] : string.Empty;
            values[headers[i]] = value;
        }

        return new TsvRow(tableName, lineNumber, values);
    }
}
