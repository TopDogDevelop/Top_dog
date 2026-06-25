using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Possession;

public static class PossessionService
{
    public static string Possess(GameState state, string memberId)
    {
        if (state.activeBattlefieldId == null || !state.combatRealtimeActive)
        {
            return "当前无进行中的实时战";
        }
        var m = FindMember(state, memberId);
        if (m == null)
        {
            return "找不到团员";
        }
        if (!m.traitIds.Contains("trait_loyal"))
        {
            return (m.name ?? memberId) + " 无死忠词条，无法附身";
        }
        if (m.equippedHullId == null)
        {
            return (m.name ?? memberId) + " 未配舰，无法附身";
        }
        var bf = FindBattlefield(state, state.activeBattlefieldId);
        if (bf == null)
        {
            return "战场不存在";
        }
        var unit = FindUnitByMember(bf, memberId);
        if (unit == null || unit.IsDestroyed())
        {
            return (m.name ?? memberId) + " 未在本战场到场";
        }
        state.possessingMemberId = memberId;
        unit.aiOrder = UnitAiOrder.MANUAL;
        return "已附身 " + (m.name ?? memberId);
    }

    public static string PossessByName(GameState state, string memberNameOrId)
    {
        var m = FindMemberByName(state, memberNameOrId);
        return m?.memberId != null ? Possess(state, m.memberId) : "找不到团员";
    }

    public static string OrderFollow(GameState state)
    {
        var bf = ActiveBattlefield(state);
        return bf != null ? FleetOrderService.OrderFollow(state, bf) : "无活跃战";
    }

    public static string OrderFocus(GameState state, string? targetUnitId = null)
    {
        var bf = ActiveBattlefield(state);
        return bf != null ? FleetOrderService.OrderFocus(state, bf, targetUnitId) : "无活跃战";
    }

    public static string SwitchBattlefield(GameState state, string battlefieldId)
    {
        foreach (var bf in state.battlefields)
        {
            if (battlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal) && !bf.finished)
            {
                state.activeBattlefieldId = battlefieldId;
                state.possessingMemberId = null;
                return "已切换战场 " + battlefieldId;
            }
        }
        return "找不到进行中的战 " + battlefieldId;
    }

    private static BattlefieldState? ActiveBattlefield(GameState state)
    {
        if (state.activeBattlefieldId == null)
        {
            return null;
        }
        return FindBattlefield(state, state.activeBattlefieldId);
    }

    private static BattlefieldState? FindBattlefield(GameState state, string id)
    {
        foreach (var bf in state.battlefields)
        {
            if (id.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }
        return null;
    }

    private static BattlefieldUnit? FindUnitByMember(BattlefieldState bf, string memberId)
    {
        foreach (var u in bf.units)
        {
            if (memberId.Equals(u.memberId, StringComparison.Ordinal))
            {
                return u;
            }
        }
        return null;
    }

    private static MemberState? FindMember(GameState state, string id)
    {
        foreach (var m in state.members)
        {
            if (id.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    private static MemberState? FindMemberByName(GameState state, string needle)
    {
        var n = needle.Trim();
        foreach (var m in state.members)
        {
            if (n.Equals(m.memberId, StringComparison.OrdinalIgnoreCase)
                || n.Equals(m.name, StringComparison.OrdinalIgnoreCase))
            {
                return m;
            }
        }
        return null;
    }
}
