using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>
/// When OutOfMatch.unity is missing from Build Settings, spawn menu UI at runtime
/// (same layout as OutOfMatch scene) so Play from empty Boot is not a black screen.
/// </summary>
public static class OutOfMatchRuntimeBootstrap
{
    private const string UiRootName = "TopDogUI";

    public static bool Ensure()
    {
        if (Object.FindAnyObjectByType<UiNavigator>() != null)
        {
            return true;
        }

        var go = new GameObject(UiRootName);
        Object.DontDestroyOnLoad(go);
        var doc = go.AddComponent<UIDocument>();

        var panelSettings = UiAssetCatalog.LoadPanelSettings();
        if (panelSettings == null)
        {
            Debug.LogError("TopDog: DefaultPanelSettings missing — run TopDog → Scaffold All Scenes");
            ShowBootstrapError(doc, "缺少 DefaultPanelSettings\n菜单 TopDog → Scaffold All Scenes");
            return false;
        }

        doc.panelSettings = panelSettings;
        var mainMenu = UiAssetCatalog.LoadUxml("Assets/UI/MainMenu.uxml");
        if (mainMenu == null)
        {
            Debug.LogError("TopDog: MainMenu.uxml missing");
            ShowBootstrapError(doc, "缺少 MainMenu.uxml\n请确认 Assets/UI 已导入");
            return false;
        }

        doc.visualTreeAsset = mainMenu;

        go.AddComponent<UiViewportDriver>();
        var nav = go.AddComponent<UiNavigator>();
        go.AddComponent<MainMenuController>();
        go.AddComponent<WorldlineController>();
        go.AddComponent<SettingsController>();
        go.AddComponent<JoinLanController>();
        go.AddComponent<CustomLobbyController>();
        go.AddComponent<StoryLevelsController>();

        var menus = UiAssetCatalog.LoadOutOfMatchMenus();
        nav.Configure(
            doc,
            menus.MainMenu,
            menus.Worldline,
            menus.Settings,
            menus.JoinLan,
            menus.CustomLobby,
            menus.StoryLevels);

        UiTheme.ApplyDocument(doc);
        UiInputSetup.EnsureForDocument(doc);
        nav.ShowMainMenu();
        go.GetComponent<UiViewportDriver>()?.ApplyLetterbox();
        OutOfMatchSceneHost.TryBootstrapScene();

        Debug.LogWarning("TopDog: OutOfMatch scene not in build — using runtime menu fallback. Run TopDog → Scaffold All Scenes.");
        return true;
    }

    private static void ShowBootstrapError(UIDocument doc, string message)
    {
        doc.visualTreeAsset = null;
        var root = doc.rootVisualElement;
        if (root == null)
        {
            return;
        }
        root.Clear();
        UiTheme.ApplyOperationsRoot(root);
        var label = new Label(message);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.fontSize = 18;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.color = new Color(1f, 0.75f, 0.55f, 1f);
        label.style.flexGrow = 1;
        root.Add(label);
    }
}
