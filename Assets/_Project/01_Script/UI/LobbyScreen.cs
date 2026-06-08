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

    // 현재 로비 씬의 FightStartView 이름 오브젝트를 찾아 Button으로 사용합니다.
    // 이후 실제 UI 프리팹이 정리되면 Inspector에서 명시적으로 연결하면 됩니다.
    private void EnsureNightDefenseStartButton()
    {
        if (nightDefenseStartButton == null)
            nightDefenseStartButton = FindNightDefenseStartButton();

        if (nightDefenseStartButton == null)
            return;

        nightDefenseStartButtonImage ??= nightDefenseStartButton.GetComponent<Image>();

        if (nightDefenseStartButtonImage == null)
        {
            nightDefenseStartButtonImage = nightDefenseStartButton.gameObject.AddComponent<Image>();
            nightDefenseStartButtonImage.color = new Color(0.15f, 0.35f, 0.95f, 0.85f);
        }

        nightDefenseStartButtonImage.raycastTarget = true;
        nightDefenseStartButton.targetGraphic = nightDefenseStartButtonImage;
    }

    // 화면 아래에 있는 기존 FightStartView 오브젝트를 찾아 Button 컴포넌트를 보장합니다.
    private Button FindNightDefenseStartButton()
    {
        Transform[] children = transform.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name != "FightStartView")
                continue;

            return children[i].GetComponent<Button>() ?? children[i].gameObject.AddComponent<Button>();
        }

        return null;
    }
}
