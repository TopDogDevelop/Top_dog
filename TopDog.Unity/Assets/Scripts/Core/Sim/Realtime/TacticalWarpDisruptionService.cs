using TopDog.Content.Modules;

namespace TopDog.Sim.Realtime;

/// <summary>跃迁干扰（跃迁扰断等）；未干扰时的发起条件见 <see cref="TacticalWarpService"/>。</summary>
public static class TacticalWarpDisruptionService
{
    public const float WarpScramRangeM = 30_000f;

    public static bool IsWarpScrambled(BattlefieldState bf, BattlefieldUnit u, ModuleRegistry? modules = null)
    {
        modules ??= ModuleRegistry.LoadDefault();
        foreach (var other in bf.units)
        {
            if (other.side == u.side
                || other.IsDestroyed()
                || BattlefieldSceneProxyService.IsSceneProxy(other)
                || other.isBuilding
                || !other.Arrived(bf.timeSec))
            {
                continue;
            }

            var dx = other.x - u.x;
            var dy = other.y - u.y;
            var dz = other.z - u.z;
            var dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            if (dist > WarpScramRangeM)
            {
                continue;
            }

            if (HasWarpScramModule(other, modules))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasWarpScramModule(BattlefieldUnit u, ModuleRegistry modules)
    {
        foreach (var modId in u.fittedModules.Values)
        {
            if (modId != null && modId.Contains("warp_scram", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var def = modules.Resolve(modId);
            if (def?.moduleId != null && def.moduleId.Contains("warp_scram", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
