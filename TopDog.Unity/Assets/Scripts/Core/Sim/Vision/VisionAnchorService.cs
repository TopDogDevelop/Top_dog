using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Vision;

/// <summary>战斗视野锚点：情报官 / 可附身友舰（TACTICAL_VIEW.md §3）。</summary>
public static class VisionAnchorService
{
    public static BattlefieldUnit? ResolveDefaultFocus(GameState state, BattlefieldState bf)
    {
        if (state.possessingMemberId != null)
        {
            var possessed = FindMemberUnit(bf, state.possessingMemberId);
            if (possessed != null)
            {
                return possessed;
            }
        }

        foreach (var u in bf.units)
        {
            if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || !u.Arrived(bf.timeSec) || u.memberId == null)
            {
                continue;
            }

            var m = FindMember(state, u.memberId);
            if (m?.traitIds.Contains("trait_intel_officer") == true)
            {
                return u;
            }
        }

        foreach (var u in bf.units)
        {
            if (u.side == UnitSide.FRIENDLY && u.alive && u.memberId != null && u.Arrived(bf.timeSec))
            {
                return u;
            }
        }

        return bf.units.Count > 0 ? bf.units[0] : null;
    }

    public static List<BattlefieldUnit> ListPossessableFriendlies(GameState state, BattlefieldState bf)
    {
        var list = new List<BattlefieldUnit>();
        foreach (var u in bf.units)
        {
            if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || u.memberId == null || !u.Arrived(bf.timeSec))
            {
                continue;
            }

            list.Add(u);
        }

        return list;
    }

    public static string? CyclePossession(GameState state, BattlefieldState bf)
    {
        var list = ListPossessableFriendlies(state, bf);
        if (list.Count == 0)
        {
            return null;
        }

        var idx = 0;
        if (state.possessingMemberId != null)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (state.possessingMemberId.Equals(list[i].memberId, StringComparison.Ordinal))
                {
                    idx = (i + 1) % list.Count;
                    break;
                }
            }
        }

        return list[idx].memberId;
    }

    private static BattlefieldUnit? FindMemberUnit(BattlefieldState bf, string memberId)
    {
        foreach (var u in bf.units)
        {
            if (memberId.Equals(u.memberId, StringComparison.Ordinal) && u.alive)
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
}
