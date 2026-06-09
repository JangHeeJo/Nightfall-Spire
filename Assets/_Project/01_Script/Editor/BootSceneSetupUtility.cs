using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// BootScene에 필요한 고정 로딩 UI를 실제 씬 오브젝트로 생성하고 연결하는 에디터 유틸리티입니다.
[InitializeOnLoad]
public static class BootSceneSetupUtility
{
    private const string BootScenePath = "Assets/_Project/00_Scenes/BootScene.unity";
    private const string LightBackgroundPath = "Assets/_Project/03_Art/BootScene/LightLoadingBg.png";
    private const string NightBackgroundPath = "Assets/_Project/03_Art/BootScene/NightLoadingBg.png";
    private const string AutoSetupSessionKey = "NightfallSpire.BootSceneSetupUtility.AutoSetupDone";

    // Unity가 스크립트를 다시 읽을 때 BootScene이 열려 있으면 고정 UI를 실제 씬에 바로 반영합니다.
    static BootSceneSetupUtility()
    {
        EditorApplication.delayCall += TryAutoSetupOpenedBootScene;
        EditorSceneManager.sceneOpened += HandleSceneOpened;
    }

    // 메뉴 또는 배치 실행에서 BootScene 로딩 UI를 구성합니다.
    [MenuItem("Nightfall Spire/Setup/Boot Loading UI")]
    public static void SetupBootLoadingUI()
    {
        EditorSceneManager.OpenScene(BootScenePath);
        SetupCurrentBootScene();
    }

    // 현재 열려 있는 씬이 BootScene이면 자동으로 고정 UI를 구성합니다.
    private static void TryAutoSetupOpenedBootScene()
    {
        if (SessionState.GetBool(AutoSetupSessionKey, false))
            return;

        if (EditorSceneManager.GetActiveScene().path != BootScenePath)
            return;

        SessionState.SetBool(AutoSetupSessionKey, true);
        SetupCurrentBootScene();
    }

