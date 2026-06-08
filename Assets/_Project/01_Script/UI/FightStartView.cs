using System;
using UnityEngine;
using UnityEngine.UI;

// 로비에서 밤 방어전을 시작하는 버튼 View입니다.
public sealed class FightStartView : MonoBehaviour, IFightStartView
{
    [SerializeField] private Button startButton; // 밤 방어 시작 입력 버튼
    [SerializeField] private Image buttonImage; // 임시 버튼 배경 이미지

    public event Action Clicked; // 버튼 클릭 이벤트

    // 씬에 Button/Image가 아직 없어도 최소 클릭 가능한 버튼을 보장합니다.
    private void Awake()
    {
        EnsureButtonComponents();
    }

    // 버튼 클릭 이벤트를 Unity Button에 연결합니다.
    private void OnEnable()
    {
        EnsureButtonComponents();
        startButton.onClick.AddListener(HandleClicked);
    }

    // View가 비활성화될 때 Unity Button 이벤트를 해제합니다.
    private void OnDisable()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(HandleClicked);
    }

    // Presenter가 버튼 입력 가능 여부를 제어합니다.
    public void SetInteractable(bool isInteractable)
    {
        EnsureButtonComponents();
        startButton.interactable = isInteractable;
    }

    // Unity Button 클릭을 Presenter가 구독하는 일반 C# 이벤트로 바꿉니다.
    private void HandleClicked()
    {
        Clicked?.Invoke();
    }

    // 현재 씬의 FightStartView 오브젝트에 클릭 가능한 최소 UI 컴포넌트를 추가합니다.
    private void EnsureButtonComponents()
    {
        buttonImage ??= GetComponent<Image>();

        if (buttonImage == null)
        {
            buttonImage = gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.15f, 0.35f, 0.95f, 0.85f);
        }

        buttonImage.raycastTarget = true;

        startButton ??= GetComponent<Button>();

        if (startButton == null)
            startButton = gameObject.AddComponent<Button>();

        startButton.targetGraphic = buttonImage;
    }
}
