// 재화 HUD View가 표시할 완성된 문자열 상태입니다.
// View는 이 값을 그대로 화면에 반영하고, 숫자 포맷 규칙은 Controller가 결정합니다.
public readonly struct CurrencyHudViewState
{
    public string GoldText { get; } // 골드 표시 문자열
    public string GemText { get; } // 젬 표시 문자열

    // Controller가 계산한 화면 표시 문자열을 묶습니다.
    public CurrencyHudViewState(string goldText, string gemText)
    {
        GoldText = goldText;
        GemText = gemText;
    }
}
