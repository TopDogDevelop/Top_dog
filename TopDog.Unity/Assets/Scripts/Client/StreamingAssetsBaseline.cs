using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/RELEASE_AND_HOTUPDATE.md §倒Y · §Android StreamingAssets · docs/ONLINE_UPDATE.md · docs/TACTICAL_VIEW.md
 * 本文件: StreamingAssetsBaseline.cs — Android 将 APK 内 assets/ 解压到 persistentDataPath
 * 【机制要点】
 * · Android 上 streamingAssetsPath 为 jar:file://…，Directory/File.Exists 不可用
 * · 首次（或换壳）把 assets/{content,maps,art} 抽到 persistentDataPath/streaming_baseline
 * · stamp 须含 lobby 模版 + art/combat_backgrounds，否则重解压（天空盒依赖 art）
 * · ContentRootBootstrap / ClientArtPaths 走解压目录即与 PC 相同枚举逻辑
 * 【关联】ContentRootBootstrap · ClientArtPaths · OnlineUpdateClient
 * ══
 */

namespace TopDog.Client;

/// <summary>
/// Android：APK StreamingAssets → 可 <c>Directory.Enumerate*</c> 的持久化基线。
/// 其它平台直接返回 <see cref="Application.streamingAssetsPath"/>。
/// </summary>
public static class StreamingAssetsBaseline
{
    public const string ExtractFolderName = "streaming_baseline";
    private const string StampFileName = ".apk_extract_stamp";

    /// <summary>
    /// 返回可用作 AppRoot 的包内内容根（含 content/map、starting_*、maps）。
    /// Android 不可直读时解压；失败返回 null。
    /// </summary>
    public static string? ResolveReadableRoot()
    {
        var sa = Application.streamingAssetsPath;
        if (IsReadablePackageRoot(sa))
        {
            return sa;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            return EnsureExtractedFromApk();
        }
        catch (Exception e)
        {
            Debug.LogError("StreamingAssetsBaseline extract failed: " + e.Message);
            return null;
        }
#else
        return IsReadablePackageRoot(sa) ? sa : null;
#endif
    }

    public static bool IsReadablePackageRoot(string? root)
    {
        if (string.IsNullOrWhiteSpace(root) || root.StartsWith("jar:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Directory.Exists(Path.Combine(root, "content", "map", "systems"))
            || Directory.Exists(Path.Combine(root, "maps"));
    }

    public static bool HasLobbyTemplates(string root) =>
        Directory.Exists(Path.Combine(root, "content", "starting_templates"))
        && Directory.Exists(Path.Combine(root, "content", "starting_assets"));

    public static bool HasCombatArt(string root) =>
        Directory.Exists(Path.Combine(root, "art", "combat_backgrounds", "Main"))
        || Directory.Exists(Path.Combine(root, "art", "CombatBackgrounds", "Main"));

#if UNITY_ANDROID && !UNITY_EDITOR
    private static string EnsureExtractedFromApk()
    {
        var dest = Path.Combine(Application.persistentDataPath, ExtractFolderName);
        var stampPath = Path.Combine(dest, StampFileName);
        var apkPath = Application.dataPath;
        if (string.IsNullOrEmpty(apkPath) || !File.Exists(apkPath))
        {
            throw new FileNotFoundException("Android dataPath APK missing", apkPath);
        }

        var fi = new FileInfo(apkPath);
        var stamp = $"{Application.version}|{fi.Length}|{fi.LastWriteTimeUtc.Ticks}";
        if (File.Exists(stampPath)
            && string.Equals(File.ReadAllText(stampPath).Trim(), stamp, StringComparison.Ordinal)
            && IsReadablePackageRoot(dest)
            && HasLobbyTemplates(dest)
            && HasCombatArt(dest))
        {
            return dest;
        }

        if (Directory.Exists(dest))
        {
            Directory.Delete(dest, recursive: true);
        }

        Directory.CreateDirectory(dest);
        var extracted = 0;
        using (var zip = ZipFile.OpenRead(apkPath))
        {
            foreach (var entry in zip.Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.FullName) || entry.FullName.EndsWith("/"))
                {
                    continue;
                }

                var name = entry.FullName.Replace('\\', '/');
                if (!name.StartsWith("assets/", StringComparison.Ordinal))
                {
                    continue;
                }

                var rel = name.Substring("assets/".Length);
                if (!ShouldExtractRelative(rel))
                {
                    continue;
                }

                var outPath = Path.Combine(dest, rel.Replace('/', Path.DirectorySeparatorChar));
                var outDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                entry.ExtractToFile(outPath, overwrite: true);
                extracted++;
            }
        }

        File.WriteAllText(stampPath, stamp);
        Debug.Log(
            $"StreamingAssetsBaseline: extracted {extracted} files -> {dest} stamp={Application.version}");
        if (!IsReadablePackageRoot(dest))
        {
            throw new InvalidOperationException(
                "APK extract finished but content/map/systems or maps still missing at " + dest);
        }

        return dest;
    }

    private static bool ShouldExtractRelative(string rel)
    {
        // 包内基线：内容/地图/战术图（与 ClientArtPaths 一致）
        return rel.StartsWith("content/", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("maps/", StringComparison.OrdinalIgnoreCase)
            || rel.StartsWith("art/", StringComparison.OrdinalIgnoreCase);
    }
#endif
}
