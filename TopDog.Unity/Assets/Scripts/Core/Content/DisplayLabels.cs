using System;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Legion;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Content;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_RIGHT_RAIL_SCENE_PROXY.md §2.3 · docs/MEMBERS.md
 * 本文件: DisplayLabels.cs — 玩家可见双语/中文标签
 * 【机制要点】
 * · ObjectOverviewLine1：舰中文名-团员-友好/敌对-军团（无英文 hull）
 * · ObjectOverviewCompact：单行 + 距焦距或「跃迁在途」
 * · JoinBilingual：中文在前 · 英文在后
 * ══
 */

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

    /// <summary>舰体中文名（不含英文后缀）。</summary>
    public static string HullZhOnly(HullDef? hull) =>
        hull == null ? "?" : (hull.displayName ?? hull.hullId ?? "?");

    /// <summary>右栏物体总览第一行：舰中文名-团员-友好/敌对-军团。</summary>
    public static string ObjectOverviewLine1(GameState state, BattlefieldUnit unit, ShipRegistry? ships)
    {
        var side = unit.side == UnitSide.ENEMY ? "敌对" : "友好";
        var legion = LegionDisplay.FormatLegionLabel(state, unit.legionId);
        if (unit.isBuilding)
        {
            var place = unit.displayName ?? unit.unitId ?? "?";
            return $"{place}-—-{side}-{legion}";
        }

        var hullZh = HullZhOnly(ships?.FindHull(unit.hullId));
        if (hullZh == "?" && !string.IsNullOrEmpty(unit.tonnageClass))
        {
            hullZh = TonnageZhOnly(unit.tonnageClass);
        }

        var member = ResolveMemberDisplayName(state, unit.memberId) ?? unit.displayName ?? "—";
        return $"{hullZh}-{member}-{side}-{legion}";
    }

    /// <summary>右栏物体总览第二行：与当前视角焦距单位的距离。</summary>
    public static string ObjectOverviewDistanceLine(float distanceM)
    {
        if (distanceM < 0f)
        {
            return "—";
        }

        if (distanceM < 1000f)
        {
            return $"距焦距 {distanceM:0} m";
        }

        if (distanceM < 1_000_000f)
        {
            return $"距焦距 {distanceM / 1000f:0.0} km";
        }

        return $"距焦距 {distanceM / 1_000_000f:0.00} Mm";
    }

    /// <summary>右栏单行：舰中文名-团员-友好/敌对-军团 · 距焦距。</summary>
    public static string ObjectOverviewCompact(
        GameState state,
        BattlefieldUnit unit,
        ShipRegistry? ships,
        float distanceM,
        bool inTransit)
    {
        var title = ObjectOverviewLine1(state, unit, ships);
        if (inTransit)
        {
            return title + " · 跃迁在途";
        }

        var dist = ObjectOverviewDistanceLine(distanceM);
        return title + " · " + dist;
    }

    private static string TonnageZhOnly(string tonnageClass) => tonnageClass switch
    {
        "FRIGATE" => "护卫舰",
        "DESTROYER" => "驱逐舰",
        "CRUISER" => "巡洋舰",
        "BATTLECRUISER" => "战列巡洋舰",
        "BATTLESHIP" => "战列舰",
        "DREADNOUGHT" => "无畏舰",
        "CARRIER" => "航空母舰",
        "SUPERCARRIER" or "SUPERCAPITAL" => "超级旗舰",
        "TITAN" => "泰坦",
        "STRIKE_CRAFT" => "舰载机",
        "MISSILE" => "导弹",
        "BUILDING" or "COMPLEX" => "建筑",
        "DRONE" or "SHUTTLE" => "无人机",
        _ => tonnageClass,
    };

    /// <summary>统一舰船详情标题：舰名 · 团员名。</summary>
    public static string ShipMemberTitle(GameState state, BattlefieldUnit unit, ShipRegistry? ships)
    {
        var memberName = ResolveMemberDisplayName(state, unit.memberId) ?? unit.displayName ?? "?";
        var hull = ships?.FindHull(unit.hullId);
        return $"{HullBilingual(hull)} · {memberName}";
    }

    public static string ShipMemberTitle(GameState state, MemberState member, ShipRegistry? ships)
    {
        var memberName = ResolveMemberDisplayName(state, member.memberId)
                         ?? member.name
                         ?? member.memberId
                         ?? "?";
        var hull = ships?.FindHull(member.equippedHullId);
        return $"{HullBilingual(hull)} · {memberName}";
    }

    private static string? ResolveMemberDisplayName(GameState state, string? memberId)
    {
        if (string.IsNullOrEmpty(memberId))
        {
            return null;
        }

        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return !string.IsNullOrWhiteSpace(m.name) ? m.name
                    : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName
                    : m.memberId;
            }
        }

        return null;
    }

    /// <summary>伴聊发言人：仅游戏内名 name；禁止 accountName / 现实人名。</summary>
    public static string ResolveBanterSpeakerName(GameState state, string? memberId)
    {
        if (string.IsNullOrEmpty(memberId))
        {
            return "?";
        }

        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(m.name))
                {
                    return m.name.Trim();
                }

                return m.memberId ?? "?";
            }
        }

        return memberId;
    }

    public static string HullLicenseLabel(string? licenseKey) => licenseKey switch
    {
        "logistics" => JoinBilingual("后勤", "Logistics"),
        "boarding" => JoinBilingual("登录", "Boarding"),
        "shield_fleet" => JoinBilingual("盾舰队装备", "Shield Fleet"),
        "armor_fleet" => JoinBilingual("甲舰队装备", "Armor Fleet"),
        "anti_missile_laser" => JoinBilingual("反导弹激光", "Anti-Missile Laser"),
        _ => licenseKey ?? "?",
    };

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
