using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TopDog.Client;

/// <summary>
/// Re-attaches OutOfMatch UI controllers when scene references break (Missing Script on TopDogUI).
/// </summary>
public static class OutOfMatchUiRepair
{
    public static void Ensure()
    {
        if (SceneManager.GetActiveScene().name != SceneCatalog.Name(TopDogSceneKind.OutOfMatch))
        {
            return;
        }

        var go = GameObject.Find("TopDogUI");
        if (go == null)
        {
            return;
        }

#if UNITY_EDITOR
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
#endif

        var doc = go.GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("TopDog: TopDogUI has no UIDocument");
            return;
        }

        UiAssetCatalog.EnsurePanelSettings(doc);
        EnsureComponent<UiViewportDriver>(go);
        EnsureComponent<UiNavigator>(go);
        EnsureComponent<MainMenuController>(go);
        EnsureComponent<WorldlineController>(go);
        EnsureComponent<StoryLevelsController>(go);
        EnsureComponent<SettingsController>(go);
        EnsureComponent<JoinLanController>(go);
        EnsureComponent<CustomLobbyController>(go);
        EnsureComponent<OutOfMatchSceneHost>(go);

        UiInputSetup.EnsureForDocument(doc);
        OutOfMatchSceneHost.TryBootstrapScene();
    }

    private static void EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null)
        {
            go.AddComponent<T>();
            Debug.LogWarning("TopDog: re-added missing component " + typeof(T).Name + " on TopDogUI");
        }
    }
}
