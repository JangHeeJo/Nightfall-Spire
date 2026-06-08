using Cysharp.Threading.Tasks;

// 열린 팝업 하나의 런타임 핸들입니다.
// 호출자는 이 핸들로 팝업 인스턴스와 닫힘 결과를 추적할 수 있습니다.
public sealed class PopupHandle
{
    private readonly UniTaskCompletionSource<PopupResult> completionSource = new(); // 닫힘 결과 대기용

    public int Id { get; } // 열린 순서를 구분하는 내부 ID
    public string Key { get; } // 중복 정책에 사용하는 요청 키
    public BasePopup Popup { get; } // 실제 생성된 팝업 인스턴스
    public PopupPriority Priority { get; } // 요청 중요도
    public bool UseDim { get; } // 딤 레이어 사용 여부
    public UniTask<PopupResult> ResultTask => completionSource.Task;

    // PopupManager가 팝업을 생성한 뒤 핸들을 만듭니다.
    internal PopupHandle(int id, string key, BasePopup popup, PopupPriority priority, bool useDim)
    {
        Id = id;
        Key = key;
        Popup = popup;
        Priority = priority;
        UseDim = useDim;
    }

    // 팝업이 닫힐 때 대기 중인 호출자에게 결과를 전달합니다.
    internal void Complete(PopupResult result)
    {
        completionSource.TrySetResult(result);
    }
}
