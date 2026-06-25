using System;
using TopDog.Content.Ships;
using TopDog.Content.Traits;

namespace TopDog.Content;

/// <summary>玩家可见双语标签：中文在前 · 英文在后（MEMBERS.md）。</summary>
public static class DisplayLabels
{
    public static string JoinBilingual(string? zh, string? en)
    {
        var z = string.IsNullOrWhiteSpace(zh) ? null : zh.Trim();
        var e = string.IsNullOrWhiteSpace(en) ? null : en.Trim();
        if (z == null)
        {
            return e ?? "?";
        }
        if (e == null || z.Equals(e, StringComparison.Ordinal))
        {
            return z;
        }
        return z + " · " + e;
    }

    public static string TraitBilingual(TraitDef? trait) =>
        trait == null
            ? "?"
            : JoinBilingual(trait.displayNameZh ?? trait.traitId, trait.displayNameEn);

    public static string HullBilingual(HullDef? hull) =>
        hull == null
            ? "?"
            : JoinBilingual(hull.displayName ?? hull.hullId, hull.displayNameEn);

    public static string TonnageBilingual(string? tonnageClass) => tonnageClass switch
    {
        "FRIGATE" => JoinBilingual("护卫舰", "Frigate"),
        "DESTROYER" => JoinBilingual("驱逐舰", "Destroyer"),
        "CRUISER" => JoinBilingual("巡洋舰", "Cruiser"),
        "BATTLECRUISER" => JoinBilingual("战列巡洋舰", "Battlecruiser"),
        "BATTLESHIP" => JoinBilingual("战列舰", "Battleship"),
        "DREADNOUGHT" => JoinBilingual("无畏舰", "Dreadnought"),
        "CARRIER" => JoinBilingual("航空母舰", "Carrier"),
        "SUPERCARRIER" or "SUPERCAPITAL" => JoinBilingual("超级旗舰", "Supercarrier"),
        "TITAN" => JoinBilingual("泰坦", "Titan"),
        "STRIKE_CRAFT" => JoinBilingual("舰载机", "Strike Craft"),
        "BUILDING" or "COMPLEX" => JoinBilingual("建筑", "Building"),
        "DRONE" or "SHUTTLE" => JoinBilingual("无人机", "Drone"),
        _ => tonnageClass ?? "?",
    };
}
