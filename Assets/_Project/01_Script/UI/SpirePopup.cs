using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 스파이어 메인 화면 팝업의 최상위 View입니다.
// 로비 진입 시 기본으로 열리는 Content 팝업이며, 팝업 내부 버튼 입력을 Controller로 전달합니다.
public sealed class SpirePopup : BasePopup
{
    private const string FightButtonName = "FightButton_Battle"; // 스파이어 팝업 안 전투 시작 버튼 이름

    [SerializeField] private Button fightButton; // 스파이어 팝업 안 중앙 FIGHT 버튼

    private UnityAction fightButtonClick; // OnDestroy에서 정확히 제거할 버튼 콜백

    public event Action FightRequested; // 전투 시작 요청 이벤트

    // 팝업 생성 직후 내부 버튼 참조와 이벤트를 준비합니다.
    protected override void OnInitialized()
    {
        CacheReferences();
        RegisterButtons();
    }

    // 전투 시작 처리 중 중복 입력을 막거나 다시 풀 때 Controller가 사용합니다.
    public void SetFightButtonInteractable(bool isInteractable)
    {
        if (fightButton != null)
            fightButton.interactable = isInteractable;
    }

    private void OnDestroy()
    {
        UnregisterButtons();
    }

    // 인스펙터 연결이 없을 때도 프리팹 내부 이름 기준으로 한 번만 찾아 보정합니다.
    private void CacheReferences()
    {
        if (fightButton != null)
            return;

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.name == FightButtonName)
            {
                fightButton = button;
                return;
            }
        }

        Debug.LogError($"[SpirePopup] {FightButtonName} 버튼을 찾지 못했습니다.", this);
    }

    // View는 클릭 사실만 알리고, 실제 씬 전환은 Controller가 처리합니다.
    private void RegisterButtons()
    {
        if (fightButton == null)
            return;

        fightButtonClick = () => FightRequested?.Invoke();
        fightButton.onClick.AddListener(fightButtonClick);
    }

    // 팝업이 파괴될 때 런타임에 붙인 클릭 콜백을 정리합니다.
    private void UnregisterButtons()
    {
        if (fightButton != null && fightButtonClick != null)
            fightButton.onClick.RemoveListener(fightButtonClick);

        fightButtonClick = null;
    }
}
