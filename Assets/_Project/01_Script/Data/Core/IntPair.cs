// id:amount 또는 tag:count 같은 쌍 데이터를 표현합니다.
// TSV에서는 "20001:5|20002:3" 같은 형식을 사용합니다.
public readonly struct IntPair
{
    public readonly int Key; // 대상 ID 또는 조건 키
    public readonly int Value; // 수량, 요구 개수, 가중치 같은 값

    // key와 value를 직접 지정해 불변 쌍 데이터를 만듭니다.
    public IntPair(int key, int value)
    {
        Key = key;
        Value = value;
    }
}

// tag:count 같은 문자열 조건을 표현합니다.
// 카드 태그, 영웅 태그, 모듈 태그 조건에 사용합니다.
public readonly struct StringIntPair
{
    public readonly string Key; // 조건 태그
    public readonly int Value; // 필요한 개수 또는 단계

    // 문자열 키와 정수 값을 직접 지정해 불변 조건 데이터를 만듭니다.
    public StringIntPair(string key, int value)
    {
        Key = key;
        Value = value;
    }
}
