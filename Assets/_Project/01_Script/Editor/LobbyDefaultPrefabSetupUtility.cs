using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// LobbyScene의 고정 UI를 프로젝트용 Lobby_Default 프리팹 기준으로 정리하는 에디터 유틸리티입니다.
[InitializeOnLoad]
public static class LobbyDefaultPrefabSetupUtility
{
    private const string LobbyScenePath = "Assets/_Project/00_Scenes/LobbyScene.unity";
    private const string LobbyDefaultPrefabPath = "Assets/_Project/03_Art/LobbyScene/Lobby_Default.prefab";
    private const string AutoSetupSessionKey = "NightfallSpire.LobbyDefaultPrefabSetupUtility.AutoSetupDone";

    // Unity가 스크립트를 다시 읽거나 LobbyScene을 열 때 로비 프리팹 배치를 자동 보정합니다.
    static LobbyDefaultPrefabSetupUtility()
    {
        EditorApplication.delayCall += TryAutoSetupOpenedLobbyScene;
        EditorSceneManager.sceneOpened += HandleSceneOpened;
    }

    // 메뉴에서 Lobby_Default 프리팹 기준 로비 UI를 강제로 다시 구성합니다.
    [MenuItem("Nightfall Spire/Setup/Lobby Default Prefab UI")]
    public static void SetupLobbyDefaultPrefabUI()
    {
        EditorSceneManager.OpenScene(LobbyScenePath);
        SetupCurrentLobbyScene();
    }

    // 현재 활성 씬이 LobbyScene이면 한 번 자동 구성합니다.
    private static void TryAutoSetupOpenedLobbyScene()
    {
        if (SessionState.GetBool(AutoSetupSessionKey, false))
            return;

        if (EditorSceneManager.GetActiveScene().path != LobbyScenePath)
            return;

        SessionState.SetBool(AutoSetupSessionKey, true);
        SetupCurrentLobbyScene();
    }

