using UnityEngine;
using UnityEngine.UI;

// 재화 HUD의 화면 표시만 담당하는 View입니다.
// 골드/젬 값이 어디서 오는지는 알지 않고, 전달받은 값을 텍스트에 표시하는 책임만 가집니다.
public sealed class CurrencyHud : MonoBehaviour
{
    [SerializeField] private Text goldText; // 골드 표시 텍스트
    [SerializeField] private Text gemText; // 젬 표시 텍스트

    // 현재 골드 값을 HUD에 표시합니다.
    // 값 변경 구독은 Binder가 담당하고, View는 전달받은 값을 화면 형식으로 바꾸는 일만 합니다.
    public void SetGold(long gold)
    {
        SetText(goldText, FormatAmount(gold));
    }

    // 현재 젬 값을 HUD에 표시합니다.
    // 나중에 아이콘, 색상, 애니메이션이 붙어도 값의 출처는 여전히 Binder가 관리합니다.
    public void SetGem(long gem)
    {
        SetText(gemText, FormatAmount(gem));
    }

    // 지정된 텍스트가 연결되어 있을 때만 값을 반영합니다.
    // 아직 씬에 텍스트 오브젝트가 없는 초기 아키텍처 단계에서도 컴파일과 실행이 깨지지 않게 둡니다.
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }

    // 재화 숫자를 화면 표시용 문자열로 변환합니다.
    // 현재는 천 단위 구분만 적용하고, K/M/B 축약 표기는 밸런스 기준이 잡힌 뒤 추가합니다.
    private string FormatAmount(long amount)
    {
        return amount.ToString("N0");
    }
}