    // BootScene을 나중에 다시 열어도 고정 UI 누락 상태를 자동으로 보정합니다.
    private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path != BootScenePath)
            return;

        SetupCurrentBootScene();
    }

    // 열려 있는 BootScene 안에 고정 로딩 UI 오브젝트를 만들고 BootLoadingView에 참조를 연결합니다.
    private static void SetupCurrentBootScene()
    {
        GameObject bootLoadingRoot = FindRequiredObject("BootLoadingRoot");
        BootLoadingView bootLoadingView = bootLoadingRoot.GetComponent<BootLoadingView>();

        if (bootLoadingView == null)
            bootLoadingView = bootLoadingRoot.AddComponent<BootLoadingView>();

        Image lightBackground = CreateOrUpdateBackground(bootLoadingRoot.transform, "LightBackground", LightBackgroundPath, 1f);
        Image nightBackground = CreateOrUpdateBackground(bootLoadingRoot.transform, "NightBackground", NightBackgroundPath, 0f);

        RectTransform loadingBarContainer = CreateOrUpdateRect("LoadingBarContainer", bootLoadingRoot.transform);
        loadingBarContainer.anchorMin = new Vector2(0.5f, 0f);
        loadingBarContainer.anchorMax = new Vector2(0.5f, 0f);
        loadingBarContainer.pivot = new Vector2(0.5f, 0.5f);
        loadingBarContainer.anchoredPosition = new Vector2(0f, 260f);
        loadingBarContainer.sizeDelta = new Vector2(760f, 120f);

        Image loadingBarFrame = CreateOrUpdateImage("LoadingBarFrame", loadingBarContainer, new Color(0.08f, 0.09f, 0.11f, 0.88f));
        RectTransform loadingBarFrameRect = loadingBarFrame.rectTransform;
        loadingBarFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadingBarFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadingBarFrameRect.pivot = new Vector2(0.5f, 0.5f);
        loadingBarFrameRect.anchoredPosition = Vector2.zero;
        loadingBarFrameRect.sizeDelta = new Vector2(560f, 34f);
        loadingBarFrame.type = Image.Type.Sliced;

        Image loadingBarFill = CreateOrUpdateImage("LoadingBarFill", loadingBarFrameRect, new Color(0.25f, 0.86f, 0.42f, 1f));
        RectTransform loadingBarFillRect = loadingBarFill.rectTransform;
        loadingBarFillRect.anchorMin = Vector2.zero;
        loadingBarFillRect.anchorMax = Vector2.one;
        loadingBarFillRect.pivot = new Vector2(0.5f, 0.5f);
        loadingBarFillRect.anchoredPosition = Vector2.zero;
        loadingBarFillRect.sizeDelta = new Vector2(-10f, -10f);
        loadingBarFill.type = Image.Type.Filled;
        loadingBarFill.fillMethod = Image.FillMethod.Horizontal;
        loadingBarFill.fillOrigin = 0;
        loadingBarFill.fillAmount = 0f;

        TMP_Text statusText = CreateOrUpdateText("StatusText", loadingBarContainer, "Starting...", 28f, new Vector2(0f, 48f), new Vector2(640f, 36f), Color.white);
        TMP_Text percentText = CreateOrUpdateText("PercentText", loadingBarContainer, "0%", 24f, Vector2.zero, new Vector2(140f, 30f), Color.white);
        TMP_Text versionText = CreateOrUpdateText("VersionText", loadingBarContainer, "v0.1", 18f, new Vector2(0f, -46f), new Vector2(280f, 28f), new Color(1f, 1f, 1f, 0.72f));

        ConnectBootLoadingView(bootLoadingView, lightBackground, nightBackground, loadingBarContainer, loadingBarFill, statusText, percentText, versionText);

        loadingBarContainer.SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("[BootSceneSetupUtility] BootScene 로딩 고정 UI 생성 및 연결 완료");
    }

    // 지정한 이름의 오브젝트를 찾고 없으면 명확하게 실패시킵니다.
    private static GameObject FindRequiredObject(string objectName)
    {
        GameObject gameObject = GameObject.Find(objectName);

        if (gameObject == null)
            throw new MissingReferenceException($"{objectName} 오브젝트를 BootScene에서 찾을 수 없습니다.");

        return gameObject;
    }

    // 배경 이미지를 생성하거나 기존 오브젝트를 재사용합니다.
    private static Image CreateOrUpdateBackground(Transform parent, string objectName, string spritePath, float alpha)
    {
        Image image = CreateOrUpdateImage(objectName, parent, new Color(1f, 1f, 1f, alpha));
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        return image;
    }

    // UI Image 오브젝트를 생성하거나 기존 오브젝트를 재사용합니다.
    private static Image CreateOrUpdateImage(string objectName, Transform parent, Color color)
    {
        RectTransform rectTransform = CreateOrUpdateRect(objectName, parent);
        Image image = rectTransform.GetComponent<Image>();

        if (image == null)
            image = rectTransform.gameObject.AddComponent<Image>();

        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // TextMeshPro UI 텍스트를 생성하거나 기존 오브젝트를 재사용합니다.
    private static TMP_Text CreateOrUpdateText(string objectName, Transform parent, string text, float fontSize, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        RectTransform rectTransform = CreateOrUpdateRect(objectName, parent);
        TextMeshProUGUI textComponent = rectTransform.GetComponent<TextMeshProUGUI>();

        if (textComponent == null)
            textComponent = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.raycastTarget = false;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;

        return textComponent;
    }

    // RectTransform 오브젝트를 생성하거나 기존 오브젝트를 재사용합니다.
    private static RectTransform CreateOrUpdateRect(string objectName, Transform parent)
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

    // BootLoadingView의 private serialized field들을 실제 씬 UI 참조로 연결합니다.
    private static void ConnectBootLoadingView(
        BootLoadingView bootLoadingView,
        Image lightBackground,
        Image nightBackground,
        RectTransform loadingBarContainer,
        Image loadingBarFill,
        TMP_Text statusText,
        TMP_Text percentText,
        TMP_Text versionText)
    {
        SerializedObject serializedObject = new SerializedObject(bootLoadingView);
        serializedObject.FindProperty("lightBackgroundImage").objectReferenceValue = lightBackground;
        serializedObject.FindProperty("nightBackgroundImage").objectReferenceValue = nightBackground;
        serializedObject.FindProperty("backgroundChangeProgress").floatValue = 0.5f;
        serializedObject.FindProperty("backgroundFadeSeconds").floatValue = 1.2f;
        serializedObject.FindProperty("loadingBarContainer").objectReferenceValue = loadingBarContainer;
        serializedObject.FindProperty("progressSlider").objectReferenceValue = null;
        serializedObject.FindProperty("progressFillImage").objectReferenceValue = loadingBarFill;
        serializedObject.FindProperty("statusText").objectReferenceValue = statusText;
        serializedObject.FindProperty("percentText").objectReferenceValue = percentText;
        serializedObject.FindProperty("versionText").objectReferenceValue = versionText;
        serializedObject.FindProperty("progressTweenSeconds").floatValue = 0.45f;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
