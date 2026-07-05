using TopDog.Sim.Realtime;

using TopDog.Sim.Skirmish;

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



/// <summary>战斗视野锚点：情报官 / 可附身友舰（TACTICAL_VIEW.md §3）。</summary>

public static class VisionAnchorService

{

    public static BattlefieldUnit? ResolveDefaultFocus(GameState state, BattlefieldState bf)

    {

        if (!string.IsNullOrEmpty(state.tacticalCameraUnitId))

        {

            var pinned = FindUnitById(bf, state.tacticalCameraUnitId)

                ?? FindTransitUnitOnBattlefield(state, bf, state.tacticalCameraUnitId);

            if (pinned != null && !pinned.IsDestroyed()

                && (IsFocusEligible(pinned, bf.timeSec)

                    || FindTransitUnitOnBattlefield(state, bf, state.tacticalCameraUnitId) != null)

                && IsTacticalCameraEligible(state, pinned, bf.timeSec))

            {

                return pinned;

            }

        }



        var warping = FindWarpPipelineFriendly(state, bf);

        if (warping != null)

        {

            return warping;

        }



        var transitProxy = FindTransitSceneProxyFocus(state, bf);

        if (transitProxy != null)

        {

            return transitProxy;

        }



        if (!string.IsNullOrEmpty(state.tacticalCameraUnitId))

        {

            state.tacticalCameraUnitId = null;

        }



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

            if (!IsTacticalCameraEligible(state, u, bf.timeSec))

            {

                continue;

            }



            var m = FindMember(state, u.memberId!);

            if (m?.traitIds.Contains("trait_intel_officer") == true)

            {

                return u;

            }

        }



        foreach (var u in bf.units)

        {

            if (IsTacticalCameraEligible(state, u, bf.timeSec))

            {

                return u;

            }

        }



        if (!SkirmishBuildingRules.IsSkirmish(state))

        {

            return bf.units.Count > 0 ? bf.units[0] : null;

        }



        return null;

    }



    private static BattlefieldUnit? FindWarpPipelineFriendly(GameState state, BattlefieldState bf)

    {

        BattlefieldUnit? best = null;

        foreach (var u in bf.units)

        {

            if (u.side != UnitSide.FRIENDLY || u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u))

            {

                continue;

            }



            if (u.warpPhase == TacticalWarpPhase.None)

            {

                continue;

            }



            if (!IsFocusEligible(u, bf.timeSec)

                || !IsTacticalCameraEligible(state, u, bf.timeSec))

            {

                continue;

            }



            if (!string.IsNullOrEmpty(state.tacticalCameraUnitId)

                && state.tacticalCameraUnitId.Equals(u.unitId, StringComparison.Ordinal))

            {

                return u;

            }



            best ??= u;

        }



        return best;

    }



    private static BattlefieldUnit? FindTransitSceneProxyFocus(GameState state, BattlefieldState bf)

    {

        if (bf.battlefieldId == null)

        {

            return null;

        }



        foreach (var entry in state.tacticalWarpInTransit)

        {

            if (entry.unit.side != UnitSide.FRIENDLY || entry.unit.IsDestroyed())

            {

                continue;

            }



            if (!bf.battlefieldId.Equals(entry.fromBattlefieldId, StringComparison.Ordinal))

            {

                continue;

            }



            var proxy = FindSceneProxyForDestination(state, bf, entry.toBattlefieldId);

            if (proxy != null)

            {

                return proxy;

            }

        }



        return null;

    }



    private static BattlefieldUnit? FindSceneProxyForDestination(

        GameState state,

        BattlefieldState bf,

        string? targetBattlefieldId)

    {

        if (targetBattlefieldId == null)

        {

            return null;

        }



        BattlefieldState? targetBf = null;

        foreach (var candidate in state.battlefields)

        {

            if (targetBattlefieldId.Equals(candidate.battlefieldId, StringComparison.Ordinal))

            {

                targetBf = candidate;

                break;

            }

        }



        if (targetBf?.eventRegionId == null)

        {

            return null;

        }



        foreach (var u in bf.units)

        {

            if (!BattlefieldSceneProxyService.IsSceneProxy(u))

            {

                continue;

            }



            if (targetBf.eventRegionId.Equals(u.sceneProxyTargetEventRegionId, StringComparison.Ordinal))

            {

                return u;

            }

        }



        return null;

    }



    private static bool IsTacticalCameraEligible(GameState state, BattlefieldUnit u, float battleTimeSec)

    {

        if (!IsPossessableFriendly(u, battleTimeSec))

        {

            return false;

        }



        if (!SkirmishBuildingRules.IsSkirmish(state))

        {

            return true;

        }



        var member = FindMember(state, u.memberId!);

        return member != null && VisionLocationService.IsVisionEligibleMember(member, state);

    }



    private static bool IsFocusEligible(BattlefieldUnit u, float battleTimeSec) =>

        !u.IsDestroyed()

        && (u.Arrived(battleTimeSec) || u.warpPhase != TacticalWarpPhase.None);



    private static bool IsPossessableFriendly(BattlefieldUnit u, float battleTimeSec) =>

        u.side == UnitSide.FRIENDLY

        && u.alive

        && u.memberId != null

        && !u.IsDestroyed()

        && !BattlefieldSceneProxyService.IsSceneProxy(u)

        && (u.Arrived(battleTimeSec) || u.warpPhase != TacticalWarpPhase.None);



    public static List<BattlefieldUnit> ListPossessableFriendlies(GameState state, BattlefieldState bf)

    {

        var list = new List<BattlefieldUnit>();

        foreach (var u in bf.units)

        {

            if (!IsPossessableFriendly(u, bf.timeSec))

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

        var list = ListTacticalFocusCandidates(state, bf);

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



    public static List<BattlefieldUnit> ListTacticalFocusCandidates(GameState state, BattlefieldState bf)

    {

        var list = new List<BattlefieldUnit>();

        foreach (var u in bf.units)

        {

            if (u.unitId == null || u.IsDestroyed())

            {

                continue;

            }



            if (u.side == UnitSide.FRIENDLY

                && !BattlefieldSceneProxyService.IsSceneProxy(u)

                && (u.Arrived(bf.timeSec) || u.warpPhase != TacticalWarpPhase.None)

                && IsTacticalCameraEligible(state, u, bf.timeSec))

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



    private static BattlefieldUnit? FindTransitUnitOnBattlefield(

        GameState state,

        BattlefieldState bf,

        string unitId)

    {

        if (bf.battlefieldId == null)

        {

            return null;

        }



        foreach (var entry in state.tacticalWarpInTransit)

        {

            if (!unitId.Equals(entry.unit.unitId, StringComparison.Ordinal))

            {

                continue;

            }



            if (bf.battlefieldId.Equals(entry.fromBattlefieldId, StringComparison.Ordinal)

                || bf.battlefieldId.Equals(entry.toBattlefieldId, StringComparison.Ordinal))

            {

                return entry.unit;

            }

        }



        return null;

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


