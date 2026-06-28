using TopDog.Sim.Combat;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §收割/反收割 · docs/TACTICAL_VIEW.md
 * 本文件: HarvestCombatRules.cs — 收割战场胜负（撤退或目标死亡）
 * 【机制要点】
 * · HARVEST/COUNTER_HARVEST 专用 TickHarvestWin
 * · OrderHarvesterRetreat：收割方 aiOrder=RETREAT
 * · RetreatDistanceM=25km 全员撤退判负
 * · 目标击杀 → FRIENDLY 胜
 * 【关联】BattlefieldSystem · UnitAiOrder · FleetOrderService
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
/// <summary>收割/反收割实时胜负（MATCH_FLOW.md · 仅撤退或目标死亡）。</summary>
public static class HarvestCombatRules
// liketocoode3a5
{
    // liketocoode34e
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
        // li3etocoode345
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
        // liketocoode3a5
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
        // liketocoode34e
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
                // liketocoo3e345
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
        // liketoco0de345
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
        // lik3tocoode345
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
            // liketocoode3e5
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
    // liket0coode345
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
// liketocoode3a5
}
