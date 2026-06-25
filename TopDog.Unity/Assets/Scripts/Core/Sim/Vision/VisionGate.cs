using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Vision;

public static class VisionGate
{
    public static bool HasDirectBattlefieldView(GameState state, string? battlefieldId)
    {
        if (battlefieldId == null || !state.combatRealtimeActive)
        {
            return false;
        }

        var bf = FindBattlefield(state, battlefieldId);
        if (bf == null)
        {
            return false;
        }

        if (state.possessingMemberId != null && UnitOnField(bf, state.possessingMemberId))
        {
            return true;
        }

        foreach (var u in bf.units)
        {
            if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || !u.Arrived(bf.timeSec))
            {
                continue;
            }
            if (u.memberId == null)
            {
                continue;
            }
            var m = FindMember(state, u.memberId);
            if (m?.traitIds.Contains("trait_intel_officer") == true)
            {
                return true;
            }
        }
        return false;
    }

    public static List<BattlefieldState> ListVisibleBattlefields(GameState state)
    {
        var list = new List<BattlefieldState>();
        if (state.spectatorFullVision || state.spectatorMode)
        {
            foreach (var bf in state.battlefields)
            {
                if (!bf.finished && bf.battlefieldId != null)
                {
                    list.Add(bf);
                }
            }
            return list;
        }

        foreach (var bf in state.battlefields)
        {
            if (!bf.finished && bf.battlefieldId != null
                && HasDirectBattlefieldView(state, bf.battlefieldId))
            {
                list.Add(bf);
            }
        }
        return list;
    }

    private static bool UnitOnField(BattlefieldState bf, string memberId)
    {
        foreach (var u in bf.units)
        {
            if (memberId.Equals(u.memberId, StringComparison.Ordinal) && u.alive)
            {
                return true;
            }
        }
        return false;
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
