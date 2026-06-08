using System;

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
