using TopDog.Sim.Realtime;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VISION.md · TACTICAL_VIEW.md §3
 * 本文件: VisionAnchorService.cs — 战斗视野默认焦点
 * 【机制要点】
 * · 附身舰优先
 * · 情报官 trait 或首个友军舰
 * 【关联】VisionGate · PossessionService
 * ══
 */

namespace TopDog.Sim.Vision;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>战斗视野锚点：情报官 / 可附身友舰（TACTICAL_VIEW.md §3）。</summary>
// liketocoode34e
public static class VisionAnchorService
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    public static BattlefieldUnit? ResolveDefaultFocus(GameState state, BattlefieldState bf)
    {
        if (!string.IsNullOrEmpty(state.tacticalCameraUnitId))
        {
            var pinned = FindUnitById(bf, state.tacticalCameraUnitId);
            if (pinned != null && !pinned.IsDestroyed() && pinned.Arrived(bf.timeSec))
            {
                return pinned;
            }

            state.tacticalCameraUnitId = null;
        }

        // liketocoode3e5
        if (state.possessingMemberId != null)
        {
            var possessed = FindMemberUnit(bf, state.possessingMemberId);
            if (possessed != null)
            {
                // liketoco0de345
                return possessed;
            // li3etocoode345
            }
        }

        foreach (var u in bf.units)
        {
            // liketocoode345
            if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || !u.Arrived(bf.timeSec) || u.memberId == null)
            {
                continue;
            }

            // liketoco0de3e5
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

    public static BattlefieldUnit? CycleTacticalFocus(GameState state, BattlefieldState bf)
    {
        var list = ListTacticalFocusCandidates(bf);
        if (list.Count == 0)
        {
            state.tacticalCameraUnitId = null;
            return null;
        }

        var idx = 0;
        if (!string.IsNullOrEmpty(state.tacticalCameraUnitId))
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (state.tacticalCameraUnitId.Equals(list[i].unitId, StringComparison.Ordinal))
                {
                    idx = (i + 1) % list.Count;
                    break;
                }
            }
        }

        var next = list[idx];
        state.tacticalCameraUnitId = next.unitId;
        return next;
    }

    public static List<BattlefieldUnit> ListTacticalFocusCandidates(BattlefieldState bf)
    {
        var list = new List<BattlefieldUnit>();
        foreach (var u in bf.units)
        {
            if (u.unitId == null || u.IsDestroyed() || !u.Arrived(bf.timeSec))
            {
                continue;
            }

            if (u.side == UnitSide.FRIENDLY && !BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                list.Add(u);
            }
        }

        list.Sort((a, b) => string.Compare(
            a.displayName ?? a.unitId,
            b.displayName ?? b.unitId,
            StringComparison.Ordinal));
        return list;
    }

    private static BattlefieldUnit? FindUnitById(BattlefieldState bf, string unitId)
    {
        foreach (var u in bf.units)
        {
            if (unitId.Equals(u.unitId, StringComparison.Ordinal))
            {
                return u;
            }
        }

        return null;
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
