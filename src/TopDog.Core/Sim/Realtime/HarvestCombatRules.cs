using TopDog.Sim.Combat;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>收割/反收割实时胜负（MATCH_FLOW.md · 仅撤退或目标死亡）。</summary>
public static class HarvestCombatRules
{
    public const float RetreatDistanceM = 25_000f;

    public static bool IsHarvestBattlefield(BattlefieldState bf) =>
        bf.combatSubtype is CombatSubtype.HARVEST or CombatSubtype.COUNTER_HARVEST;

    public static void TickHarvestWin(GameState state, BattlefieldState bf)
    {
        if (bf.finished || !IsHarvestBattlefield(bf))
        {
            return;
        }

        if (TryFinishTargetKilled(bf))
        {
            return;
        }

        if (bf.harvesterRetreatRequested && TryFinishHarvesterRetreat(bf))
        {
            return;
        }
    }

    public static string OrderHarvesterRetreat(GameState state, BattlefieldState bf)
    {
        if (!IsHarvestBattlefield(bf))
        {
            return "非收割战场";
        }

        bf.harvesterRetreatRequested = true;
        var count = 0;
        foreach (var u in bf.units)
        {
            if (!IsHarvesterSideUnit(bf, u) || u.IsDestroyed() || u.isBuilding)
            {
                continue;
            }
            u.aiOrder = UnitAiOrder.RETREAT;
            u.throttleOn = true;
            count++;
        }
        return count > 0 ? "收割方撤退 " + count + " 艘" : "无可撤退舰";
    }

    private static bool TryFinishTargetKilled(BattlefieldState bf)
    {
        var targetId = bf.capturedMemberId;
        if (targetId == null)
        {
            foreach (var u in bf.units)
            {
                if (u.side == UnitSide.ENEMY && !u.isBuilding && u.alive)
                {
                    targetId = u.memberId ?? u.unitId;
                    break;
                }
            }
        }

        BattlefieldUnit? target = null;
        foreach (var u in bf.units)
        {
            if (targetId != null
                && (targetId.Equals(u.memberId, StringComparison.Ordinal)
                    || targetId.Equals(u.unitId, StringComparison.Ordinal)))
            {
                target = u;
                break;
            }
        }

        if (target == null || target.IsDestroyed())
        {
            bf.finished = true;
            bf.winnerSide = UnitSide.FRIENDLY;
            bf.winReason = "harvest_target_killed";
            return true;
        }

        return false;
    }

    private static bool TryFinishHarvesterRetreat(BattlefieldState bf)
    {
        var anyHarvesterAlive = false;
        var allRetreated = true;
        foreach (var u in bf.units)
        {
            if (!IsHarvesterSideUnit(bf, u) || u.isBuilding)
            {
                continue;
            }
            if (u.IsDestroyed())
            {
                continue;
            }
            anyHarvesterAlive = true;
            var dist = (float)Math.Sqrt(u.x * u.x + u.y * u.y + u.z * u.z);
            if (u.aiOrder != UnitAiOrder.RETREAT || dist < RetreatDistanceM)
            {
                allRetreated = false;
            }
        }

        if (!anyHarvesterAlive || allRetreated)
        {
            bf.finished = true;
            bf.winnerSide = UnitSide.ENEMY;
            bf.winReason = "harvester_retreat";
            return true;
        }

        return false;
    }

    private static bool IsHarvesterSideUnit(BattlefieldState bf, BattlefieldUnit u)
    {
        if (bf.combatSubtype == CombatSubtype.HARVEST)
        {
            return u.side == UnitSide.FRIENDLY;
        }
        if (bf.combatSubtype == CombatSubtype.COUNTER_HARVEST)
        {
            return u.side == UnitSide.ENEMY;
        }
        return false;
    }
}
