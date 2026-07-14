using System;
using System.IO;
using System.Reflection;
using TopDog.Net.Lan;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/RELEASE_AND_HOTUPDATE.md §2.7 · §5
 * 本文件: HybridClrHotLoader.cs — 从 content_runtime/hotupdate/<platform> 加载热更 DLL
 * 【机制要点】
 * · Editor / 未启用 HybridCLR：TopDog.Hot 随 AOT 进包，只反射调用入口
 * · HybridCLR 启用后的 Player：补 AOT 元数据再 Assembly.Load
 * · hotfix 按平台分仓（倒Y 分端）；缺平台目录时回退旧平铺 hotupdate/ 并警告
 * · 客户端下载见 OnlineUpdateClient.IsManifestPathWantedForThisPlatform（不对端拉 DLL）
 * · shellCompatibilityId 不匹配则跳过热更 DLL（content 仍可用）
 * 【关联】OnlineUpdateClient · HotRuntime · GameAppBootstrap · docs/RELEASE_AND_HOTUPDATE.md §倒Y
 * ══
 */

namespace TopDog.Client.OnlineUpdate;

public static class HybridClrHotLoader
{
    public const string HotUpdateDirName = "hotupdate";
    public const string HotAssemblyFileName = "TopDog.Hot.dll";
    public const string ShellCompatibilityFileName = "shellCompatibilityId.txt";

    /// <summary>Must match publish_online_update.ps1 -ShellCompatibilityId default.</summary>
    public const string ExpectedShellCompatibilityId = "topdog-unity-6000.3.19f1-hc8.12";

    public static string HotUpdateRuntimeRoot =>
        Path.Combine(OnlineUpdateClient.ContentRuntimeRoot, HotUpdateDirName);

