using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 로비 화면 전체를 대표하는 View입니다.
// 개별 버튼마다 스크립트를 만들지 않고, 로비 화면의 주요 입력을 화면 단위 이벤트로 모읍니다.
public sealed class LobbyScreen : MonoBehaviour, ILobbyScreenView
{
    private const string DefaultFocusedCommandKey = "BottomButton_Spire"; // 로비 최초 진입 시 선택된 것으로 볼 기본 하단 탭
    private const string FocusObjectName = "Focus"; // 선택된 탭 표시 오브젝트 이름
    private const string AlertDotObjectName = "Alert_Dot_01_Red"; // 알림 점 오브젝트 이름

    private readonly Dictionary<string, Button> buttonsByCommandKey = new(); // 버튼 오브젝트 이름을 명령 키로 쓰는 버튼 캐시
    private readonly Dictionary<string, BottomTabIndicator> tabIndicatorsByCommandKey = new(); // 하단 탭 상태 표시 오브젝트 캐시
    private readonly List<ButtonBinding> activeBindings = new(); // OnDisable에서 제거할 런타임 버튼 연결 목록

    public event Action<string> CommandRequested; // 눌린 버튼 이름을 Controller로 전달하는 이벤트

    // 씬에 배치된 로비 버튼들을 이름 기반 명령으로 묶습니다.
    private void Awake()
    {
        RebuildButtonCache();
    }

    // 현재 활성화된 버튼들을 공통 명령 이벤트로 연결합니다.
    private void OnEnable()
    {
        RebuildButtonCache();
        RegisterCachedButtons();
    }

    // Unity가 씬 오브젝트를 모두 활성화한 뒤 한 번 더 버튼 연결을 갱신합니다.
    private void Start()
    {
        RebuildButtonCache();
        RegisterCachedButtons();
    }

    // View가 비활성화될 때 런타임에 연결한 버튼 이벤트를 해제합니다.
    private void OnDisable()
    {
        UnregisterActiveBindings();
    }

    // 로비 화면 View가 처리할 버튼을 하나 이상 찾았는지 확인합니다.
    public bool IsReady()
    {
        return buttonsByCommandKey.Count > 0;
    }

    // Controller가 특정 명령 버튼의 입력 가능 여부를 제어합니다.
    public void SetCommandInteractable(string commandKey, bool isInteractable)
    {
        if (string.IsNullOrWhiteSpace(commandKey))
            return;

        if (buttonsByCommandKey.TryGetValue(commandKey, out Button button) && button != null)
            button.interactable = isInteractable;
    }

    // 하단 탭 Focus는 하나만 켜지도록 전체 탭 캐시를 갱신합니다.
    public void SetFocusedCommand(string commandKey)
    {
        if (string.IsNullOrWhiteSpace(commandKey))
            return;

        if (!tabIndicatorsByCommandKey.ContainsKey(commandKey))
            return;

        foreach (KeyValuePair<string, BottomTabIndicator> pair in tabIndicatorsByCommandKey)
            pair.Value.SetFocus(pair.Key == commandKey);
    }

    // 컨텐츠 해금/업그레이드/수령 가능 같은 상태를 하단 탭 빨간 점으로 표시합니다.
    public void SetCommandAlertVisible(string commandKey, bool isVisible)
    {
        if (string.IsNullOrWhiteSpace(commandKey))
            return;

        if (tabIndicatorsByCommandKey.TryGetValue(commandKey, out BottomTabIndicator indicator))
            indicator.SetAlert(isVisible);
    }

    // 자식 Button을 모두 모아 오브젝트 이름을 명령 키로 등록합니다.
    private void RebuildButtonCache()
    {
        buttonsByCommandKey.Clear();
        tabIndicatorsByCommandKey.Clear();

        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null || string.IsNullOrWhiteSpace(button.name))
                continue;

            if (!buttonsByCommandKey.ContainsKey(button.name))
                buttonsByCommandKey.Add(button.name, button);

