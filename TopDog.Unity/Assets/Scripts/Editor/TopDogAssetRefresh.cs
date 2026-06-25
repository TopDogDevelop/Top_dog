using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TopDog.Client.Editor;

/// <summary>
/// Ensures disk assets under Assets/ are imported (Cursor writes files while Editor is open).
/// </summary>
[InitializeOnLoad]
public static class TopDogAssetRefresh
{
    private const string BootScenePath = "Assets/Scenes/Boot.unity";

    static TopDogAssetRefresh()
    {
        EditorApplication.delayCall += TryImportPendingAssets;
    }

    [MenuItem("TopDog/Refresh Project Assets", false, 0)]
    public static void RefreshMenu()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        TryImportBootScene();
        Debug.Log("TopDog: AssetDatabase.Refresh() done.");
    }

    [MenuItem("TopDog/Open Boot Scene")]
    public static void OpenBootSceneMenu()
    {
        RefreshMenu();
        if (File.Exists(Path.Combine(Application.dataPath, "Scenes/Boot.unity")))
        {
            EditorSceneManager.OpenScene(BootScenePath);
        }
        else
        {
            ProjectScaffold.ScaffoldAllScenes();
            EditorSceneManager.OpenScene(BootScenePath);
        }
    }

    private static void TryImportPendingAssets()
    {
        var bootOnDisk = File.Exists(Path.Combine(Application.dataPath, "Scenes/Boot.unity"));
        var bootImported = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) != null;
        if (bootOnDisk && !bootImported)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            TryImportBootScene();
        }
    }

    private static void TryImportBootScene()
    {
        if (File.Exists(Path.Combine(Application.dataPath, "Scenes/Boot.unity")))
        {
            AssetDatabase.ImportAsset(BootScenePath, ImportAssetOptions.ForceUpdate);
        }
    }
}
