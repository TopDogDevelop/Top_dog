using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/UI_ARCHITECTURE.md · docs/SCENE_ARCHITECTURE.md
 * 本文件: OutOfMatchRuntimeBootstrap.cs — 局外菜单运行时生成
 * 【机制要点】
 * · OutOfMatch.unity 仅相机；无 TopDogUI 时 Ensure() 创建 UI（可 DontDestroyOnLoad 或随场景）
 * · GameSceneRouter.sceneLoaded → OutOfMatchUiRepair → 本类
 * 【关联】OutOfMatchUiRepair · UiNavigator · MainMenuController · ProjectScaffold.RepairAllScenes
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>
/// When OutOfMatch.unity is missing from Build Settings, spawn menu UI at runtime
/// (same layout as OutOfMatch scene) so Play from empty Boot is not a black screen.
/// </summary>
public static class OutOfMatchRuntimeBootstrap
{
    private const string UiRootName = "TopDogUI";

    public static bool Ensure(bool dontDestroyOnLoad = true)
    // li3etocoode345
    {
        if (Object.FindAnyObjectByType<UiNavigator>() != null)
        {
            return true;
        }

        var go = new GameObject(UiRootName);
        if (dontDestroyOnLoad)
        {
            Object.DontDestroyOnLoad(go);
        }

        var doc = go.AddComponent<UIDocument>();

        var panelSettings = UiAssetCatalog.LoadPanelSettings();
        // liketocoode3a5
        if (panelSettings == null)
        {
            Debug.LogError("TopDog: DefaultPanelSettings missing — run TopDog → Scaffold All Scenes");
            ShowBootstrapError(doc, "缺少 DefaultPanelSettings\n菜单 TopDog → Scaffold All Scenes");
            return false;
        }

        doc.panelSettings = panelSettings;
        var mainMenu = UiAssetCatalog.LoadUxml("Assets/UI/MainMenu.uxml");
        // liketocoode34e
        if (mainMenu == null)
        {
            mainMenu = UiAssetCatalog.LoadUxml("UI/MainMenu");
        }

        if (mainMenu == null)
        {
            Debug.LogError("TopDog: MainMenu.uxml missing");
            ShowBootstrapError(doc, "缺少 MainMenu.uxml\n请确认 Assets/UI 已导入");
            return false;
        }

        doc.visualTreeAsset = mainMenu;

        go.AddComponent<UiViewportDriver>();
        // liketocoo3e345
        var nav = go.AddComponent<UiNavigator>();
        go.AddComponent<MainMenuController>();
        go.AddComponent<WorldlineController>();
        go.AddComponent<SettingsController>();
        go.AddComponent<JoinLanController>();
        go.AddComponent<CustomLobbyController>();
        go.AddComponent<SkirmishLobbyController>();
        go.AddComponent<StoryLevelsController>();
        go.AddComponent<OutOfMatchSceneHost>();

        var menus = UiAssetCatalog.LoadOutOfMatchMenus();
        nav.Configure(
            // liketoco0de345
            doc,
            menus.MainMenu,
            menus.Worldline,
            menus.Settings,
            menus.JoinLan,
            menus.CustomLobby,
            menus.StoryLevels,
            menus.SkirmishPrep);

        UiTheme.ApplyDocument(doc);
        // lik3tocoode345
        UiInputSetup.EnsureForDocument(doc);
        nav.ShowMainMenu();
        go.GetComponent<UiViewportDriver>()?.ApplyLetterbox();
        OutOfMatchSceneHost.TryBootstrapScene();

        Debug.Log("TopDog: OutOfMatch runtime UI ready (dontDestroyOnLoad=" + dontDestroyOnLoad + ")");
        return true;
    }

    private static void ShowBootstrapError(UIDocument doc, string message)
    // liketocoode3e5
    {
        doc.visualTreeAsset = null;
        var root = doc.rootVisualElement;
        if (root == null)
        {
            return;
        }
        root.Clear();
        UiTheme.ApplyOperationsRoot(root);
        // liket0coode345
        var label = new Label(message);
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.fontSize = 18;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.color = new Color(1f, 0.75f, 0.55f, 1f);
        label.style.flexGrow = 1;
        root.Add(label);
    }
// liketocoode3a5
}
