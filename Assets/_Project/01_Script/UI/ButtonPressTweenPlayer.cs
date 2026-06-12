using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

// 모바일 게임 버튼처럼 눌림이 확실히 보이는 공통 DOTween 피드백입니다.
// 개별 버튼마다 스크립트를 붙이지 않고, View가 클릭 순간 이 함수를 호출해서 같은 느낌을 재사용합니다.
public static class ButtonPressTweenPlayer
{
    private const float PressedScale = 0.9f; // 손가락으로 눌린 순간의 축소 비율
    private const float ReboundScale = 1.05f; // 복귀할 때 살짝 튀어 오르는 비율
    private const float PressDuration = 0.055f; // 눌림 속도
    private const float ReboundDuration = 0.095f; // 반동 복귀 속도
    private const float SettleDuration = 0.055f; // 원래 크기로 안정되는 속도

    // 버튼 Transform 기준으로 눌림, 반동, 원위치 복귀를 순서대로 재생합니다.
    public static UniTask PlayAsync(Transform target)
    {
        if (target == null)
            return UniTask.CompletedTask;

        DOTween.Kill(target, true);

        Vector3 baseScale = target.localScale;
        Sequence sequence = DOTween.Sequence()
            .SetTarget(target)
            .SetUpdate(true)
            .Append(target.DOScale(baseScale * PressedScale, PressDuration).SetEase(Ease.OutQuad))
            .Append(target.DOScale(baseScale * ReboundScale, ReboundDuration).SetEase(Ease.OutBack))
            .Append(target.DOScale(baseScale, SettleDuration).SetEase(Ease.OutQuad));

        return WaitForTweenAsync(sequence);
    }

    // DOTween 완료/중단을 UniTask로 기다립니다.
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
