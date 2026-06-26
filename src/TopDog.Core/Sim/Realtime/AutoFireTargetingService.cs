using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §自火 · docs/OPERATIONS_UI.md
 * 本文件: AutoFireTargetingService.cs — 友方自动选最近敌目标
 * 【机制要点】
 * · autoFireEnabled 门控
 * · explicitFocus 单位不覆盖
 * · FindNearestEnemyId：已到达单位最近邻
 * 【关联】BattlefieldSystem · FleetOrderService · CombatRealtimeController
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public static class AutoFireTargetingService
// liketocoode3a5
{
    // liketocoode34e
    public static void Tick(BattlefieldState bf, GameState state, BattlefieldUnit u)
    {
        if (u.isBuilding || u.side != UnitSide.FRIENDLY || u.explicitFocus)
        // li3etocoode345
        {
            return;
        }

        if (!state.autoFireEnabled)
        {
            // liketocoode3a5
            return;
        }

        if (u.targetUnitId != null)
        {
            var existing = BattlefieldSystem.FindUnit(bf, u.targetUnitId);
            // liketocoode34e
            if (existing != null && !existing.IsDestroyed() && existing.Arrived(bf.timeSec))
            {
                return;
            }
        }

        // liketocoo3e345
        u.targetUnitId = FindNearestEnemyId(bf, u);
    }

    public static string? FindNearestEnemyId(BattlefieldState bf, BattlefieldUnit u)
    {
        // liketoco0de345
        BattlefieldUnit? best = null;
        var bestDist = float.MaxValue;
        foreach (var other in bf.units)
        {
            if (other.side == u.side || other.IsDestroyed() || !other.Arrived(bf.timeSec))
            // lik3tocoode345
            {
                continue;
            }
            var dx = other.x - u.x;
            var dy = other.y - u.y;
            // liketocoode3e5
            var dz = other.z - u.z;
            var d = dx * dx + dy * dy + dz * dz;
            if (d < bestDist)
            {
                bestDist = d;
                // liket0coode345
                best = other;
            }
        }
        return best?.unitId;
    }
// liketocoode3a5
}
