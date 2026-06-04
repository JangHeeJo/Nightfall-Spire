using Cysharp.Threading.Tasks;
using UnityEngine;

// 현재 씬의 ScreenFadeView 참조를 보관하고 전역 페이드 요청을 전달합니다.
// GameRoot가 Canvas를 직접 소유하지 않기 때문에, 각 씬 DynamicUIRoot가 자기 Fade View를 등록합니다.
public sealed class ScreenFadeManager
{
    private Object registeredOwner; // 페이드 View를 등록한 씬 Root
    private string registeredSceneName; // 디버깅용 씬 이름

    public ScreenFadeView CurrentFadeView { get; private set; } // 현재 씬의 페이드 View
    public bool HasActiveFade => CurrentFadeView != null;

    // 현재 씬의 ScreenFadeView를 등록합니다.
    // 씬이 바뀔 때마다 DynamicUIRoot가 새 View를 등록해서 전역 페이드 요청의 대상이 바뀝니다.
    public void RegisterSceneFade(Object owner, string sceneName, ScreenFadeView fadeView)
    {
        registeredOwner = owner;
        registeredSceneName = sceneName;
        CurrentFadeView = fadeView;

        if (CurrentFadeView == null)
        {
            Debug.LogWarning($"[ScreenFadeManager] {registeredSceneName}에 ScreenFadeView가 연결되지 않았습니다.");
            return;
        }

        CurrentFadeView.SetFadeAlpha(0f);
        Debug.Log($"[ScreenFadeManager] {registeredSceneName} ScreenFade 등록 완료");
    }

    // 등록한 씬 Root가 파괴될 때 ScreenFadeView 참조를 해제합니다.
    // 현재 등록 주체가 아닌 오브젝트의 해제 요청은 씬 전환 타이밍 충돌로 보고 무시합니다.
    public void UnregisterSceneFade(Object owner)
    {
        if (registeredOwner != owner)
            return;

        registeredOwner = null;
        registeredSceneName = string.Empty;
        CurrentFadeView = null;

        Debug.Log("[ScreenFadeManager] ScreenFade 등록 해제");
    }

    // 현재 씬을 어둡게 가립니다.
    // 씬 전환이나 로딩 시작 전에 호출해서 뒤쪽 화면 입력도 함께 막을 수 있습니다.
    public UniTask FadeOutAsync(float duration = 0.25f)
    {
        if (CurrentFadeView == null)
            return UniTask.CompletedTask;

        return CurrentFadeView.FadeToAsync(1f, duration, true);
    }

    // 현재 씬을 다시 보이게 합니다.
    // 씬 로드가 끝났거나 전환 연출이 끝난 뒤 호출해서 입력 차단도 해제합니다.
    public UniTask FadeInAsync(float duration = 0.25f)
    {
        if (CurrentFadeView == null)
            return UniTask.CompletedTask;

        return CurrentFadeView.FadeToAsync(0f, duration, false);
    }
}
