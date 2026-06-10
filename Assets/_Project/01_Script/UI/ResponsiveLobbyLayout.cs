using UnityEngine;

// 로비처럼 세로형 화면에서 아이패드와 일반 휴대폰의 화면 비율 차이를 흡수합니다.
// 버튼 하나하나를 코드로 움직이지 않고, 큰 UI 영역의 크기와 위치만 보정하는 용도입니다.
public sealed class ResponsiveLobbyLayout : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private RectTransform centerSpireArea; // 중앙 성채를 담는 영역입니다.
    [SerializeField] private RectTransform fightButtonArea; // Fight 버튼을 담는 영역입니다.
    [SerializeField] private RectTransform bottomNavigationArea; // 하단 탭 메뉴 영역입니다.
    [SerializeField] private RectTransform topHudArea; // 상단 재화와 정보 영역입니다.
    [SerializeField] private RectTransform leftShortcutArea; // 좌측 바로가기 버튼 묶음입니다.
    [SerializeField] private RectTransform rightShortcutArea; // 우측 바로가기 버튼 묶음입니다.
    [SerializeField] private RectTransform fullScreenBackgroundArea; // SafeArea 밖 빈 공간까지 덮어야 하는 장식 배경입니다.

    [Header("Aspect Rules")]
    [SerializeField] private float wideScreenAspect = 0.68f; // 이 값보다 넓으면 아이패드 계열로 봅니다.
    [SerializeField] private float tallScreenAspect = 0.50f; // 이 값보다 좁으면 긴 휴대폰 계열로 봅니다.
    [SerializeField] private LayoutProfile normalProfile = new(1f, 0f, 0f, 0f); // 기준 휴대폰 비율에서 쓰는 보정값입니다.
    [SerializeField] private LayoutProfile tallPhoneProfile = new(1.04f, 24f, 8f, 0f); // 긴 휴대폰에서 중앙 공간을 조금 더 활용합니다.
    [SerializeField] private LayoutProfile wideTabletProfile = new(1.15f, 18f, 0f, 0f); // 아이패드에서는 빈 공간이 커지므로 성채를 키우고 버튼 영역은 SafeArea 안에 둡니다.

    private Vector2Int lastScreenSize; // 마지막으로 계산한 화면 크기
    private ScreenOrientation lastOrientation; // 마지막으로 계산한 화면 방향
    private LayoutProfile appliedProfile; // 현재 적용 중인 비율 보정값
    private bool baseLayoutCaptured; // 씬에서 잡아둔 기준 배치를 저장했는지 여부
    private TransformSnapshot centerBase; // 중앙 성채 영역 기준 배치
    private TransformSnapshot fightBase; // Fight 버튼 영역 기준 배치
    private TransformSnapshot bottomBase; // 하단 메뉴 영역 기준 배치
    private TransformSnapshot topBase; // 상단 HUD 영역 기준 배치
    private TransformSnapshot leftBase; // 좌측 버튼 영역 기준 배치
    private TransformSnapshot rightBase; // 우측 버튼 영역 기준 배치

    // 씬에 배치된 기준 위치를 먼저 기억합니다.
    private void Awake()
    {
        CaptureBaseLayout();
    }

    // 로비 UI가 켜질 때 현재 화면 비율에 맞춰 한 번 배치합니다.
    private void OnEnable()
    {
        CaptureBaseLayout();
        ApplyIfChanged(true);
    }

    // 기기 회전, 시뮬레이터 기기 변경, 해상도 변경이 일어나면 다시 배치합니다.
    private void Update()
    {
        ApplyIfChanged(false);
    }

    // Inspector에서 보정 수치를 바꿨을 때 에디터에서 즉시 확인할 수 있게 합니다.
    private void OnValidate()
    {
        CaptureBaseLayout();
        ApplyIfChanged(true);
    }

    // 씬에서 기준 위치를 다시 잡은 뒤 Inspector 메뉴로 호출하면 새 기준 배치를 저장합니다.
    [ContextMenu("현재 배치를 반응형 기준값으로 다시 저장")]
    private void RecaptureBaseLayout()
    {
        baseLayoutCaptured = false;
        CaptureBaseLayout();
        ApplyIfChanged(true);
    }

    // 화면 크기나 방향이 바뀌었을 때만 레이아웃 보정을 다시 적용합니다.
    private void ApplyIfChanged(bool forceApply)
    {
        Vector2Int screenSize = new(Screen.width, Screen.height);
        ScreenOrientation orientation = Screen.orientation;

        bool changed = forceApply || screenSize != lastScreenSize || orientation != lastOrientation;

        if (!changed)
            return;

        lastScreenSize = screenSize;
        lastOrientation = orientation;
        appliedProfile = SelectProfile(screenSize);
        ApplyProfile(appliedProfile);
    }

    // 현재 화면의 가로/세로 비율로 휴대폰, 긴 휴대폰, 태블릿 보정값 중 하나를 고릅니다.
    private LayoutProfile SelectProfile(Vector2Int screenSize)
    {
        if (screenSize.x <= 0 || screenSize.y <= 0)
            return normalProfile;

        float aspect = (float)screenSize.x / screenSize.y;

        if (aspect >= wideScreenAspect)
            return wideTabletProfile;

        if (aspect <= tallScreenAspect)
            return tallPhoneProfile;

        return normalProfile;
    }

    // 선택된 보정값을 큰 UI 영역들에만 적용합니다.
    private void ApplyProfile(LayoutProfile profile)
    {
        CaptureBaseLayout();
        ApplySnapshot(centerSpireArea, centerBase, profile.centerScale, profile.centerOffsetY);
        ApplySnapshot(fightButtonArea, fightBase, 1f, profile.fightOffsetY);
        ApplySnapshot(bottomNavigationArea, bottomBase, 1f, profile.bottomOffsetY);
        ApplySnapshot(topHudArea, topBase, 1f, 0f);
        ApplySnapshot(leftShortcutArea, leftBase, 1f, 0f);
        ApplySnapshot(rightShortcutArea, rightBase, 1f, 0f);
        StretchBackgroundToScreen();
    }

    // 현재 씬에서 맞춰둔 RectTransform 값을 기준 배치로 저장합니다.
    private void CaptureBaseLayout()
    {
        if (baseLayoutCaptured)
            return;

        centerBase = TransformSnapshot.Capture(centerSpireArea);
        fightBase = TransformSnapshot.Capture(fightButtonArea);
        bottomBase = TransformSnapshot.Capture(bottomNavigationArea);
        topBase = TransformSnapshot.Capture(topHudArea);
        leftBase = TransformSnapshot.Capture(leftShortcutArea);
        rightBase = TransformSnapshot.Capture(rightShortcutArea);
        baseLayoutCaptured = true;
    }

    // 기준 배치에 화면 비율별 스케일과 Y 보정값만 더해 적용합니다.
    private static void ApplySnapshot(RectTransform target, TransformSnapshot snapshot, float scaleMultiplier, float offsetY)
    {
        if (target == null)
            return;

        float safeScale = Mathf.Max(0.01f, scaleMultiplier);
        target.anchoredPosition = snapshot.AnchoredPosition + new Vector2(0f, offsetY);
        target.localScale = new Vector3(
            snapshot.LocalScale.x * safeScale,
            snapshot.LocalScale.y * safeScale,
            snapshot.LocalScale.z);
    }

    // 조작 UI는 SafeArea 안에 두되, 장식 배경은 화면 끝까지 깔아서 빈 공간이 보이지 않게 합니다.
    private void StretchBackgroundToScreen()
    {
        if (fullScreenBackgroundArea == null)
            return;

        Rect safeArea = Screen.safeArea;

        if (safeArea.width <= 0f || safeArea.height <= 0f || Screen.width <= 0 || Screen.height <= 0)
            return;

        RectTransform parent = fullScreenBackgroundArea.parent as RectTransform;

        if (parent == null)
            return;

        float leftPadding = safeArea.xMin / safeArea.width * parent.rect.width;
        float rightPadding = (Screen.width - safeArea.xMax) / safeArea.width * parent.rect.width;
        float bottomPadding = safeArea.yMin / safeArea.height * parent.rect.height;
        float topPadding = (Screen.height - safeArea.yMax) / safeArea.height * parent.rect.height;

        fullScreenBackgroundArea.anchorMin = Vector2.zero;
        fullScreenBackgroundArea.anchorMax = Vector2.one;
        fullScreenBackgroundArea.offsetMin = new Vector2(-leftPadding, -bottomPadding);
        fullScreenBackgroundArea.offsetMax = new Vector2(rightPadding, topPadding);
        fullScreenBackgroundArea.localScale = Vector3.one;
    }

    [System.Serializable]
    private struct LayoutProfile
    {
        public float centerScale; // 중앙 성채 영역 스케일
        public float centerOffsetY; // 중앙 성채 영역의 Y 위치 보정
        public float fightOffsetY; // Fight 버튼 영역의 Y 위치 보정
        public float bottomOffsetY; // 하단 메뉴 영역의 Y 위치 보정

        public LayoutProfile(float centerScale, float centerOffsetY, float fightOffsetY, float bottomOffsetY)
        {
            this.centerScale = centerScale;
            this.centerOffsetY = centerOffsetY;
            this.fightOffsetY = fightOffsetY;
            this.bottomOffsetY = bottomOffsetY;
        }
    }

    private readonly struct TransformSnapshot
    {
        public readonly Vector2 AnchoredPosition; // 씬에서 설정한 기준 위치
        public readonly Vector3 LocalScale; // 씬에서 설정한 기준 스케일

        private TransformSnapshot(Vector2 anchoredPosition, Vector3 localScale)
        {
            AnchoredPosition = anchoredPosition;
            LocalScale = localScale;
        }

        // 대상 RectTransform의 현재 위치와 스케일을 기준값으로 저장합니다.
        public static TransformSnapshot Capture(RectTransform target)
        {
            if (target == null)
                return new TransformSnapshot(Vector2.zero, Vector3.one);

            return new TransformSnapshot(target.anchoredPosition, target.localScale);
        }
    }
}
