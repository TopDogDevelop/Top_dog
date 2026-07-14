using System.IO;
using TopDog.Client.OnlineUpdate;
using TopDog.Foundation.Io;
using UnityEngine;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VISUAL_ASSETS.md · docs/TACTICAL_VIEW.md · docs/RELEASE_AND_HOTUPDATE.md §倒Y · §Android StreamingAssets
 * 本文件: ClientArtPaths.cs — 玩家包 art 根解析（图标 / 战斗天空盒）
 * 【机制要点】
 * · Player：content_runtime/art → AppRoot/art → streaming_baseline/art → StreamingAssets/art
 * · Android jar: StreamingAssets 不可枚举；须经 StreamingAssetsBaseline 解压后读 baseline/art
 * 【关联】CombatBackgroundCatalog · TacticalIconCatalog · StreamingAssetsBaseline
 * ══
 */

namespace TopDog.Client;

/// <summary>
/// Resolves art folders for player builds.
/// Editor: Assets/Art works via Application.dataPath.
/// Player: Art must live under StreamingAssets/art, streaming_baseline/art, or content_runtime/art.
/// </summary>
public static class ClientArtPaths
{
    private static bool _loggedRoots;

    public static string? FindTacticalIconFile(string fileName)
    {
        LogRootsOnce();
        foreach (var root in CandidateArtRoots())
        {
            var path = Path.Combine(root, "tactical_icons", fileName);
            if (File.Exists(path))
            {
                return path;
            }

            // Legacy Editor layout: Assets/Art/TacticalIcons
            path = Path.Combine(root, "TacticalIcons", fileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public static string? FindCombatBackgroundSetDir(string pool, string setId)
    {
        LogRootsOnce();
        foreach (var root in CandidateArtRoots())
        {
            var path = Path.Combine(root, "combat_backgrounds", pool, setId);
            if (Directory.Exists(path))
            {
                return path;
            }

            path = Path.Combine(root, "CombatBackgrounds", pool, setId);
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static void LogRootsOnce()
    {
        if (_loggedRoots)
        {
            return;
        }

        _loggedRoots = true;
        foreach (var root in CandidateArtRoots())
        {
            var exists = Directory.Exists(root);
            var equirectHint = exists
                && (Directory.Exists(Path.Combine(root, "combat_backgrounds"))
                    || Directory.Exists(Path.Combine(root, "CombatBackgrounds")));
            var iconsHint = exists
                && (Directory.Exists(Path.Combine(root, "tactical_icons"))
                    || Directory.Exists(Path.Combine(root, "TacticalIcons")));
            Debug.Log(
                $"[ClientArtPaths] root={(string.IsNullOrEmpty(root) ? "(empty)" : root)} "
                + $"exists={exists} backgrounds={equirectHint} icons={iconsHint}");
        }
    }

    private static string[] CandidateArtRoots()
    {
        // Order: hot-update → AppRoot → Android baseline → StreamingAssets → Editor Assets/Art
        return new[]
        {
            Path.Combine(OnlineUpdateClient.ContentRuntimeRoot, "art"),
            Path.Combine(AppRoot.Find(), "art"),
            Path.Combine(Application.persistentDataPath, StreamingAssetsBaseline.ExtractFolderName, "art"),
            Path.Combine(Application.streamingAssetsPath ?? "", "art"),
            Path.Combine(Application.dataPath ?? "", "Art"),
        };
    }
}
