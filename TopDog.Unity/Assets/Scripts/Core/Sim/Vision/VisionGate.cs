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

    /// <summary>右栏「可降临战场」：任一友舰在场、跃迁中或途中的战场（不要求直接视野）。</summary>
    public static List<BattlefieldState> ListRailBattlefields(GameState state)
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
            if (bf.finished || bf.battlefieldId == null)
            {
                continue;
            }

            if (CountFriendlyPresence(state, bf) > 0
                || HasDirectBattlefieldView(state, bf.battlefieldId))
            {
                list.Add(bf);
            }
        }

        return list;
    }

    public static int CountFriendlyPresence(GameState state, BattlefieldState bf)
    {
        return CountOnFieldFriendlies(bf) + CountTransitFriendlies(state, bf);
    }

    public static int CountOnFieldFriendlies(BattlefieldState bf)
    {
        var count = 0;
        foreach (var u in bf.units)
        {
            if (u.side == UnitSide.FRIENDLY && !u.IsDestroyed() && u.memberId != null
                && !BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>跃迁途中：目标战场计 +1；右栏/旧逻辑仍计来源战场。</summary>
    public static int CountTransitFriendlies(GameState state, BattlefieldState bf)
    {
        if (bf.battlefieldId == null)
        {
            return 0;
        }

        var count = 0;
        foreach (var entry in state.tacticalWarpInTransit)
        {
            if (entry.unit.side != UnitSide.FRIENDLY || entry.unit.IsDestroyed())
            {
                continue;
            }

            if (bf.battlefieldId.Equals(entry.toBattlefieldId, StringComparison.Ordinal)
                || bf.battlefieldId.Equals(entry.fromBattlefieldId, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>视角跟随：场上友军 + 跃迁目标为该战场的友军（不计仍挂在来源上的在途）。</summary>
    public static int CountFriendlyFollowScore(GameState state, BattlefieldState bf)
    {
        var score = CountOnFieldFriendlies(bf);
        if (bf.battlefieldId == null)
        {
            return score;
        }

        foreach (var entry in state.tacticalWarpInTransit)
        {
            if (entry.unit.side != UnitSide.FRIENDLY || entry.unit.IsDestroyed())
            {
                continue;
            }

            if (bf.battlefieldId.Equals(entry.toBattlefieldId, StringComparison.Ordinal))
            {
                score++;
            }
        }

        return score;
    }

    public static (int friendly, int enemy, int total) CountCombatUnits(BattlefieldState bf)
    {
        var friendly = 0;
        var enemy = 0;
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }

            if (u.side == UnitSide.FRIENDLY)
            {
                friendly++;
            }
            else if (u.side == UnitSide.ENEMY)
            {
                enemy++;
            }
        }

        return (friendly, enemy, friendly + enemy);
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
