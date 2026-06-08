using UnityEngine;
using UnityEngine.UI;

// 재화 HUD의 Unity View입니다.
// Model을 직접 알지 않고 Presenter가 전달한 표시 문자열만 화면에 반영합니다.
public sealed class CurrencyHud : MonoBehaviour, ICurrencyHudView
{
    [SerializeField] private Text goldText; // 골드 표시 텍스트
    [SerializeField] private Text gemText; // 젬 표시 텍스트

    // Presenter가 만든 표시 상태를 화면 텍스트에 반영합니다.
    public void Render(CurrencyHudViewState viewState)
    {
        SetText(goldText, viewState.GoldText);
        SetText(gemText, viewState.GemText);
    }

    // HUD 텍스트가 모두 연결되어 있는지 확인합니다.
    // Presenter가 초기화 시 경고를 낼 수 있도록 View의 준비 상태만 알려줍니다.
    public bool IsReady()
    {
        return goldText != null && gemText != null;
    }

    // 지정된 텍스트가 연결되어 있을 때만 값을 반영합니다.
    // 아직 씬에 텍스트 오브젝트가 없는 초기 아키텍처 단계에서도 컴파일과 실행이 깨지지 않게 둡니다.
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
