using System.IO;
using TopDog.Foundation.Io;
using UnityEngine;

namespace TopDog.Client;

/// <summary>Points TopDog.Core content loaders at StreamingAssets before any simulation starts.</summary>
public static class ContentRootBootstrap
{
    public static void Apply()
    {
        var root = Application.streamingAssetsPath;
        var hasTutorialMap = Directory.Exists(Path.Combine(root, "content", "map", "systems"));
        var hasPackagedMaps = Directory.Exists(Path.Combine(root, "maps"));
        if (hasTutorialMap || hasPackagedMaps)
        {
            AppRoot.SetOverrideRoot(root);
            Debug.Log("TopDog content root -> " + root);
        }
        else
        {
            Debug.LogWarning(
                "StreamingAssets/content/map or StreamingAssets/maps not found; "
                + "simulation may fail to load maps.");
        }
    }
}
