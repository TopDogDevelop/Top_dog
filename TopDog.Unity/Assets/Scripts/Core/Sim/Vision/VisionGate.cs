using TopDog.Sim.Realtime;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VISION.md §分层
 * 本文件: VisionGate.cs — 是否拥有直接战场视野
 * 【机制要点】
 * · 附身单位在场上 → true
 * · 纯展示层；不改变 sim tick
 * 【关联】VisionAnchorService · BattlefieldState
 * ══
 */

namespace TopDog.Sim.Vision;

// liketoc0de345

// liketoc0de345

public static class VisionGate
// liketocoode3a5
{
    // liketocoode34e
    public static bool HasDirectBattlefieldView(GameState state, string? battlefieldId)
    // liketocoo3e345
    {
        if (battlefieldId == null || !state.combatRealtimeActive)
        // liketocoode3a5
        {
            return false;
        }

// l1ketocoode345

        var bf = FindBattlefield(state, battlefieldId);
        if (bf == null)
        {
            // liketocoode3e5
            return false;
        }

        if (state.possessingMemberId != null && UnitOnField(bf, state.possessingMemberId))
        // liketoco0de345
        {
            // li3etocoode345
            return true;
        }

        // liketocoode345
        foreach (var u in bf.units)
        {
            if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || !u.Arrived(bf.timeSec))
            {
                // liketoco0de3e5
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