            BottomTabIndicator indicator = BottomTabIndicator.TryCreate(button.transform);
            if (indicator.HasAnyIndicator && !tabIndicatorsByCommandKey.ContainsKey(button.name))
                tabIndicatorsByCommandKey.Add(button.name, indicator);
        }

        if (buttonsByCommandKey.Count == 0)
            Debug.LogWarning("[LobbyScreen] 로비 고정 UI 아래에서 Button 컴포넌트를 찾지 못했습니다.");

        SetFocusedCommand(DefaultFocusedCommandKey);
    }

    // 캐시된 모든 버튼을 같은 방식으로 연결해 Controller가 버튼 이름만 받게 합니다.
    private void RegisterCachedButtons()
    {
        UnregisterActiveBindings();

        foreach (KeyValuePair<string, Button> pair in buttonsByCommandKey)
        {
            string commandKey = pair.Key;
            Button button = pair.Value;

            if (button == null)
                continue;

            PrepareButtonForRuntimeClick(button);

            UnityAction callback = () => HandleCommandClicked(commandKey);
            button.onClick.AddListener(callback);
            activeBindings.Add(new ButtonBinding(button, callback));
        }
    }

    // 수동 배치된 버튼의 Target Graphic이 비어 있으면 하위 Graphic으로 클릭 판정을 보정합니다.
    private void PrepareButtonForRuntimeClick(Button button)
    {
        if (button.targetGraphic != null)
            return;

        Graphic graphic = button.GetComponentInChildren<Graphic>(true);
        if (graphic == null)
            return;

        graphic.raycastTarget = true;
        button.targetGraphic = graphic;
    }

    // 로비 버튼 클릭을 버튼 이름 기반 명령으로 전달합니다.
    private void HandleCommandClicked(string commandKey)
    {
        if (CommandRequested == null)
        {
            Debug.LogError($"[LobbyScreen] {commandKey} 클릭을 처리할 Controller 구독자가 없습니다.");
            return;
        }

        SetFocusedCommand(commandKey);
        CommandRequested?.Invoke(commandKey);
    }

    // OnEnable에서 연결한 콜백을 모두 제거해 씬 재활성화 시 중복 호출을 막습니다.
    private void UnregisterActiveBindings()
    {
        for (int i = 0; i < activeBindings.Count; i++)
            activeBindings[i].Unregister();

        activeBindings.Clear();
    }

    // 버튼과 콜백을 한 쌍으로 보관해 정확히 같은 콜백만 제거합니다.
    private readonly struct ButtonBinding
    {
        private readonly Button button; // 이벤트를 붙인 버튼
        private readonly UnityAction callback; // 제거해야 할 콜백

        // 버튼과 콜백을 함께 저장합니다.
        public ButtonBinding(Button button, UnityAction callback)
        {
            this.button = button;
            this.callback = callback;
        }

        // Button이 살아 있을 때만 등록했던 콜백을 제거합니다.
        public void Unregister()
        {
            if (button != null && callback != null)
                button.onClick.RemoveListener(callback);
        }
    }

    // 버튼 하위의 Focus와 Alert_Dot_01_Red를 한 번 찾아 보관합니다.
    private readonly struct BottomTabIndicator
    {
        private readonly GameObject focusObject; // 선택된 탭 표시 오브젝트
        private readonly GameObject alertObject; // 컨텐츠 알림 점 오브젝트

        public bool HasAnyIndicator => focusObject != null || alertObject != null;

        private BottomTabIndicator(GameObject focusObject, GameObject alertObject)
        {
            this.focusObject = focusObject;
            this.alertObject = alertObject;
        }

        // 버튼 자식 이름 기준으로 상태 표시 오브젝트를 찾습니다.
        public static BottomTabIndicator TryCreate(Transform buttonRoot)
        {
            if (buttonRoot == null)
                return default;

            return new BottomTabIndicator(
                FindChildGameObject(buttonRoot, FocusObjectName),
                FindChildGameObject(buttonRoot, AlertDotObjectName));
        }

        // 선택 상태를 표시하거나 숨깁니다.
        public void SetFocus(bool isVisible)
        {
            if (focusObject != null && focusObject.activeSelf != isVisible)
                focusObject.SetActive(isVisible);
        }

        // 알림 점을 표시하거나 숨깁니다.
        public void SetAlert(bool isVisible)
        {
            if (alertObject != null && alertObject.activeSelf != isVisible)
                alertObject.SetActive(isVisible);
        }

        // 이름이 같은 하위 오브젝트를 깊이 우선으로 찾습니다.
        private static GameObject FindChildGameObject(Transform root, string objectName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == objectName)
                    return child.gameObject;

                GameObject found = FindChildGameObject(child, objectName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
