using Cysharp.Threading.Tasks;
using UnityEngine;

// 팝업 열기/닫기 연출을 담당하는 클래스입니다.
// DOTween을 붙이기 전까지는 CanvasGroup 상태만 즉시 반영하고,
// 나중에 DOTween 설치 후 이 클래스 내부만 교체하면 PopupManager 구조는 유지됩니다.
public sealed class PopupTweenManager
{
    // 팝업이 열릴 때 실행되는 공통 연출 자리입니다.
    // 현재는 즉시 표시만 하고, 추후 scale/alpha tween을 여기에서 처리합니다.
    public UniTask PlayOpenAsync(BasePopup popup)
    {
        CanvasGroup canvasGroup = popup.CanvasGroup;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        return UniTask.CompletedTask;
    }

    // 팝업이 닫힐 때 실행되는 공통 연출 자리입니다.
    // 현재는 즉시 숨김만 하고, 추후 닫힘 tween을 여기에서 처리합니다.
    public UniTask PlayCloseAsync(BasePopup popup)
    {
        CanvasGroup canvasGroup = popup.CanvasGroup;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        return UniTask.CompletedTask;
    }
}
