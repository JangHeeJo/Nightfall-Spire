using System;
using UnityEngine;
using UnityEngine.UI;

// 로비 화면 전체를 대표하는 View입니다.
// 개별 버튼마다 스크립트를 만들지 않고, 로비 화면의 주요 입력을 화면 단위 이벤트로 모읍니다.
public sealed class LobbyScreen : MonoBehaviour, ILobbyScreenView
{
    [SerializeField] private Button nightDefenseStartButton; // 밤 방어 시작 버튼
    [SerializeField] private Image nightDefenseStartButtonImage; // 임시 버튼 배경 이미지

    public event Action NightDefenseRequested; // 밤 방어 시작 요청 이벤트

    // 씬에 Button/Image가 아직 없어도 로비 화면에서 최소 클릭 가능한 시작 버튼을 보장합니다.
    private void Awake()
    {
        EnsureNightDefenseStartButton();
    }

    // Unity Button 클릭을 화면 단위 C# 이벤트로 바꿉니다.
    private void OnEnable()
    {
        EnsureNightDefenseStartButton();

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
        EnsureNightDefenseStartButton();
        return nightDefenseStartButton != null;
    }

    // Presenter가 밤 방어 시작 입력 가능 여부를 제어합니다.
    public void SetNightDefenseStartInteractable(bool isInteractable)
    {
        EnsureNightDefenseStartButton();

        if (nightDefenseStartButton != null)
            nightDefenseStartButton.interactable = isInteractable;
    }

    // 밤 방어 시작 버튼 클릭을 Presenter가 구독하는 이벤트로 전달합니다.
    private void HandleNightDefenseStartClicked()
    {
        NightDefenseRequested?.Invoke();
    }

    // 현재 로비 씬의 Lobby_Default 프리팹 안에 있는 시작 버튼을 찾아 Button으로 사용합니다.
    // 프리팹 구조가 바뀌어도 Button_03_Red 이름을 우선 기준으로 삼습니다.
    private void EnsureNightDefenseStartButton()
    {
        if (nightDefenseStartButton == null)
            nightDefenseStartButton = FindNightDefenseStartButton();

        if (nightDefenseStartButton == null)
            return;

        nightDefenseStartButtonImage ??= nightDefenseStartButton.targetGraphic as Image;
        nightDefenseStartButtonImage ??= nightDefenseStartButton.GetComponent<Image>();
        nightDefenseStartButtonImage ??= nightDefenseStartButton.GetComponentInChildren<Image>(true);

        if (nightDefenseStartButtonImage == null)
        {
            nightDefenseStartButtonImage = nightDefenseStartButton.gameObject.AddComponent<Image>();
            nightDefenseStartButtonImage.color = new Color(0.15f, 0.35f, 0.95f, 0.85f);
        }

        nightDefenseStartButtonImage.raycastTarget = true;
        nightDefenseStartButton.targetGraphic = nightDefenseStartButtonImage;
    }

    // Lobby_Default 프리팹 안의 Button_03_Red 또는 START/FIGHT 라벨을 가진 버튼을 찾습니다.
    private Button FindNightDefenseStartButton()
    {
        Transform namedButton = FindChildByName(transform, "Button_03_Red");

        if (namedButton != null)
            return EnsureButtonComponent(namedButton);

        Button[] buttons = transform.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == "Button_03_Red")
                return buttons[i];
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            TMPro.TMP_Text label = buttons[i].GetComponentInChildren<TMPro.TMP_Text>(true);

            if (label != null && (label.text == "START" || label.text == "FIGHT"))
                return buttons[i];
        }

        return null;
    }

    // 현재 로비 View 하위에서 이름이 같은 UI Transform을 찾습니다.
    private Transform FindChildByName(Transform root, string objectName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }

    // 프리팹에 Button 컴포넌트가 빠져 있어도 기존 이미지를 그대로 클릭 대상으로 사용합니다.
    private Button EnsureButtonComponent(Transform target)
    {
        Button button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
        Graphic targetGraphic = target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);

        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
            button.targetGraphic = targetGraphic;
        }

        return button;
    }
}
