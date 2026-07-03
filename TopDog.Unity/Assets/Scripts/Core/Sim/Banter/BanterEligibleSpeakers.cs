using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>伴聊发言人候选池（与名册排序无关）。</summary>
public static class BanterEligibleSpeakers
{
    public static List<MemberState> List(GameState state)
    {
        var localLegion = LegionRegistry.Local(state)?.legionId;
        var list = new List<MemberState>();
        if (state.phase == GamePhase.COMBAT && state.combatRealtimeActive)
        {
            var bf = ActiveBattlefield(state);
            if (bf == null)
            {
                return list;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var u in bf.units)
            {
                if (u.IsDestroyed() || string.IsNullOrWhiteSpace(u.memberId) || !seen.Add(u.memberId))
                {
                    continue;
                }

                if (u.side != UnitSide.FRIENDLY)
                {
                    continue;
                }

                var m = FindMember(state, u.memberId);
                if (m != null)
                {
                    list.Add(m);
                }
            }

            return list;
        }

        foreach (var m in state.members)
        {
            if (m.memberId == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(localLegion))
            {
                var legionId = LegionPlayerRegistry.ResolveMemberLegionId(state, m);
                if (!string.Equals(legionId, localLegion, StringComparison.Ordinal))
                {
                    continue;
                }
            }

            list.Add(m);
        }

        return list;
    }

    private static BattlefieldState? ActiveBattlefield(GameState state)
    {
        if (string.IsNullOrWhiteSpace(state.activeBattlefieldId))
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            if (state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
    }

    private static MemberState? FindMember(GameState state, string memberId)
    {
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }

        return null;
    }
}
