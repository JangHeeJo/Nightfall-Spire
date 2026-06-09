using System;
using UnityEngine;
using UnityEngine.UI;

// 로비 화면 전체를 대표하는 View입니다.
// 개별 버튼마다 스크립트를 만들지 않고, 로비 화면의 주요 입력을 화면 단위 이벤트로 모읍니다.
public sealed class LobbyScreen : MonoBehaviour, ILobbyScreenView
{
    [SerializeField] private Button nightDefenseStartButton; // 밤 방어 시작 버튼

    public event Action NightDefenseRequested; // 밤 방어 시작 요청 이벤트

    // 씬에 배치된 로비 버튼 참조가 유효한지 확인합니다.
    private void Awake()
    {
        ValidateReferences();
    }

    // Unity Button 클릭을 화면 단위 C# 이벤트로 바꿉니다.
    private void OnEnable()
    {
        if (nightDefenseStartButton != null)
            nightDefenseStartButton.onClick.AddListener(HandleNightDefenseStartClicked);
    }

    // View가 비활성화될 때 Unity Button 이벤트를 해제합니다.
    private void OnDisable()
    {
        if (nightDefenseStartButton != null)
            nightDefenseStartButton.onClick.RemoveListener(HandleNightDefenseStartClicked);
    }

    // 로비 화면 View가 시작 버튼을 찾았는지 확인합니다.
    public bool IsReady()
    {
        return nightDefenseStartButton != null;
    }

    // Presenter가 밤 방어 시작 입력 가능 여부를 제어합니다.
    public void SetNightDefenseStartInteractable(bool isInteractable)
    {
        if (nightDefenseStartButton != null)
            nightDefenseStartButton.interactable = isInteractable;
    }

    // 밤 방어 시작 버튼 클릭을 Presenter가 구독하는 이벤트로 전달합니다.
    private void HandleNightDefenseStartClicked()
    {
        NightDefenseRequested?.Invoke();
    }

    // 시작 버튼은 씬/프리팹에서 명시적으로 연결되어야 합니다.
    private void ValidateReferences()
    {
        if (nightDefenseStartButton == null)
        {
            Debug.LogError("[LobbyScreen] nightDefenseStartButton 참조가 비어 있습니다. 로비 고정 UI에서 Fight 버튼을 직접 연결해야 합니다.");
            return;
        }

        if (nightDefenseStartButton.targetGraphic == null)
            Debug.LogError("[LobbyScreen] 시작 버튼의 targetGraphic이 비어 있습니다. 버튼의 Image 또는 TMP Graphic을 연결해야 클릭 상태가 정상 동작합니다.");

        if (nightDefenseStartButton.targetGraphic != null)
            nightDefenseStartButton.targetGraphic.raycastTarget = true;
    }
}