    public static string PlatformFolderName
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return "android";
#elif UNITY_STANDALONE_WIN && !UNITY_EDITOR
            return "windows-x64";
#else
            return "windows-x64";
#endif
        }
    }

    public static string PlatformHotUpdateRoot =>
        Path.Combine(HotUpdateRuntimeRoot, PlatformFolderName);

    private static readonly string[] PreferAotMetadataAssemblies =
    {
        "mscorlib.dll",
        "System.dll",
        "System.Core.dll",
        "TopDog.Core.dll",
        "TopDog.Client.dll",
    };

    public static void LoadAfterContentReady()
    {
        try
        {
#if !UNITY_EDITOR
            TryLoadHybridClrHotAssembly();
#endif
            InvokeHotEntry();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>Resolve platform hotupdate dir; fall back to legacy flat hotupdate/.</summary>
    public static string ResolveHotUpdateLoadRoot(out bool usedLegacyFlat)
    {
        usedLegacyFlat = false;
        var platformRoot = PlatformHotUpdateRoot;
        var platformDll = Path.Combine(platformRoot, HotAssemblyFileName);
        if (File.Exists(platformDll) || Directory.Exists(Path.Combine(platformRoot, "aot")))
        {
            return platformRoot;
        }

        var legacyDll = Path.Combine(HotUpdateRuntimeRoot, HotAssemblyFileName);
        if (File.Exists(legacyDll) || Directory.Exists(Path.Combine(HotUpdateRuntimeRoot, "aot")))
        {
            usedLegacyFlat = true;
            Debug.LogWarning(
                "TopDog HybridCLR: using legacy flat hotupdate/ — republish with platform split "
                + "(hotupdate/" + PlatformFolderName + "/)");
            return HotUpdateRuntimeRoot;
        }

        return platformRoot;
    }

#if !UNITY_EDITOR
    private static void TryLoadHybridClrHotAssembly()
    {
        // HybridCLR.Runtime may be present while native libil2cpp is stock (Unity 6000.5 + HC 8.12).
        // Use reflection and soft-fail so AOT-packed TopDog.Hot still boots.
        var runtimeApi = Type.GetType("HybridCLR.RuntimeApi, HybridCLR.Runtime");
        if (runtimeApi == null)
        {
            Debug.Log("TopDog HybridCLR: RuntimeApi not present — using AOT TopDog.Hot if packed");
            return;
        }

        var loadRoot = ResolveHotUpdateLoadRoot(out _);
        if (!IsShellCompatible(loadRoot))
        {
            Debug.LogWarning(
                "TopDog HybridCLR: shellCompatibilityId mismatch — skip hot DLL, keep shared content. "
                + "Expected=" + ExpectedShellCompatibilityId);
            return;
        }

        var loadMeta = runtimeApi.GetMethod(
            "LoadMetadataForAOTAssembly",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(byte[]), Type.GetType("HybridCLR.HomologousImageMode, HybridCLR.Runtime")! },
            null);
        if (loadMeta == null)
        {
            Debug.LogWarning("TopDog HybridCLR: LoadMetadataForAOTAssembly not found");
            return;
        }

        var modeType = Type.GetType("HybridCLR.HomologousImageMode, HybridCLR.Runtime");
        var superSet = Enum.Parse(modeType!, "SuperSet");
        var metaRoot = Path.Combine(loadRoot, "aot");
        if (Directory.Exists(metaRoot))
        {
            foreach (var name in PreferAotMetadataAssemblies)
            {
                var path = Path.Combine(metaRoot, name);
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    var bytes = File.ReadAllBytes(path);
                    var err = loadMeta.Invoke(null, new object[] { bytes, superSet });
                    Debug.Log("TopDog HybridCLR: LoadMetadata " + name + " -> " + err);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("TopDog HybridCLR: LoadMetadata failed for " + name + ": " + e.Message);
                    return;
                }
            }
        }

        var dllPath = Path.Combine(loadRoot, HotAssemblyFileName);
        if (!File.Exists(dllPath))
        {
            Debug.Log("TopDog HybridCLR: hot dll missing at " + dllPath + " (AOT assembly if packed)");
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(dllPath);
            var pdb = dllPath + ".pdb";
            byte[] pdbBytes = File.Exists(pdb) ? File.ReadAllBytes(pdb) : null;
            Assembly.Load(bytes, pdbBytes);
            Debug.Log("TopDog HybridCLR: loaded " + dllPath + " (" + bytes.Length + " bytes)");
        }
        catch (Exception e)
        {
            Debug.LogWarning("TopDog HybridCLR: Assembly.Load failed (need HybridCLR-enabled shell): " + e.Message);
        }
    }

    private static bool IsShellCompatible(string loadRoot)
    {
        var path = Path.Combine(loadRoot, ShellCompatibilityFileName);
        if (!File.Exists(path))
        {
            // Legacy packages without the file: allow load but warn once.
            Debug.LogWarning("TopDog HybridCLR: no shellCompatibilityId.txt in " + loadRoot);
            return true;
        }

        try
        {
            var remote = File.ReadAllText(path).Trim();
            return string.Equals(remote, ExpectedShellCompatibilityId, StringComparison.Ordinal);
        }
        catch (Exception e)
        {
            Debug.LogWarning("TopDog HybridCLR: failed reading shellCompatibilityId: " + e.Message);
            return false;
        }
    }
#endif

    private static void InvokeHotEntry()
    {
        var type = Type.GetType("TopDog.Hot.HotRuntime, TopDog.Hot")
                   ?? Type.GetType("TopDog.Hot.HotRuntime");
        if (type == null)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType("TopDog.Hot.HotRuntime");
                if (type != null)
                {
                    break;
                }
            }
        }

        if (type == null)
        {
            Debug.LogWarning("TopDog HybridCLR: HotRuntime type not found");
            return;
        }

        var method = type.GetMethod("NotifyBootReady", BindingFlags.Public | BindingFlags.Static);
        method?.Invoke(null, new object[] { ContentVersionGate.Current ?? "" });
    }
}
