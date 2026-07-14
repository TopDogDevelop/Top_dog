using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/UI_ARCHITECTURE.md · docs/RELEASE_AND_HOTUPDATE.md
 * 本文件: TopDogPlayModeBootstrap.cs — Play 模式持久宿主生成
 * 【机制要点】
 * · 以 GameAppHost.Instance 为准；有 Bootstrap/Router 但无有效 Host 时补建
 * · 加载 OutOfMatch 由 GameAppBootstrap 负责
 * 【关联】GameSceneRouter · OutOfMatchRuntimeBootstrap · GameAppHost
 * ══
 */

namespace TopDog.Client;

/// <summary>
/// Spawns persistent TopDog host when Boot scene did not provide a usable host.
/// </summary>
public static class TopDogPlayModeBootstrap
{
    private const string UiRootName = "TopDogPersistent";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePersistentHost()
    {
        if (GameAppHost.Instance != null)
        {
            return;
        }

        // Component may exist while Instance was never set (Awake failed mid-way).
        var orphanHost = Object.FindAnyObjectByType<GameAppHost>();
        if (orphanHost != null)
        {
            GameAppHost.EnsureAlive();
            return;
        }

        foreach (var stale in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (stale.name != UiRootName)
            {
                continue;
            }

            if (stale.GetComponent<GameAppHost>() != null
                || stale.GetComponent<GameAppBootstrap>() != null
                || stale.GetComponent<GameSceneRouter>() != null)
            {
                // Persistent shell exists but Instance missing — attach Host + boot path.
                if (stale.GetComponent<GameAppHost>() == null)
                {
                    stale.AddComponent<GameAppHost>();
                }

                if (stale.GetComponent<GameSceneRouter>() == null)
                {
                    stale.AddComponent<GameSceneRouter>();
                }

                // Without Bootstrap, Play stays on empty Boot (black screen).
                if (stale.GetComponent<GameAppBootstrap>() == null)
                {
                    stale.AddComponent<GameAppBootstrap>();
                }

                GameAppHost.EnsureAlive();
                Debug.Log("TopDog: repaired TopDogPersistent (Host/Router/Bootstrap).");
                return;
            }

            Debug.LogWarning("TopDog: removing empty TopDogPersistent shell (no host components)");
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