    // LobbyScene을 열 때마다 프리팹 기반 배치를 보정합니다.
    private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path != LobbyScenePath)
            return;

        SetupCurrentLobbyScene();
    }

    // LobbyScene의 기존 임시 UI를 걷어내고 Lobby_Default 프리팹을 고정 UI로 배치합니다.
    private static void SetupCurrentLobbyScene()
    {
        RectTransform staticCanvas = FindRequiredRect("Canvas_StaticUI");
        RectTransform staticRoot = FindOrCreateRect("LobbyStaticUIRoot", staticCanvas);
        StretchFull(staticRoot);

        RemoveLegacyStaticCanvasObjects(staticCanvas, staticRoot);
        RemoveOldLobbyStaticChildren(staticRoot);

        GameObject lobbyDefault = EnsureLobbyDefaultPrefab(staticRoot);
        RectTransform lobbyDefaultRect = lobbyDefault.GetComponent<RectTransform>();
        StretchFull(lobbyDefaultRect);

        SetupLobbyTexts(lobbyDefault);
        ConnectLobbyScreen(staticRoot, lobbyDefault);
        EnsureSceneRootReferences(staticRoot);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("[LobbyDefaultPrefabSetupUtility] Lobby_Default 프리팹 기준 로비 고정 UI 세팅 완료");
    }

    // Canvas_StaticUI 바로 아래에 남은 이전 임시 HUD와 로비 placeholder를 제거합니다.
    private static void RemoveLegacyStaticCanvasObjects(RectTransform staticCanvas, RectTransform staticRoot)
    {
        string[] legacyObjectNames =
        {
            "TopCurrencyHud",
            "GoalPanel",
            "LeftQuickMenu",
            "FightPanel",
            "FightButton",
            "SpireTowerArea"
        };

        for (int i = staticCanvas.childCount - 1; i >= 0; i--)
        {
            Transform child = staticCanvas.GetChild(i);

            if (child == staticRoot)
                continue;

            if (IsLegacyObjectName(child.name, legacyObjectNames))
                Object.DestroyImmediate(child.gameObject);
        }
    }

    // 삭제 대상 이름 목록에 현재 오브젝트 이름이 포함되어 있는지 확인합니다.
    private static bool IsLegacyObjectName(string objectName, string[] legacyObjectNames)
    {
        for (int i = 0; i < legacyObjectNames.Length; i++)
        {
            if (objectName == legacyObjectNames[i])
                return true;
        }

        return false;
    }

    // 이전 placeholder 방식으로 생긴 오브젝트와 기존 임시 고정 UI를 제거합니다.
    private static void RemoveOldLobbyStaticChildren(RectTransform staticRoot)
    {
        for (int i = staticRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = staticRoot.GetChild(i);

            if (child.name == "Lobby_Default")
                continue;

            Object.DestroyImmediate(child.gameObject);
        }
    }

    // Lobby_Default 프리팹 인스턴스를 보장합니다.
    private static GameObject EnsureLobbyDefaultPrefab(RectTransform parent)
    {
        Transform existing = parent.Find("Lobby_Default");

        if (existing != null)
            return existing.gameObject;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyDefaultPrefabPath);

        if (prefab == null)
            throw new MissingReferenceException($"{LobbyDefaultPrefabPath} 프리팹을 찾을 수 없습니다.");

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;

        if (instance == null)
            throw new MissingReferenceException("Lobby_Default 프리팹 인스턴스를 생성하지 못했습니다.");

        instance.name = "Lobby_Default";
        instance.layer = parent.gameObject.layer;
        return instance;
    }

    // 데모 프리팹 문구를 현재 프로젝트 로비 문구로 바꿉니다.
    private static void SetupLobbyTexts(GameObject lobbyDefault)
    {
        TMP_Text[] texts = lobbyDefault.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            string currentText = texts[i].text;

            if (currentText == "START")
                texts[i].text = "FIGHT";
            else if (currentText == "Battle 5")
                texts[i].text = "Week 1 Night 1";
            else if (currentText == "Hero's Arena")
                texts[i].text = "Nightfall Spire";
            else if (currentText == "Inventory")
                texts[i].text = "Heroes";
            else if (currentText == "Mission")
                texts[i].text = "Quest";
            else if (currentText == "AD Skip")
                texts[i].text = "Reward";
            else if (currentText.Contains("Layerlab"))
                texts[i].text = "Prepare your tower before nightfall.";
        }
    }

    // LobbyScreen이 Lobby_Default 안의 시작 버튼을 사용하도록 연결합니다.
    private static void ConnectLobbyScreen(RectTransform staticRoot, GameObject lobbyDefault)
    {
        LobbyScreen lobbyScreen = staticRoot.GetComponent<LobbyScreen>() ?? staticRoot.gameObject.AddComponent<LobbyScreen>();
        Button startButton = FindStartButton(lobbyDefault);

        if (startButton == null)
        {
            Debug.LogWarning("[LobbyDefaultPrefabSetupUtility] Lobby_Default 안에서 시작 버튼을 찾지 못했습니다.");
            return;
        }

        Image targetImage = startButton.targetGraphic as Image ?? startButton.GetComponent<Image>();
        SerializedObject serializedObject = new SerializedObject(lobbyScreen);
        serializedObject.FindProperty("nightDefenseStartButton").objectReferenceValue = startButton;
        serializedObject.FindProperty("nightDefenseStartButtonImage").objectReferenceValue = targetImage;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();

        LobbyStaticUIRoot staticUIRoot = staticRoot.GetComponent<LobbyStaticUIRoot>() ?? staticRoot.gameObject.AddComponent<LobbyStaticUIRoot>();
        SerializedObject serializedStaticRoot = new SerializedObject(staticUIRoot);
        serializedStaticRoot.FindProperty("currencyHudView").objectReferenceValue = null;
        serializedStaticRoot.FindProperty("lobbyScreen").objectReferenceValue = lobbyScreen;
        serializedStaticRoot.ApplyModifiedPropertiesWithoutUndo();
    }

    // 프리팹 안에서 전투 시작 버튼으로 쓸 Button을 찾고, 버튼 컴포넌트가 없으면 현재 씬 인스턴스에 보강합니다.
    private static Button FindStartButton(GameObject lobbyDefault)
    {
        Transform namedButton = FindChildByName(lobbyDefault.transform, "Button_03_Red");

        if (namedButton != null)
            return EnsureButtonComponent(namedButton);

        Button[] buttons = lobbyDefault.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == "Button_03_Red")
                return buttons[i];
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            TMP_Text text = buttons[i].GetComponentInChildren<TMP_Text>(true);

            if (text != null && (text.text == "START" || text.text == "FIGHT"))
                return buttons[i];
        }

        return null;
    }

    // 중첩 프리팹 안쪽까지 이름 기준으로 Transform을 찾습니다.
    private static Transform FindChildByName(Transform root, string objectName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }

    // 기존 프리팹의 Graphic을 최대한 유지하면서 클릭 가능한 Button 컴포넌트만 보장합니다.
    private static Button EnsureButtonComponent(Transform target)
    {
        Button button = target.GetComponent<Button>();

        if (button == null)
        {
            button = target.gameObject.AddComponent<Button>();
            Debug.Log("[LobbyDefaultPrefabSetupUtility] Button_03_Red에 Button 컴포넌트를 추가했습니다.");
        }

        Graphic targetGraphic = target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);

        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = true;
            button.targetGraphic = targetGraphic;
        }

        return button;
    }

    // LobbySceneRoot가 Static UI Root와 Dynamic UI Root를 참조하도록 보장합니다.
    private static void EnsureSceneRootReferences(RectTransform staticRoot)
    {
        LobbySceneRoot sceneRoot = Object.FindFirstObjectByType<LobbySceneRoot>(FindObjectsInactive.Include);
        LobbyStaticUIRoot staticUIRoot = staticRoot.GetComponent<LobbyStaticUIRoot>() ?? staticRoot.gameObject.AddComponent<LobbyStaticUIRoot>();
        LobbyDynamicUIRoot dynamicUIRoot = Object.FindFirstObjectByType<LobbyDynamicUIRoot>(FindObjectsInactive.Include);

        if (sceneRoot == null)
            return;

        SerializedObject serializedSceneRoot = new SerializedObject(sceneRoot);
        serializedSceneRoot.FindProperty("staticUIRoot").objectReferenceValue = staticUIRoot;
        serializedSceneRoot.FindProperty("dynamicUIRoot").objectReferenceValue = dynamicUIRoot;
        serializedSceneRoot.ApplyModifiedPropertiesWithoutUndo();
    }

    // 씬에 반드시 있어야 하는 RectTransform을 찾고 없으면 명확하게 실패시킵니다.
    private static RectTransform FindRequiredRect(string objectName)
    {
        GameObject gameObject = GameObject.Find(objectName);

        if (gameObject == null)
            throw new MissingReferenceException($"{objectName} 오브젝트를 LobbyScene에서 찾을 수 없습니다.");

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();

        if (rectTransform == null)
            throw new MissingComponentException($"{objectName} 오브젝트에 RectTransform이 없습니다.");

        return rectTransform;
    }

    // 지정한 부모 아래에서 RectTransform 오브젝트를 찾거나 생성합니다.
    private static RectTransform FindOrCreateRect(string objectName, RectTransform parent)
    {
        Transform child = parent.Find(objectName);
        GameObject gameObject;

        if (child == null)
        {
            gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            gameObject.transform.SetParent(parent, false);
        }
        else
        {
            gameObject = child.gameObject;
        }

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();

        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();

        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        return rectTransform;
    }

    // RectTransform을 부모 전체 영역에 맞춥니다.
    private static void StretchFull(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
