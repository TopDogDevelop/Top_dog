using TopDog.Client.StarMap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>
/// Re-attaches Operations UI when scene script refs break (Missing Script on TopDogUI).
/// </summary>
public static class OperationsUiRepair
{
    public static void Ensure()
    {
        if (SceneManager.GetActiveScene().name != SceneCatalog.Name(TopDogSceneKind.Operations))
        {
            return;
        }

        var go = GameObject.Find("TopDogUI");
        if (go == null)
        {
            return;
        }

        var doc = go.GetComponent<UIDocument>();
        if (doc == null)
        {
            Debug.LogError("TopDog: TopDogUI has no UIDocument");
            return;
        }

        UiAssetCatalog.EnsurePanelSettings(doc);
        EnsureComponent<UiViewportDriver>(go);
        EnsureComponent<CampaignShellController>(go);
        EnsureComponent<StarMapHostController>(go);
        EnsureComponent<OperationsSceneHost>(go);

        UiInputSetup.EnsureForDocument(doc);
        OperationsSceneHost.TryBootstrapScene();
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
