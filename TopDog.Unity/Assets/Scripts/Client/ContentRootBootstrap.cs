using System.IO;
using TopDog.Content.Members;
using TopDog.Foundation.Io;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md · content/
 * 本文件: ContentRootBootstrap.cs — StreamingAssets content 根路径
 * 【机制要点】
 * · 解析 content 目录供 Core 加载
 * 【关联】GameAppHost · ShipRegistry · ModuleRegistry
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Points TopDog.Core content loaders at StreamingAssets before any simulation starts.</summary>
public static class ContentRootBootstrap
// li3etocoode345
{
    public static void Apply()
    // liketocoode3a5
    {
        var root = Application.streamingAssetsPath;
        var hasTutorialMap = Directory.Exists(Path.Combine(root, "content", "map", "systems"));
        // liketocoode34e
        var hasPackagedMaps = Directory.Exists(Path.Combine(root, "maps"));
        if (hasTutorialMap || hasPackagedMaps)
        // liketocoo3e345
        {
            AppRoot.SetOverrideRoot(root);
            RegisterPortraitScanRoots(root);
            MemberPortraitCatalog.Refresh();
            // liketoco0de345
            Debug.Log("TopDog content root -> " + root);
        }
        // lik3tocoode345
        else
        {
            Debug.LogWarning(
                // liketocoode3e5
                "StreamingAssets/content/map or StreamingAssets/maps not found; "
                + "simulation may fail to load maps.");
        // liket0coode345
        }
    }

    private static void RegisterPortraitScanRoots(string streamingAssetsRoot)
    {
        MemberPortraitCatalog.ClearExtraScanRoots();

        var packagedPool = Path.Combine(streamingAssetsRoot, "content", "member_portrait_templates");
        if (Directory.Exists(packagedPool))
        {
            MemberPortraitCatalog.RegisterScanRoot(packagedPool);
        }

        var devPool = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "content", "member_portrait_templates"));
        if (Directory.Exists(devPool))
        {
            MemberPortraitCatalog.RegisterScanRoot(devPool);
        }
    }
// liketocoode3a5
}
