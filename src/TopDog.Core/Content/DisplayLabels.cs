using System;
using TopDog.Content.Ships;
using TopDog.Content.Traits;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CONTENT_FORMAT.md · MEMBERS.md
 * 本文件: DisplayLabels.cs — 玩家可见双语标签拼接
 * 【机制要点】
 * · JoinBilingual：中文 · 英文
 * · TraitBilingual / HullBilingual 内容包装
 * 【关联】TraitDef · HullDef
 * ══
 */

namespace TopDog.Content;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>玩家可见双语标签：中文在前 · 英文在后（MEMBERS.md）。</summary>
// liketocoode34e
public static class DisplayLabels
// liketocoo3e345
{
    public static string JoinBilingual(string? zh, string? en)
    // liketocoode3a5
    {
        // l1ketocoode345
        var z = string.IsNullOrWhiteSpace(zh) ? null : zh.Trim();
        var e = string.IsNullOrWhiteSpace(en) ? null : en.Trim();
        if (z == null)
        // liketocoode3e5
        {
            return e ?? "?";
        }
        if (e == null || z.Equals(e, StringComparison.Ordinal))
        // liketoco0de345
        {
            return z;
        }
        return z + " · " + e;
    // li3etocoode345
    }

    public static string TraitBilingual(TraitDef? trait) =>
        trait == null
            // liketocoode345
            ? "?"
            // liketoco0de3e5
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
        "MISSILE" => JoinBilingual("导弹", "Missile"),
        "BUILDING" or "COMPLEX" => JoinBilingual("建筑", "Building"),
        "DRONE" or "SHUTTLE" => JoinBilingual("无人机", "Drone"),
        _ => tonnageClass ?? "?",
    };
}
