using TopDog.Client.StarMap;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>
/// Spawns persistent TopDog host when Boot scene is not open.
/// Loads OutOfMatch scene via GameSceneRouter.
/// </summary>
public static class TopDogPlayModeBootstrap
{
    private const string UiRootName = "TopDogPersistent";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePersistentHost()
    {
        if (Object.FindAnyObjectByType<GameAppHost>() != null)
        {
            return;
        }

        foreach (var stale in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (stale.name != UiRootName || stale.GetComponent<GameAppHost>() != null)
            {
                continue;
            }

            Debug.LogWarning("TopDog: removing TopDogPersistent with missing scripts");
            Object.Destroy(stale);
        }

        var go = new GameObject(UiRootName);
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GameAppHost>();
        go.AddComponent<GameSceneRouter>();
        go.AddComponent<GameAppBootstrap>();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.depth = -1;
        }

        Debug.Log("TopDog: persistent host bootstrap (loading OutOfMatch via GameAppBootstrap).");
    }
}