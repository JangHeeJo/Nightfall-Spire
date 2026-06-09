using TMPro;
using UnityEngine;
using UnityEngine.UI;

// BootScene 로딩 UI View입니다.
// 씬에서 Slider, Fill Image, TextMeshPro를 연결하면 GameRoot 초기화 진행 상태를 표시합니다.
public sealed class BootLoadingView : MonoBehaviour, IBootLoadingView
{
    [SerializeField] private Slider progressSlider; // Slider 방식 진행률 표시
    [SerializeField] private Image progressFillImage; // Image Fill 방식 진행률 표시
    [SerializeField] private TMP_Text statusText; // 로딩 단계 문구
    [SerializeField] private TMP_Text percentText; // 퍼센트 표시
    [SerializeField] private TMP_Text versionText; // 버전 표시

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
}
