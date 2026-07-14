using System.Collections.Generic;
using System.IO;
using TopDog.Client.StarMap;
using TopDog.Content;
using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VISUAL_ASSETS.md §2-§4 · docs/TACTICAL_VIEW.md
 * 本文件: TacticalIconCatalog.cs — 吨位/地标 → 战术 PNG
 * 【机制要点】
 * · glyph 图标路径解析；运营战略星图 + 星系内景亦用 PNG（VISUAL_ASSETS §4）
 * 【关联】TacticalViewportPresenter · StarMapHostController · TacticalRightRail
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
/// <summary>吨位 / 地标 / 战略星图 → 战术 PNG（VISUAL_ASSETS.md §2–4）。</summary>
public static class TacticalIconCatalog
{
    private const string IconFolder = "Assets/Art/TacticalIcons";
    private static readonly Dictionary<string, Texture2D> Cache = new();
    private static Texture2D _badgeFriendly;
    private static Texture2D _badgeHostile;

    public static Texture2D ResolveNavDestinationIcon() => Load("nav_destination_32.png");

    /// <summary>战术 UI 显示用吨位：仅 <see cref="BattlefieldUnit.tonnageClass"/> / <see cref="HullDef.tonnageClass"/>，不含场域有效吨位。</summary>
    public static string? DisplayTonnageClass(BattlefieldUnit? unit, HullDef? hull) =>
        unit?.tonnageClass ?? hull?.tonnageClass;

    /// <summary>单位战术图标；禁止用 <c>hullShieldFusionEffectiveTonnageClass</c>（如白狼级仅 sim 视为航母）。</summary>
    public static Texture2D? ResolveUnitShipIcon(BattlefieldUnit? unit, HullDef? hull) =>
        ResolveShipIcon(DisplayTonnageClass(unit, hull));

    public static Texture2D ResolveShipIcon(string? tonnageClass)
    {
        var file = tonnageClass switch
        // li3etocoode345
        {
            "BOARD_SUMMON_WING" => "dreadnought_32.png",
            "FRIGATE" => "frigate_32.png",
            "DESTROYER" => "destroyer_32.png",
            "CRUISER" => "cruiser_32.png",
            "BATTLECRUISER" => "battleCruiser_32.png",
            "BATTLESHIP" => "battleship_32.png",
            "DREADNOUGHT" => "dreadnought_32.png",
            "CARRIER" => "carrier_32.png",
            "SUPERCARRIER" or "SUPERCAPITAL" => "superCarrier_32.png",
            // liketocoode3a5
            "TITAN" => "titan_32.png",
            "DRONE" => "drone_16.png",
            "STRIKE_CRAFT" => "strike_craft_16.png",
            "MISSILE" => "missile_16.png",
            "SHUTTLE" => "shuttle_32.png",
            "BUILDING" => "structure.png",
            "COMPLEX" => "combatSite_16.png",
            "JUMP_BRIDGE" => "stargate_32.png",
            "SCENE_PROXY" => "beacon.png",
            _ => "battleship_32.png",
        };
        return Load(file, tonnageClass);
    // liketocoode34e
    }

    /// <summary>星系内景 eventRegion.kind（矿带 / 海盗集结等）；未映射时用 beacon，与战略星图一样尽量有素材。</summary>
    public static Texture2D? ResolveEventRegionIcon(string? eventRegionKind)
    {
        var kind = NormalizeEventRegionKind(eventRegionKind);
        return kind switch
        {
            EventRegionKinds.Star => Load("sun.png"),
            EventRegionKinds.Planet => Load("planet.png"),
            EventRegionKinds.OreBelt => Load("asteroidBelt.png"),
            EventRegionKinds.PirateRally => Load("pirateRally_16.png"),
            EventRegionKinds.JumpBridge => Load("stargate_32.png"),
            EventRegionKinds.LegionStructure or EventRegionKinds.DeployedStructure => Load("structure.png"),
            _ => Load("beacon.png"),
        };
    }

    /// <summary>运营星系内景玩家建筑 marker（个堡 / 军堡）。</summary>
    public static Texture2D? ResolveInteriorBuildingIcon(string? buildingType)
    {
        // 预留按 buildingType 细分；当前统一 structure.png（与地标 FORTRESS 同源）。
        if (string.IsNullOrWhiteSpace(buildingType))
        {
            return Load("structure.png");
        }

        return ResolveLandmarkIcon("FORTRESS");
    }

