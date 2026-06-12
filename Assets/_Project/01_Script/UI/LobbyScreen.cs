using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 로비 화면 전체를 대표하는 View입니다.
// 개별 버튼마다 스크립트를 만들지 않고, 로비 화면의 주요 입력을 화면 단위 이벤트로 모읍니다.
public sealed class LobbyScreen : MonoBehaviour, ILobbyScreenView
{
    private readonly Dictionary<string, Button> buttonsByCommandKey = new(); // 버튼 오브젝트 이름을 명령 키로 쓰는 버튼 캐시
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

    // 자식 Button을 모두 모아 오브젝트 이름을 명령 키로 등록합니다.
    private void RebuildButtonCache()
    {
        buttonsByCommandKey.Clear();

        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null || string.IsNullOrWhiteSpace(button.name))
                continue;

            if (!buttonsByCommandKey.ContainsKey(button.name))
                buttonsByCommandKey.Add(button.name, button);
        }

        if (buttonsByCommandKey.Count == 0)
            Debug.LogWarning("[LobbyScreen] 로비 고정 UI 아래에서 Button 컴포넌트를 찾지 못했습니다.");
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
}
