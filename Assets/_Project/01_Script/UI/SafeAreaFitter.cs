using UnityEngine;

// 노치, 홈바, 둥근 모서리 때문에 실제로 터치 가능한 화면 영역이 줄어드는 기기를 보정합니다.
// Canvas 바로 아래의 SafeAreaRoot에 붙이고, 그 아래에 고정 UI를 배치해서 모든 화면이 같은 기준을 쓰게 합니다.
public sealed class SafeAreaFitter : MonoBehaviour
{
    [SerializeField] private RectTransform targetRoot; // Safe Area를 적용할 루트. 비워두면 자기 자신의 RectTransform을 사용합니다.
    [SerializeField] private bool ignoreLeft; // 좌측 여백을 무시해야 하는 특수 UI에서 사용합니다.
    [SerializeField] private bool ignoreRight; // 우측 여백을 무시해야 하는 특수 UI에서 사용합니다.
    [SerializeField] private bool ignoreTop; // 상단 여백을 무시해야 하는 특수 UI에서 사용합니다.
    [SerializeField] private bool ignoreBottom; // 하단 여백을 무시해야 하는 특수 UI에서 사용합니다.
    [SerializeField] private Vector2 extraPaddingMin; // Safe Area 적용 뒤 추가로 더할 좌하단 여백입니다.
    [SerializeField] private Vector2 extraPaddingMax; // Safe Area 적용 뒤 추가로 더할 우상단 여백입니다.

    private Rect lastSafeArea; // 마지막으로 적용한 Safe Area 값
    private Vector2Int lastScreenSize; // 마지막으로 적용한 화면 크기
    private ScreenOrientation lastOrientation; // 마지막으로 적용한 화면 방향

    // 오브젝트가 켜질 때 현재 기기 Safe Area를 즉시 반영합니다.
    private void OnEnable()
    {
        ApplyIfChanged(true);
    }

    // 에디터 시뮬레이터나 실제 기기 회전처럼 화면 정보가 바뀌면 다시 보정합니다.
    private void Update()
    {
        ApplyIfChanged(false);
    }

    // Inspector에서 값이 바뀔 때도 바로 결과를 볼 수 있게 보정합니다.
    private void OnValidate()
    {
        ApplyIfChanged(true);
    }

    // Safe Area, 해상도, 화면 방향 중 하나라도 바뀌었을 때만 RectTransform을 갱신합니다.
    private void ApplyIfChanged(bool forceApply)
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new(Screen.width, Screen.height);
        ScreenOrientation orientation = Screen.orientation;

        bool changed = forceApply
            || safeArea != lastSafeArea
            || screenSize != lastScreenSize
            || orientation != lastOrientation;

        if (!changed)
            return;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        lastOrientation = orientation;
        ApplySafeArea(safeArea, screenSize);
    }

    // 픽셀 단위 Safe Area를 Canvas anchor 좌표로 바꿔 루트 영역을 조정합니다.
    private void ApplySafeArea(Rect safeArea, Vector2Int screenSize)
    {
        RectTransform root = ResolveTargetRoot();

        if (root == null || screenSize.x <= 0 || screenSize.y <= 0)
            return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x = ignoreLeft ? 0f : anchorMin.x / screenSize.x;
        anchorMin.y = ignoreBottom ? 0f : anchorMin.y / screenSize.y;
        anchorMax.x = ignoreRight ? 1f : anchorMax.x / screenSize.x;
        anchorMax.y = ignoreTop ? 1f : anchorMax.y / screenSize.y;

        root.anchorMin = ClampAnchor(anchorMin);
        root.anchorMax = ClampAnchor(anchorMax);
        root.offsetMin = extraPaddingMin;
        root.offsetMax = -extraPaddingMax;
    }

    // 적용 대상이 비어 있으면 자기 자신의 RectTransform을 사용합니다.
    private RectTransform ResolveTargetRoot()
    {
        if (targetRoot != null)
            return targetRoot;

        targetRoot = transform as RectTransform;
        return targetRoot;
    }

    // 잘못된 기기 값이나 에디터 시뮬레이터 값이 들어와도 anchor 범위를 벗어나지 않게 막습니다.
    private static Vector2 ClampAnchor(Vector2 anchor)
    {
        anchor.x = Mathf.Clamp01(anchor.x);
        anchor.y = Mathf.Clamp01(anchor.y);
        return anchor;
    }
}
