using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// BootScene 로딩 UI View입니다.
// 씬에서 Slider 또는 Fill Image를 연결하면 GameRoot 초기화 진행 상태를 표시합니다.
public sealed class BootLoadingView : MonoBehaviour, IBootLoadingView
{
    [Header("Background")]
    [SerializeField] private Image lightBackgroundImage; // 낮 버전 배경 이미지
    [SerializeField] private Image nightBackgroundImage; // 밤 버전 배경 이미지
    [SerializeField] private float backgroundChangeProgress = 0.5f; // 밤 배경으로 전환을 시작하는 진행률
    [SerializeField] private float backgroundFadeSeconds = 1.2f; // 낮/밤 배경이 교차되는 시간

    [Header("Loading")]
    [SerializeField] private RectTransform loadingBarContainer; // 로딩바를 배치할 부모
    [SerializeField] private Slider progressSlider; // Slider 방식 진행률 표시. Fill Image만 사용할 때는 비워도 됩니다.
    [SerializeField] private Image progressFillImage; // Image Fill 방식 진행률 표시. Slider만 사용할 때는 비워도 됩니다.
    [SerializeField] private TMP_Text statusText; // 로딩 단계 문구
    [SerializeField] private TMP_Text percentText; // 퍼센트 표시
    [SerializeField] private TMP_Text versionText; // 버전 표시
    [SerializeField] private float progressTweenSeconds = 0.45f; // 실제 로딩값을 화면에 부드럽게 반영하는 시간

    private Tween backgroundTween; // 진행률 50% 지점에서 실행되는 배경 전환 Tween
    private Tween progressTween; // 로딩바 진행률 표시 Tween
    private float displayedProgress; // 화면에 현재 표시 중인 진행률
    private bool nightBackgroundShown; // 밤 배경 전환을 이미 실행했는지 여부

    // View가 생성될 때 고정 로딩 UI 연결 상태를 검사합니다.
    private void Awake()
    {
        ValidateReferences();
    }

    // View가 켜질 때 배경과 진행률 표시 상태를 초기화합니다.
    private void OnEnable()
    {
        ValidateReferences();
        ResetBackground();
        ApplyProgress(displayedProgress);
    }

    // View가 꺼질 때 진행 중인 DOTween 연출을 정리합니다.
    private void OnDisable()
    {
        StopTweens();
    }

    // 0~1 진행률을 Slider, Image Fill, 퍼센트 텍스트에 반영합니다.
    public void SetProgress(float progress01)
    {
        float clamped = Mathf.Clamp01(progress01);

        if (progressTween != null && progressTween.IsActive())
            progressTween.Kill();

        progressTween = DOTween.To(() => displayedProgress, ApplyProgress, clamped, progressTweenSeconds)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
    }

    // 실제 UI 컴포넌트에 진행률 값을 즉시 반영합니다.
    private void ApplyProgress(float progress01)
    {
        float clamped = Mathf.Clamp01(progress01);
        displayedProgress = clamped;

        if (clamped >= backgroundChangeProgress)
            ShowNightBackground();

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

    // 로딩 시작 상태의 낮 배경을 표시하고 밤 배경은 숨깁니다.
    private void ResetBackground()
    {
        if (lightBackgroundImage == null || nightBackgroundImage == null)
            return;

        if (backgroundTween != null && backgroundTween.IsActive())
            backgroundTween.Kill();

        nightBackgroundShown = false;
        lightBackgroundImage.color = WithAlpha(lightBackgroundImage.color, 1f);
        nightBackgroundImage.color = WithAlpha(nightBackgroundImage.color, 0f);
    }

    // 진행률이 절반 이상 차면 밤 배경을 천천히 드러냅니다.
    private void ShowNightBackground()
    {
        if (nightBackgroundShown || nightBackgroundImage == null)
            return;

        nightBackgroundShown = true;

        if (backgroundTween != null && backgroundTween.IsActive())
            backgroundTween.Kill();

        backgroundTween = nightBackgroundImage.DOFade(1f, backgroundFadeSeconds)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    // 현재 진행 중인 로딩 UI Tween을 모두 중단합니다.
    private void StopTweens()
    {
        if (backgroundTween != null && backgroundTween.IsActive())
            backgroundTween.Kill();

        if (progressTween != null && progressTween.IsActive())
            progressTween.Kill();

        backgroundTween = null;
        progressTween = null;
    }

    // 기존 색상에서 알파만 바꾼 색상을 반환합니다.
    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    // 고정 로딩 UI는 BootScene에 배치된 오브젝트를 Inspector에서 직접 연결해야 합니다.
    // 로딩바는 Slider 방식과 Fill Image 방식 중 하나만 연결되어도 정상입니다.
    private void ValidateReferences()
    {
        if (progressSlider == null && progressFillImage == null)
            Debug.LogError("[BootLoadingView] 로딩 진행률을 표시할 Slider 또는 Fill Image가 연결되지 않았습니다.", this);

        if (lightBackgroundImage == null)
            Debug.LogError("[BootLoadingView] 낮 배경 이미지가 연결되지 않았습니다.", this);

        if (nightBackgroundImage == null)
            Debug.LogError("[BootLoadingView] 밤 배경 이미지가 연결되지 않았습니다.", this);
    }
}
