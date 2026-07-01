using System;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Content;

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
