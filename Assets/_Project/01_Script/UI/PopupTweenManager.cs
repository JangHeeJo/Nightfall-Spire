using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// 팝업 열기/닫기 연출을 담당하는 클래스입니다.
// PopupManager는 생성/닫기 정책만 알고, 실제 연출 세부값은 이 클래스 안에 모아둡니다.
public sealed class PopupTweenManager
{
    private const float ButtonPressedScale = 0.9f; // 버튼을 누른 순간 손가락에 눌린 것처럼 줄어드는 비율
    private const float ButtonReboundScale = 1.05f; // 버튼이 다시 올라올 때 살짝 튀어 오르는 비율
    private const float ButtonPressDuration = 0.055f; // 버튼이 눌리는 속도
    private const float ButtonReboundDuration = 0.095f; // 버튼이 튀어 오르는 속도
    private const float ButtonSettleDuration = 0.055f; // 버튼이 원래 크기로 안정되는 속도

    // 팝업이 열릴 때 실행되는 공통 연출 자리입니다.
    // CanvasGroup 알파와 Transform 스케일을 DOTween으로 보간해 모든 팝업의 기본 등장감을 통일합니다.
    public async UniTask PlayOpenAsync(BasePopup popup)
    {
        CanvasGroup canvasGroup = popup.CanvasGroup;

        if (canvasGroup == null)
            return;

        DOTween.Kill(canvasGroup);
        DOTween.Kill(popup.transform);

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        popup.transform.localScale = Vector3.one * 0.88f;

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(canvasGroup.DOFade(1f, 0.14f).SetEase(Ease.OutQuad))
            .Join(popup.transform.DOScale(Vector3.one * 1.04f, 0.18f).SetEase(Ease.OutBack))
            .Append(popup.transform.DOScale(Vector3.one, 0.07f).SetEase(Ease.OutQuad));

        await WaitForTweenAsync(sequence);
    }

    // 모든 UI 버튼에서 재사용하는 공통 눌림 연출입니다.
    // 버튼마다 별도 스크립트를 붙이지 않고, 화면/팝업 루트에서 클릭 순간 호출합니다.
    public static UniTask PlayButtonPressAsync(Transform target)
    {
        if (target == null)
            return UniTask.CompletedTask;

        DOTween.Kill(target, true);

        Vector3 baseScale = target.localScale;
        Sequence sequence = DOTween.Sequence()
            .SetTarget(target)
            .SetUpdate(true)
            .Append(target.DOScale(baseScale * ButtonPressedScale, ButtonPressDuration).SetEase(Ease.OutQuad))
            .Append(target.DOScale(baseScale * ButtonReboundScale, ButtonReboundDuration).SetEase(Ease.OutBack))
            .Append(target.DOScale(baseScale, ButtonSettleDuration).SetEase(Ease.OutQuad));

        return WaitForTweenAsync(sequence);
    }

    // 팝업이 닫힐 때 실행되는 공통 연출 자리입니다.
    // 닫힘 연출이 끝난 뒤에는 입력 차단을 해제해서 Destroy 전 짧은 틈에도 뒤쪽 UI가 막히지 않게 합니다.
    public async UniTask PlayCloseAsync(BasePopup popup)
    {
        CanvasGroup canvasGroup = popup.CanvasGroup;

        if (canvasGroup == null)
            return;

        DOTween.Kill(canvasGroup);
        DOTween.Kill(popup.transform);

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(canvasGroup.DOFade(0f, 0.1f).SetEase(Ease.InQuad))
            .Join(popup.transform.DOScale(Vector3.one * 0.9f, 0.1f).SetEase(Ease.InBack));

        await WaitForTweenAsync(sequence);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    // DOTween의 완료/중단 콜백을 UniTask 흐름으로 변환합니다.
    // 팝업 로직은 UniTask를 기준으로 움직이므로, Tween 완료 시점을 await할 수 있게 감쌉니다.
    private static UniTask WaitForTweenAsync(Tween tween)
    {
        UniTaskCompletionSource completionSource = new();
        bool completed = false;

        void Complete()
        {
            if (completed)
                return;

            completed = true;
            completionSource.TrySetResult();
        }

        tween.OnComplete(Complete);
        tween.OnKill(Complete);

        return completionSource.Task;
    }
}
