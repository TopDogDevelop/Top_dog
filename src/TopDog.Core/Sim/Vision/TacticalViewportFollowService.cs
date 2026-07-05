using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Vision;

/// <summary>实时战：友舰离场/跃迁后自动切换 activeBattlefieldId 到有友舰的战场。</summary>
public static class TacticalViewportFollowService
{
    public static void Tick(GameState state)
    {
        if (!state.combatRealtimeActive || state.battlefields.Count == 0)
        {
            return;
        }

        var best = FindBestBattlefield(state);
        if (best?.battlefieldId == null)
        {
            return;
        }

        if (state.activeBattlefieldId != null
            && state.activeBattlefieldId.Equals(best.battlefieldId, StringComparison.Ordinal))
        {
            return;
        }

        var active = FindBattlefield(state, state.activeBattlefieldId);
        if (active != null && HasWarpPipelineFriendlies(state, active))
        {
            return;
        }

        var activeScore = active != null ? VisionGate.CountFriendlyFollowScore(state, active) : 0;
        var bestScore = VisionGate.CountFriendlyFollowScore(state, best);
        if (activeScore > 0 && bestScore <= activeScore)
        {
            return;
        }

        if (activeScore == 0 && bestScore == 0)
        {
            return;
        }

        state.activeBattlefieldId = best.battlefieldId;
        if (state.possessingMemberId != null
            && (active == null || !UnitOnField(active, state.possessingMemberId)))
        {
            state.possessingMemberId = null;
        }
    }

    private static bool HasWarpPipelineFriendlies(GameState state, BattlefieldState bf)
    {
        if (bf.battlefieldId == null)
        {
            return false;
        }

        foreach (var u in bf.units)
        {
            if (u.side == UnitSide.FRIENDLY && !u.IsDestroyed() && u.warpPhase != TacticalWarpPhase.None)
            {
                return true;
            }
        }

        return false;
    }

    private static BattlefieldState? FindBestBattlefield(GameState state)
    {
        BattlefieldState? best = null;
        var bestCount = -1;
        foreach (var bf in state.battlefields)
        {
            if (bf.finished || bf.battlefieldId == null)
            {
                continue;
            }

            var count = VisionGate.CountFriendlyFollowScore(state, bf);
            if (count > bestCount)
            {
                bestCount = count;
                best = bf;
            }
        }

        return bestCount > 0 ? best : null;
    }

    private static BattlefieldState? FindBattlefield(GameState state, string? id)
    {
        if (id == null)
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            if (id.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
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
}
