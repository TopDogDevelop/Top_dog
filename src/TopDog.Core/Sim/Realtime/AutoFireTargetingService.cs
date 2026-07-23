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
        if (u.isBuilding || u.explicitFocus
            || BattlefieldSceneProxyService.IsSceneProxy(u))
        {
            return;
        }

        if (!state.autoFireEnabled)
        {
            return;
        }

        if (u.targetUnitId != null)
        {
            var existing = BattlefieldSystem.FindUnit(bf, u.targetUnitId);
            if (existing != null && !existing.IsDestroyed() && existing.Arrived(bf.timeSec)
                && !BattlefieldSceneProxyService.IsSceneProxy(existing)
                && CombatHostility.AreHostile(u, existing))
            {
                return;
            }
        }

        u.targetUnitId = FindNearestEnemyId(bf, u);
    }

    public static string? FindNearestEnemyId(BattlefieldState bf, BattlefieldUnit u)
    {
        // Prefer spatial hash when rebuilt this tick; else full scan (legacy small battles).
        if (bf.spatialHash != null)
        {
            BattlefieldUnit? best = null;
            var bestDist = float.MaxValue;
            const int explore = 256;
            foreach (var other in bf.spatialHash.QueryExpanding(u.x, u.y, u.z, explore))
            {
                if (!CombatHostility.AreHostile(u, other) || other.IsDestroyed() || !other.Arrived(bf.timeSec)
                    || other.IsBallisticMissile()
                    || BattlefieldSceneProxyService.IsSceneProxy(other)
                    || other.unitId == u.unitId)
                {
                    continue;
                }

                var dx = other.x - u.x;
                var dy = other.y - u.y;
                var dz = other.z - u.z;
                var d = dx * dx + dy * dy + dz * dz;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = other;
                }
            }

            return best?.unitId;
        }

        BattlefieldUnit? bestFull = null;
        var bestDistFull = float.MaxValue;
        foreach (var other in bf.units)
        {
            if (!CombatHostility.AreHostile(u, other) || other.IsDestroyed() || !other.Arrived(bf.timeSec)
                || other.IsBallisticMissile()
                || BattlefieldSceneProxyService.IsSceneProxy(other))
            {
                continue;
            }

            var dx = other.x - u.x;
            var dy = other.y - u.y;
            var dz = other.z - u.z;
            var d = dx * dx + dy * dy + dz * dz;
            if (d < bestDistFull)
            {
                bestDistFull = d;
                bestFull = other;
            }
        }

        return bestFull?.unitId;
    }
}
