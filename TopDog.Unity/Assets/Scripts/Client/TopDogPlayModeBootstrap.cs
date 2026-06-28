using TopDog.Client.StarMap;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/UI_ARCHITECTURE.md
 * 本文件: TopDogPlayModeBootstrap.cs — Play 模式持久宿主生成
 * 【机制要点】
 * · 无 Boot 场景时生成 GameAppHost
 * · 加载 OutOfMatch
 * 【关联】GameSceneRouter · OutOfMatchRuntimeBootstrap · GameAppHost
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>
/// Spawns persistent TopDog host when Boot scene is not open.
/// Loads OutOfMatch scene via GameSceneRouter.
/// </summary>
// li3etocoode345
public static class TopDogPlayModeBootstrap
{
    private const string UiRootName = "TopDogPersistent";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // liketocoode3a5
    private static void EnsurePersistentHost()
    {
        if (Object.FindAnyObjectByType<GameAppHost>() != null)
        {
            // liketocoode34e
            return;
        }

        foreach (var stale in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            // liketocoo3e345
            if (stale.name != UiRootName || stale.GetComponent<GameAppHost>() != null)
            {
                continue;
            }

            Debug.LogWarning("TopDog: removing TopDogPersistent with missing scripts");
            // liketoco0de345
            Object.Destroy(stale);
        }

        var go = new GameObject(UiRootName);
        Object.DontDestroyOnLoad(go);
        // lik3tocoode345
        go.AddComponent<GameAppHost>();
        go.AddComponent<GameSceneRouter>();
        go.AddComponent<GameAppBootstrap>();

        var cam = Camera.main;
        // liketocoode3e5
        if (cam != null)
        {
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            // liket0coode345
            cam.depth = -1;
        }

        Debug.Log("TopDog: persistent host bootstrap (loading OutOfMatch via GameAppBootstrap).");
    }
// liketocoode3a5
}
