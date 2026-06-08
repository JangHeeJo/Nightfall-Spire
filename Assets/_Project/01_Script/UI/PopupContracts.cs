using System;
using Cysharp.Threading.Tasks;

// 팝업 시스템에서 쓰는 요청, 정책, 결과 계약을 한곳에 모아둡니다.
// MonoBehaviour가 아닌 작은 enum/값 타입을 파일마다 쪼개지 않기 위해 이 파일에 함께 둡니다.

// 같은 팝업 요청이 들어왔을 때 PopupManager가 처리할 방식을 정의합니다.
public enum PopupOpenPolicy
{
    Stack, // 기존 팝업 위에 새 팝업을 쌓습니다.
    SingleInstance, // 같은 Key의 팝업이 이미 열려 있으면 기존 팝업을 재사용합니다.
    ReplaceTop, // 가장 위 팝업을 닫고 새 팝업을 엽니다.
    ReplaceAll // 현재 열린 모든 팝업을 닫고 새 팝업을 엽니다.
}

// 여러 팝업 요청이 동시에 들어올 때 중요도를 표현합니다.
public enum PopupPriority
{
    Normal,
    Important,
    Critical
}

// 팝업이 닫힌 이유를 결과 처리 쪽에 전달합니다.
public enum PopupCloseReason
{
    None,
    Confirmed,
    Cancelled,
    Dismissed,
    Replaced,
    SceneChanged
}

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

// PopupManager에 전달하는 팝업 열기 요청입니다.
// 호출자는 프리팹, 중복 처리 정책, 딤 사용 여부를 한 번에 명시합니다.
public sealed class PopupRequest<TPopup> where TPopup : BasePopup
{
    public string Key { get; } // 중복 팝업 판단에 쓰는 요청 키
    public TPopup Prefab { get; } // 생성할 팝업 프리팹
    public PopupOpenPolicy OpenPolicy { get; } // 같은 팝업이 있을 때 처리 방식
    public PopupPriority Priority { get; } // 중요도
    public bool UseDim { get; } // 이 팝업이 뒤쪽 UI를 어둡게 막아야 하는지 여부

    // 팝업 요청 값을 생성합니다.
    public PopupRequest(
        string key,
        TPopup prefab,
        PopupOpenPolicy openPolicy = PopupOpenPolicy.Stack,
        PopupPriority priority = PopupPriority.Normal,
        bool useDim = true)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("팝업 Key는 비어 있을 수 없습니다.", nameof(key));

        Key = key;
        Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
        OpenPolicy = openPolicy;
        Priority = priority;
        UseDim = useDim;
    }
}

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