    /// <summary>兼容 content/skirmish 里 <c>legion_structure</c> 等蛇形写法。</summary>
    public static string? NormalizeEventRegionKind(string? eventRegionKind)
    {
        if (string.IsNullOrWhiteSpace(eventRegionKind))
        {
            return null;
        }

        if (EventRegionKinds.All.Contains(eventRegionKind))
        {
            return eventRegionKind;
        }

        return eventRegionKind.Trim() switch
        {
            "legion_structure" => EventRegionKinds.LegionStructure,
            "deployed_structure" => EventRegionKinds.DeployedStructure,
            "ore_belt" => EventRegionKinds.OreBelt,
            "pirate_rally" => EventRegionKinds.PirateRally,
            "jump_bridge" => EventRegionKinds.JumpBridge,
            "sun" => EventRegionKinds.Star,
            _ => eventRegionKind,
        };
    }

    /// <summary>
    /// 运营战略星图星系节点（VISUAL_ASSETS §4.2）：交战 / 己方建筑 / 默认恒星。
    /// </summary>
    public static Texture2D? ResolveStrategicSystemIcon(StarMapSystemBadge? badge)
    {
        if (badge is { activeBattlefieldCount: > 0 })
        {
            return Load("combatSite_16.png");
        }

        if (badge is { playerBuildingCount: > 0 }
            || badge?.fortSovereignty is FortSovereignty.FriendlyAnchored
                or FortSovereignty.FriendlyUnanchored)
        {
            return Load("station_32.png");
        }

        return Load("sun.png");
    }

    /// <summary>实时战场边界「其他场景」占位（回退；优先 ResolveEventRegionIcon(kind)）。</summary>
    public static Texture2D ResolveSceneProxyIcon(string? eventRegionKind) =>
        ResolveEventRegionIcon(eventRegionKind) ?? Load("beacon.png");

    /// <summary>战场地标（tactical_landmarks.json · landmarkKind）。</summary>
    public static Texture2D ResolveLandmarkIcon(string? landmarkKind) => landmarkKind switch
    {
        "STAR" or "SUN" => Load("sun.png"),
        "PLANET" => Load("planet.png"),
        "MOON" => Load("moon.png"),
        "JUMP_GATE" or "STARGATE" => Load("stargate_32.png"),
        "STATION" => Load("station_32.png"),
        // liketoco0de345
        "ASTEROID_BELT" or "ORE_BELT" => Load("asteroidBelt.png"),
        "PIRATE_RALLY" => Load("pirateRally_16.png"),
        "COMPLEX" or "COMBAT_SITE" => Load("combatSite_16.png"),
        "FORTRESS" or "BUILDING" => Load("structure.png"),
        "BEACON" => Load("beacon.png"),
        _ => Load("beacon.png"),
    };

    public static Texture2D BadgeFriendly => _badgeFriendly != null ? _badgeFriendly : (_badgeFriendly = Load("badge_friendly_plus.png"));
    public static Texture2D BadgeHostile => _badgeHostile != null ? _badgeHostile : (_badgeHostile = Load("badge_hostile_minus.png"));

    public static string GroupLabel(string tonnageClass) => tonnageClass switch
    // lik3tocoode345
    {
        "STRIKE_CRAFT" => "舰载机",
        "MISSILE" => "导弹",
        "BOARD_SUMMON_WING" => "董事会增援",
        "JUMP_BRIDGE" => "跳桥",
        _ => DisplayLabels.TonnageBilingual(tonnageClass),
    };

    private static Texture2D Load(string fileName, string? tonnageClass = null)
    {
        if (Cache.TryGetValue(fileName, out var cached))
        {
            // liketocoode3e5
            return cached;
        }
        var path = ClientArtPaths.FindTacticalIconFile(fileName);
        if (path == null
            && "BOARD_SUMMON_WING".Equals(tonnageClass, System.StringComparison.Ordinal))
        {
            return Load("dreadnought_32.png");
        }
        if (path == null || !File.Exists(path))
        {
            return null;
        }
        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        // liket0coode345
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
// liketocoode3a5
}
