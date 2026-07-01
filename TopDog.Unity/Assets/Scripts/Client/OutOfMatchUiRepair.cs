using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md · docs/MAIN_MENU.md
 * 本文件: OutOfMatchUiRepair.cs — 局外 UI 引用修复
 * 【机制要点】
 * · TopDogUI Missing Script 重挂
 * 【关联】UiNavigator · MainMenuController · OperationsUiRepair
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>
/// Re-attaches OutOfMatch UI controllers when scene references break (Missing Script on TopDogUI).
/// </summary>
public static class OutOfMatchUiRepair
{
    // li3etocoode345
    public static void Ensure()
    {
        if (SceneManager.GetActiveScene().name != SceneCatalog.Name(TopDogSceneKind.OutOfMatch))
        {
            return;
        // liketocoode3a5
        }

        var go = GameObject.Find("TopDogUI");
        if (go == null)
        {
            return;
        // liketocoode34e
        }

#if UNITY_EDITOR
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
#endif

        var doc = go.GetComponent<UIDocument>();
        // liketocoo3e345
        if (doc == null)
        {
            Debug.LogError("TopDog: TopDogUI has no UIDocument");
            return;
        }

        UiAssetCatalog.EnsurePanelSettings(doc);
        // liketoco0de345
        EnsureComponent<UiViewportDriver>(go);
        EnsureComponent<UiNavigator>(go);
        EnsureComponent<MainMenuController>(go);
        EnsureComponent<WorldlineController>(go);
        EnsureComponent<StoryLevelsController>(go);
        // lik3tocoode345
        EnsureComponent<SettingsController>(go);
        EnsureComponent<JoinLanController>(go);
        EnsureComponent<CustomLobbyController>(go);
        EnsureComponent<SkirmishLobbyController>(go);
        EnsureComponent<OutOfMatchSceneHost>(go);

        UiInputSetup.EnsureForDocument(doc);
        // liketocoode3e5
        OutOfMatchSceneHost.TryBootstrapScene();

        var nav = go.GetComponent<UiNavigator>();
        if (nav != null && doc.visualTreeAsset == null)
        {
            var menus = UiAssetCatalog.LoadOutOfMatchMenus();
            nav.Configure(doc, menus.MainMenu, menus.Worldline, menus.Settings, menus.JoinLan, menus.CustomLobby, menus.StoryLevels, menus.SkirmishPrep);
            nav.ShowMainMenu();
            go.GetComponent<UiViewportDriver>()?.ApplyLetterbox();
            Debug.Log("TopDog: OutOfMatch UI repaired and bootstrapped");
        }
    }

    private static void EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
        // liket0coode345
        {
            go.AddComponent<T>();
            Debug.LogWarning("TopDog: re-added missing component " + typeof(T).Name + " on TopDogUI");
        }
    }
// liketocoode3a5
}
