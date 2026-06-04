using System;
using System.Collections.Generic;
using System.Globalization;

// TSV 한 줄을 컬럼명 기준으로 읽게 해주는 헬퍼입니다.
// Row 클래스는 문자열 인덱스를 직접 다루지 않고 이 클래스를 통해 타입 변환을 수행합니다.
public sealed class TsvRow
{
    private static readonly char[] ListSeparator = { '|' }; // 리스트 컬럼 구분자
    private static readonly char[] PairSeparator = { ':' }; // pair 컬럼 구분자

    private readonly Dictionary<string, string> values; // 컬럼명별 원본 문자열 값
    private readonly string tableName; // 오류 메시지에 표시할 테이블 이름
    private readonly int lineNumber; // 오류 메시지에 표시할 원본 줄 번호

    // 파서가 만든 컬럼 Dictionary를 받아 Row 조회 객체를 만듭니다.
    public TsvRow(Dictionary<string, string> values)
        : this("UnknownTable", 0, values)
    {
    }

    // 파서가 만든 컬럼 Dictionary와 위치 정보를 받아 Row 조회 객체를 만듭니다.
    public TsvRow(string tableName, int lineNumber, Dictionary<string, string> values)
    {
        this.tableName = tableName;
        this.lineNumber = lineNumber;
        this.values = values;
    }

    // 문자열 컬럼 값을 가져옵니다.
    public string GetString(string columnName)
    {
        if (!values.TryGetValue(columnName, out string value))
            throw new KeyNotFoundException($"{tableName} {lineNumber}번째 줄에서 컬럼을 찾을 수 없습니다. 컬럼: {columnName}");

        return value;
    }

    // 정수 컬럼 값을 가져옵니다.
    public int GetInt(string columnName)
    {
        string value = GetString(columnName);
        return string.IsNullOrWhiteSpace(value) ? 0 : int.Parse(value, CultureInfo.InvariantCulture);
    }

    // long 컬럼 값을 가져옵니다.
    public long GetLong(string columnName)
    {
        string value = GetString(columnName);
        return string.IsNullOrWhiteSpace(value) ? 0 : long.Parse(value, CultureInfo.InvariantCulture);
    }

    // float 컬럼 값을 가져옵니다.
    public float GetFloat(string columnName)
    {
        string value = GetString(columnName);
        return string.IsNullOrWhiteSpace(value) ? 0f : float.Parse(value, CultureInfo.InvariantCulture);
    }

    // TRUE/FALSE 또는 true/false 컬럼 값을 bool로 가져옵니다.
    public bool GetBool(string columnName)
    {
        string value = GetString(columnName);
        return !string.IsNullOrWhiteSpace(value) && bool.Parse(value);
    }

    // 문자열 enum 컬럼 값을 enum으로 변환합니다.
    public TEnum GetEnum<TEnum>(string columnName) where TEnum : struct
    {
        string value = GetString(columnName);

        if (string.IsNullOrWhiteSpace(value))
            return default;

        if (Enum.TryParse(value, true, out TEnum result))
            return result;

        throw new FormatException($"{tableName} {lineNumber}번째 줄의 enum 값이 잘못되었습니다. 컬럼: {columnName}, 값: {value}, 타입: {typeof(TEnum).Name}");
    }

    // | 로 구분된 enum 리스트를 가져옵니다.
    public List<TEnum> GetEnumList<TEnum>(string columnName) where TEnum : struct
    {
        string value = GetString(columnName);
        List<TEnum> result = new();

        if (string.IsNullOrWhiteSpace(value))
            return result;

        string[] parts = value.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();

            if (Enum.TryParse(part, true, out TEnum enumValue))
            {
                result.Add(enumValue);
                continue;
            }

            throw new FormatException($"{tableName} {lineNumber}번째 줄의 enum 리스트 값이 잘못되었습니다. 컬럼: {columnName}, 값: {part}, 타입: {typeof(TEnum).Name}");
        }

        return result;
    }

    // | 로 구분된 문자열 리스트를 가져옵니다.
    public List<string> GetStringList(string columnName)
    {
        string value = GetString(columnName);
        List<string> result = new();

        if (string.IsNullOrWhiteSpace(value))
            return result;

        string[] parts = value.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
            result.Add(parts[i].Trim());

        return result;
    }

    // | 로 구분된 정수 리스트를 가져옵니다.
    public List<int> GetIntList(string columnName)
    {
        string value = GetString(columnName);
        List<int> result = new();

        if (string.IsNullOrWhiteSpace(value))
            return result;

        string[] parts = value.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
            result.Add(int.Parse(parts[i].Trim(), CultureInfo.InvariantCulture));

        return result;
    }

    // "id:amount|id:amount" 형식의 정수 pair 리스트를 가져옵니다.
    public List<IntPair> GetIntPairList(string columnName)
    {
        string value = GetString(columnName);
        List<IntPair> result = new();

        if (string.IsNullOrWhiteSpace(value))
            return result;

        string[] parts = value.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            string[] pair = parts[i].Split(PairSeparator, StringSplitOptions.RemoveEmptyEntries);

            if (pair.Length != 2)
                throw new FormatException($"{columnName} pair 형식이 잘못되었습니다. 값: {parts[i]}");

            int key = int.Parse(pair[0].Trim(), CultureInfo.InvariantCulture);
            int pairValue = int.Parse(pair[1].Trim(), CultureInfo.InvariantCulture);
            result.Add(new IntPair(key, pairValue));
        }

        return result;
    }

    // "tag:count|tag:count" 형식의 문자열 조건 리스트를 가져옵니다.
    public List<StringIntPair> GetStringIntPairList(string columnName)
    {
        string value = GetString(columnName);
        List<StringIntPair> result = new();

        if (string.IsNullOrWhiteSpace(value))
            return result;

        string[] parts = value.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            string[] pair = parts[i].Split(PairSeparator, StringSplitOptions.RemoveEmptyEntries);

            if (pair.Length != 2)
                throw new FormatException($"{columnName} 조건 형식이 잘못되었습니다. 값: {parts[i]}");

            string key = pair[0].Trim();
            int pairValue = int.Parse(pair[1].Trim(), CultureInfo.InvariantCulture);
            result.Add(new StringIntPair(key, pairValue));
        }

        return result;
    }
}
