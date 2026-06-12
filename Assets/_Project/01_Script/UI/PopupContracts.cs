using System;
using Cysharp.Threading.Tasks;

// 팝업 시스템에서 쓰는 요청, 정책, 결과 계약을 한곳에 모아둡니다.
// MonoBehaviour가 아닌 작은 enum/값 타입을 파일마다 쪼개지 않기 위해 이 파일에 함께 둡니다.

// 팝업 View가 직접 PopupManager를 알지 않고 닫기만 요청할 수 있게 하는 함수 계약입니다.
public delegate UniTask PopupCloseRequester(BasePopup popup, PopupCloseReason closeReason, object payload);

// 같은 팝업 요청이 들어왔을 때 PopupManager가 처리할 방식을 정의합니다.
public enum PopupOpenPolicy
{
    Stack, // 기존 팝업 위에 새 팝업을 쌓습니다.
    SingleInstance, // 같은 Key의 팝업이 이미 열려 있으면 기존 팝업을 재사용합니다.
    ReplaceTop, // 가장 위 팝업을 닫고 새 팝업을 엽니다.
    ReplaceAll // 현재 열린 모든 팝업을 닫고 새 팝업을 엽니다.
}

// 팝업이 들어갈 UI 층을 구분합니다.
// 하단 탭으로 여는 화면은 Content 슬롯에 하나만 유지하고, 상세/확인창은 Overlay로 그 위에 쌓습니다.
public enum PopupLayerSlot
{
    Content, // 히어로, 상점, 소환처럼 하단 탭에서 갈아끼우는 화면 팝업
    Overlay // 상세 정보, 확인창, 보상창처럼 현재 화면 위에 추가로 뜨는 팝업
}

// 여러 팝업 요청이 동시에 들어올 때 중요도를 표현합니다.
public enum PopupPriority
{
    Normal, // 일반 팝업 요청
    Important, // 보상이나 주요 확인처럼 우선순위가 높은 요청
    Critical // 에러, 결제, 데이터 손상처럼 반드시 먼저 처리해야 하는 요청
}

// 팝업이 닫힌 이유를 결과 처리 쪽에 전달합니다.
public enum PopupCloseReason
{
    None, // 아직 닫힘 이유가 정해지지 않은 상태
    Confirmed, // 사용자가 확인 또는 선택을 확정함
    Cancelled, // 사용자가 취소를 선택함
    Dismissed, // 배경 닫기나 닫기 버튼으로 단순 종료함
    Replaced, // 다른 팝업 요청에 의해 교체됨
    SceneChanged // 씬 전환으로 팝업 레이어가 해제됨
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
    public PopupLayerSlot LayerSlot { get; } // 팝업을 배치할 논리 슬롯
    public PopupPriority Priority { get; } // 중요도
    public bool UseDim { get; } // 이 팝업이 뒤쪽 UI를 어둡게 막아야 하는지 여부
    public Action<TPopup> ConfigureBeforeOpen { get; } // 팝업 생성 직후 열기 연출 전에 데이터를 주입하는 선택 콜백

    // 팝업 요청 값을 생성합니다.
    public PopupRequest(
        string key,
        TPopup prefab,
        PopupOpenPolicy openPolicy = PopupOpenPolicy.Stack,
        PopupLayerSlot layerSlot = PopupLayerSlot.Overlay,
        PopupPriority priority = PopupPriority.Normal,
        bool useDim = true,
        Action<TPopup> configureBeforeOpen = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("팝업 Key는 비어 있을 수 없습니다.", nameof(key));

        Key = key;
        Prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
        OpenPolicy = openPolicy;
        LayerSlot = layerSlot;
        Priority = priority;
        UseDim = useDim;
        ConfigureBeforeOpen = configureBeforeOpen;
    }
}

// PopupManager가 팝업별 Controller를 만들 때 넘겨주는 실행 환경입니다.
public readonly struct PopupControllerContext
{
    public GameContext GameContext { get; } // 현재 게임 진행 데이터와 도메인 서비스 묶음
    public PopupManager PopupManager { get; } // 다른 팝업 열기나 닫기가 필요한 Controller용 관리자

    // 팝업 Controller가 필요한 전역 의존성을 명시적으로 묶습니다.
    public PopupControllerContext(GameContext gameContext, PopupManager popupManager)
    {
        GameContext = gameContext;
        PopupManager = popupManager;
    }
}

// 팝업 인스턴스에 맞는 Controller를 만들어주는 조립 계약입니다.
// View가 Controller 타입을 직접 알지 않도록 PopupManager 바깥의 Factory가 이 책임을 가집니다.
public interface IPopupControllerFactory
{
    IDisposable CreateController(BasePopup popup, PopupControllerContext context);
}

// 열린 팝업 하나의 런타임 핸들입니다.
// 호출자는 이 핸들로 팝업 인스턴스와 닫힘 결과를 추적할 수 있습니다.
public sealed class PopupHandle
{
    private readonly UniTaskCompletionSource<PopupResult> completionSource = new(); // 닫힘 결과 대기용

    public int Id { get; } // 열린 순서를 구분하는 내부 ID
    public string Key { get; } // 중복 정책에 사용하는 요청 키
    public BasePopup Popup { get; } // 실제 생성된 팝업 인스턴스
    public PopupLayerSlot LayerSlot { get; } // 이 팝업이 차지한 논리 슬롯
    public PopupPriority Priority { get; } // 요청 중요도
    public bool UseDim { get; } // 딤 레이어 사용 여부
    public UniTask<PopupResult> ResultTask => completionSource.Task;
    internal IDisposable Controller { get; } // 팝업과 같은 생명주기를 갖는 Controller

    // PopupManager가 팝업을 생성한 뒤 핸들을 만듭니다.
    internal PopupHandle(int id, string key, BasePopup popup, PopupLayerSlot layerSlot, PopupPriority priority, bool useDim, IDisposable controller)
    {
        Id = id;
        Key = key;
        Popup = popup;
        LayerSlot = layerSlot;
        Priority = priority;
        UseDim = useDim;
        Controller = controller;
    }

    // 팝업이 닫힐 때 대기 중인 호출자에게 결과를 전달합니다.
    internal void Complete(PopupResult result)
    {
        completionSource.TrySetResult(result);
    }
}
