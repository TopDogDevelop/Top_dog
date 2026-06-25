using System.Collections.Generic;
using System.IO;
using TopDog.Content;
using UnityEngine;

namespace TopDog.Client.Tactical;

/// <summary>吨位 → 战术 PNG（VISUAL_ASSETS.md §2）。</summary>
public static class TacticalIconCatalog
{
    private const string IconFolder = "Assets/Art/TacticalIcons";
    private static readonly Dictionary<string, Texture2D> Cache = new();
    private static Texture2D _badgeFriendly;
    private static Texture2D _badgeHostile;

    public static Texture2D ResolveShipIcon(string tonnageClass)
    {
        var file = tonnageClass switch
        {
            "FRIGATE" or "DESTROYER" or "DRONE" or "STRIKE_CRAFT" or "SHUTTLE" => "frigate_32.png",
            "CRUISER" => "battleship_32.png",
            "BATTLECRUISER" => "battleship_32.png",
            "BATTLESHIP" => "battleship_32.png",
            "DREADNOUGHT" => "dreadnought_32.png",
            "CARRIER" => "carrier_32.png",
            "SUPERCARRIER" or "SUPERCAPITAL" => "superCarrier_32.png",
            "TITAN" => "titan_32.png",
            "BUILDING" or "COMPLEX" => "structure.png",
            _ => "battleship_32.png",
        };
        return Load(file);
    }

    public static Texture2D BadgeFriendly => _badgeFriendly != null ? _badgeFriendly : (_badgeFriendly = Load("badge_friendly_plus.png"));
    public static Texture2D BadgeHostile => _badgeHostile != null ? _badgeHostile : (_badgeHostile = Load("badge_hostile_minus.png"));

    public static string GroupLabel(string tonnageClass) =>
        DisplayLabels.TonnageBilingual(tonnageClass);

    private static Texture2D Load(string fileName)
    {
        if (Cache.TryGetValue(fileName, out var cached))
        {
            return cached;
        }
        var path = Path.Combine(Application.dataPath, "Art", "TacticalIcons", fileName);
        if (!File.Exists(path))
        {
            return null;
        }
        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Object.Destroy(tex);
            return null;
        }
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        Cache[fileName] = tex;
        return tex;
    }
}
