using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

public static class AutoFireTargetingService
{
    public static void Tick(BattlefieldState bf, GameState state, BattlefieldUnit u)
    {
        if (u.isBuilding || u.side != UnitSide.FRIENDLY || u.explicitFocus)
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
            if (existing != null && !existing.IsDestroyed() && existing.Arrived(bf.timeSec))
            {
                return;
            }
        }

        u.targetUnitId = FindNearestEnemyId(bf, u);
    }

    public static string? FindNearestEnemyId(BattlefieldState bf, BattlefieldUnit u)
    {
        BattlefieldUnit? best = null;
        var bestDist = float.MaxValue;
        foreach (var other in bf.units)
        {
            if (other.side == u.side || other.IsDestroyed() || !other.Arrived(bf.timeSec))
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
}
