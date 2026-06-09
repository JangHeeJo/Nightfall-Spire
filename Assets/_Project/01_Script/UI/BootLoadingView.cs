using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// BootScene 로딩 UI View입니다.
// 씬에서 Slider, Fill Image, TextMeshPro를 연결하면 GameRoot 초기화 진행 상태를 표시합니다.
public sealed class BootLoadingView : MonoBehaviour, IBootLoadingView
{
    [Header("Background")]
    [SerializeField] private Image lightBackgroundImage; // 낮 버전 배경 이미지
    [SerializeField] private Image nightBackgroundImage; // 밤 버전 배경 이미지
    [SerializeField] private float backgroundHoldSeconds = 5f; // 한 배경을 유지하는 시간
    [SerializeField] private float backgroundFadeSeconds = 1.2f; // 낮/밤 배경이 교차되는 시간

    [Header("Loading")]
    [SerializeField] private GameObject loadingBarPrefab; // 런타임에 생성할 로딩바 프리팹
    [SerializeField] private RectTransform loadingBarContainer; // 로딩바 프리팹을 배치할 부모
    [SerializeField] private Slider progressSlider; // Slider 방식 진행률 표시
    [SerializeField] private Image progressFillImage; // Image Fill 방식 진행률 표시
    [SerializeField] private TMP_Text statusText; // 로딩 단계 문구
    [SerializeField] private TMP_Text percentText; // 퍼센트 표시
    [SerializeField] private TMP_Text versionText; // 버전 표시

    private Sequence backgroundSequence; // 낮/밤 배경 반복 연출 Tween
    private GameObject loadingBarInstance; // 런타임에 생성된 로딩바 인스턴스

    // View가 생성될 때 비어 있는 UI 참조를 자식 오브젝트에서 자동으로 보강합니다.
    private void Awake()
    {
        EnsureLoadingBarInstance();
        EnsureLoadingReferences();
    }

    // View가 켜질 때 낮/밤 배경 교차 연출을 시작합니다.
    private void OnEnable()
    {
        EnsureLoadingBarInstance();
        EnsureLoadingReferences();
        StartBackgroundLoop();
    }

    // View가 꺼질 때 진행 중인 DOTween 연출을 정리합니다.
    private void OnDisable()
    {
        StopBackgroundLoop();
    }

    // 0~1 진행률을 Slider, Image Fill, 퍼센트 텍스트에 반영합니다.
    public void SetProgress(float progress01)
    {
        float clamped = Mathf.Clamp01(progress01);

        if (progressSlider != null)
            progressSlider.value = clamped;

        if (progressFillImage != null)
            progressFillImage.fillAmount = clamped;

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(clamped * 100f)}%";
    }

    // 현재 로딩 단계 문구를 표시합니다.
    public void SetStatusText(string nextStatusText)
    {
        if (statusText != null)
            statusText.text = nextStatusText ?? string.Empty;
    }

    // 버전 또는 빌드 정보를 표시합니다.
    public void SetVersionText(string nextVersionText)
    {
        if (versionText != null)
            versionText.text = nextVersionText ?? string.Empty;
    }

    // 낮/밤 배경 이미지를 DOTween으로 천천히 교차시킵니다.
    private void StartBackgroundLoop()
    {
        StopBackgroundLoop();

        if (lightBackgroundImage == null || nightBackgroundImage == null)
            return;

        lightBackgroundImage.color = WithAlpha(lightBackgroundImage.color, 1f);
        nightBackgroundImage.color = WithAlpha(nightBackgroundImage.color, 0f);

        backgroundSequence = DOTween.Sequence()
            .AppendInterval(backgroundHoldSeconds)
            .Append(nightBackgroundImage.DOFade(1f, backgroundFadeSeconds))
            .AppendInterval(backgroundHoldSeconds)
            .Append(nightBackgroundImage.DOFade(0f, backgroundFadeSeconds))
            .SetLoops(-1)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    // 현재 배경 연출 Tween을 중단합니다.
    private void StopBackgroundLoop()
    {
        if (backgroundSequence == null)
            return;

        backgroundSequence.Kill();
        backgroundSequence = null;
    }

    // 기존 색상에서 알파만 바꾼 색상을 반환합니다.
    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    // Inspector 연결이 비어 있으면 자식 로딩바 프리팹에서 Slider와 Fill 이미지를 찾아 연결합니다.
    private void EnsureLoadingReferences()
    {
        if (progressSlider == null)
            progressSlider = GetComponentInChildren<Slider>(true);

        if (progressFillImage == null && progressSlider != null && progressSlider.fillRect != null)
            progressFillImage = progressSlider.fillRect.GetComponent<Image>();
    }

    // 씬에 로딩바 프리팹 참조가 있으면 지정된 컨테이너 아래에 한 번만 생성합니다.
    private void EnsureLoadingBarInstance()
    {
        if (loadingBarInstance != null || loadingBarPrefab == null)
            return;

        Transform parent = loadingBarContainer != null ? loadingBarContainer : transform;

        // 외부 프리팹 인스턴스 기반 로딩바는 제네릭 Instantiate에서 캐스팅 예외가 날 수 있어 Object로 먼저 생성합니다.
        Object instantiatedObject = Instantiate((Object)loadingBarPrefab, parent);
        loadingBarInstance = ResolveInstantiatedGameObject(instantiatedObject);

        if (loadingBarInstance == null)
        {
            Debug.LogError("[BootLoadingView] LoadingBar 프리팹 생성 결과를 GameObject로 해석할 수 없습니다.");
            return;
        }

        loadingBarInstance.name = loadingBarPrefab.name;

        if (loadingBarInstance.transform is RectTransform rectTransform)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }

    // Unity가 프리팹 복제 결과를 GameObject 또는 Component로 돌려주는 경우를 모두 GameObject로 통일합니다.
    private static GameObject ResolveInstantiatedGameObject(Object instantiatedObject)
    {
        if (instantiatedObject is GameObject gameObject)
            return gameObject;

        if (instantiatedObject is Component component)
            return component.gameObject;

        return null;
    }
}
