using Cysharp.Threading.Tasks;
using UnityEngine;

// 씬 Canvas 아래에서 화면 전체 페이드를 담당하는 View입니다.
// 실제 오브젝트는 각 씬의 Canvas_DynamicUI 아래에 두고, 전역 요청은 ScreenFadeManager가 전달합니다.
public sealed class ScreenFadeView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup; // 페이드 알파와 입력 차단을 제어합니다.

    private float currentAlpha; // 현재 페이드 알파 값

    // View가 생성될 때 필요한 CanvasGroup을 준비하고 기본 투명 상태로 맞춥니다.
    // 씬에 CanvasGroup이 빠져 있어도 실행 중 자동으로 보강해서 기본 동작이 깨지지 않게 합니다.
    private void Awake()
    {
        EnsureCanvasGroup();
        SetFadeAlpha(0f);
    }

    // 현재 페이드 알파를 즉시 설정합니다.
    // 0이면 완전히 투명, 1이면 완전히 어두운 상태로 보고 입력 차단도 함께 갱신합니다.
    public void SetFadeAlpha(float alpha)
    {
        EnsureCanvasGroup();

        currentAlpha = Mathf.Clamp01(alpha);
        canvasGroup.alpha = currentAlpha;
        bool blocksInput = currentAlpha > 0.001f;
        canvasGroup.blocksRaycasts = blocksInput;
        canvasGroup.interactable = blocksInput;
    }

    // 목표 알파까지 시간 보간으로 페이드합니다.
    // DOTween을 아직 의존성으로 넣지 않았기 때문에 UniTask.Yield 기반으로 프레임마다 직접 보간합니다.
    public async UniTask FadeToAsync(float targetAlpha, float duration, bool blockInputAfterFade)
    {
        EnsureCanvasGroup();

        float startAlpha = currentAlpha;
        float endAlpha = Mathf.Clamp01(targetAlpha);

        if (duration <= 0f)
        {
            SetFadeAlpha(endAlpha);
            SetInputBlock(blockInputAfterFade);
            return;
        }

        SetInputBlock(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFadeAlpha(Mathf.Lerp(startAlpha, endAlpha, t));
            await UniTask.Yield();
        }

        SetFadeAlpha(endAlpha);
        SetInputBlock(blockInputAfterFade);
    }

    // CanvasGroup의 입력 차단 상태만 바꿉니다.
    // 완전히 투명해진 뒤에는 뒤쪽 UI를 다시 누를 수 있어야 하므로 FadeIn 끝에서 false로 돌립니다.
    private void SetInputBlock(bool blockInput)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.blocksRaycasts = blockInput;
        canvasGroup.interactable = blockInput;
    }

    // 페이드 제어에 필요한 CanvasGroup을 보장합니다.
    // Inspector에 연결되어 있지 않으면 같은 오브젝트에서 찾고, 없으면 새로 추가합니다.
    private void EnsureCanvasGroup()
    {
        if (canvasGroup != null)
            return;

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
}
