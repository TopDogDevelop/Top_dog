using System.IO;
using TopDog.Client.OnlineUpdate;
using TopDog.Content.Members;
using TopDog.Foundation.Io;
using TopDog.Net.Lan;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md · docs/ONLINE_UPDATE.md · docs/RELEASE_AND_HOTUPDATE.md §Android StreamingAssets
 * 本文件: ContentRootBootstrap.cs — StreamingAssets / content_runtime / Android 解压基线
 * 【机制要点】
 * · Editor：始终 StreamingAssets
 * · 热更 content_runtime：须含 map/systems 且含 starting_templates+assets，否则回退包内基线
 * · Android jar: StreamingAssets → StreamingAssetsBaseline 解压后再 SetOverrideRoot
 * 【关联】GameAppHost · OnlineUpdateClient · StreamingAssetsBaseline · ShipRegistry
 * ══
 */

namespace TopDog.Client;

/// <summary>Points TopDog.Core content loaders at StreamingAssets or hot-update runtime root.</summary>
public static class ContentRootBootstrap
{
    public static void Apply()
    {
        OnlineUpdateClient.SyncGateFromDisk();

#if UNITY_EDITOR
        // Editor：始终用 StreamingAssets，避免 LocalLow content_runtime 旧文件挡住本地 JSON 改动
        // （热更根只在首次填缺失文件，不会覆盖已有 scenario，导致「改了未实装」）。
        UsePackageContentRoot(Application.streamingAssetsPath);
        return;
#endif

        var packageRoot = StreamingAssetsBaseline.ResolveReadableRoot();
        var runtime = OnlineUpdateClient.ContentRuntimeRoot;
        var runtimeSystems = Path.Combine(runtime, "content", "map", "systems");
        var runtimeUsable = Directory.Exists(runtimeSystems)
            && File.Exists(OnlineUpdateClient.AppliedVersionPath)
            && StreamingAssetsBaseline.HasLobbyTemplates(runtime);

        if (runtimeUsable)
        {
            AppRoot.SetOverrideRoot(runtime);
            RegisterPortraitScanRoots(runtime, packageRoot ?? runtime);
            RegisterMapsScanRoots(runtime, packageRoot ?? runtime);
            MemberPortraitCatalog.Refresh();
            Debug.Log("TopDog content root (runtime) -> " + runtime + " @ " + ContentVersionGate.Current);
            return;
        }

        if (!string.IsNullOrEmpty(packageRoot))
        {
            UsePackageContentRoot(packageRoot);
            if (Directory.Exists(runtimeSystems) && File.Exists(OnlineUpdateClient.AppliedVersionPath))
            {
                Debug.LogWarning(
                    "content_runtime exists but missing starting_templates/assets; "
                    + "using package baseline instead: " + packageRoot);
            }

            return;
        }

        Debug.LogWarning(
            "No readable content root (StreamingAssets / streaming_baseline); "
            + "maps and lobby templates will be empty.");
    }

    private static void UsePackageContentRoot(string root)
    {
        if (!StreamingAssetsBaseline.IsReadablePackageRoot(root)
            && !Directory.Exists(Path.Combine(root, "content")))
        {
            Debug.LogWarning(
                "Package content root unreadable: " + root);
            return;
        }

        AppRoot.SetOverrideRoot(root);
        RegisterPortraitScanRoots(root, root);
        RegisterMapsScanRoots(root, root);
        MemberPortraitCatalog.Refresh();
        Debug.Log("TopDog content root -> " + root + " @ " + ContentVersionGate.Current);
    }

    private static void RegisterMapsScanRoots(string primaryRoot, string streamingOrPackageRoot)
    {
        AppRoot.ClearExtraMapsRoots();
        AppRoot.RegisterMapsRoot(Path.Combine(primaryRoot, "maps"));
        AppRoot.RegisterMapsRoot(Path.Combine(streamingOrPackageRoot, "maps"));
        AppRoot.RegisterMapsRoot(Path.Combine(OnlineUpdateClient.ContentRuntimeRoot, "maps"));
    }

    private static void RegisterPortraitScanRoots(string primaryRoot, string streamingOrPackageRoot)
    {
        MemberPortraitCatalog.ClearExtraScanRoots();

        var packagedPool = Path.Combine(primaryRoot, "content", "member_portrait_templates");
        if (Directory.Exists(packagedPool))
        {
            MemberPortraitCatalog.RegisterScanRoot(packagedPool);
        }

        var streamingPool = Path.Combine(streamingOrPackageRoot, "content", "member_portrait_templates");
        if (Directory.Exists(streamingPool))
        {
            MemberPortraitCatalog.RegisterScanRoot(streamingPool);
        }

        var runtimePool = Path.Combine(
            OnlineUpdateClient.ContentRuntimeRoot, "content", "member_portrait_templates");
        if (Directory.Exists(runtimePool))
        {
            MemberPortraitCatalog.RegisterScanRoot(runtimePool);
        }

        var devPool = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "content", "member_portrait_templates"));
        if (Directory.Exists(devPool))
        {
            MemberPortraitCatalog.RegisterScanRoot(devPool);
        }
    }
}
