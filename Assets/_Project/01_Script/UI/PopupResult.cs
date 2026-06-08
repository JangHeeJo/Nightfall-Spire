// 팝업이 닫힌 뒤 호출자에게 돌려주는 결과 값입니다.
public readonly struct PopupResult
{
    public PopupCloseReason CloseReason { get; } // 팝업이 닫힌 이유
    public object Payload { get; } // 선택 카드 ID나 보상 정보 같은 선택 결과

    public bool IsConfirmed => CloseReason == PopupCloseReason.Confirmed;
    public bool IsCancelled => CloseReason == PopupCloseReason.Cancelled;

    // 닫힘 이유와 선택 결과를 함께 보관합니다.
    public PopupResult(PopupCloseReason closeReason, object payload = null)
    {
        CloseReason = closeReason;
        Payload = payload;
    }
}
