using TopDog.Content.Modules;

namespace TopDog.Sim.Realtime;

/// <summary>跃迁干扰（跃迁扰断等）；未干扰时的发起条件见 <see cref="TacticalWarpService"/>。</summary>
public static class TacticalWarpDisruptionService
{
    public const float WarpScramRangeM = 30_000f;
    public const float DefaultWarpScramStrength = 2f;

    public static float EffectiveWarpScramStrength(
        BattlefieldState bf,
        BattlefieldUnit u,
        ModuleRegistry? modules = null)
    {
        modules ??= ModuleRegistry.LoadDefault();
        var total = 0f;
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

            total += SumWarpScramStrength(other, modules);
        }

        return total;
    }

    public static bool IsWarpScrambled(
        BattlefieldState bf,
        BattlefieldUnit u,
        float warpScramResist,
        ModuleRegistry? modules = null)
    {
        var strength = EffectiveWarpScramStrength(bf, u, modules);
        return strength > 0f && warpScramResist <= strength;
    }

    [Obsolete("Use EffectiveWarpScramStrength with resist comparison.")]
    public static bool IsWarpScrambled(BattlefieldState bf, BattlefieldUnit u, ModuleRegistry? modules = null) =>
        EffectiveWarpScramStrength(bf, u, modules) > 0f;

    private static float SumWarpScramStrength(BattlefieldUnit u, ModuleRegistry modules)
    {
        var total = 0f;
        foreach (var modId in u.fittedModules.Values)
        {
            if (modId == null)
            {
                continue;
            }

            if (!IsWarpScramModuleId(modId))
            {
                var def = modules.Resolve(modId);
                if (def?.moduleId == null || !IsWarpScramModuleId(def.moduleId))
                {
                    continue;
                }

                total += ResolveWarpScramStrength(def);
                continue;
            }

            var resolved = modules.Resolve(modId);
            total += ResolveWarpScramStrength(resolved);
        }

        return total;
    }

    public static bool IsWarpScramModuleId(string modId) =>
        modId.Contains("warp_scram", StringComparison.OrdinalIgnoreCase);

    public static float ResolveWarpScramStrength(ModuleDef? def)
    {
        if (def == null)
        {
            return DefaultWarpScramStrength;
        }

        if (def.warpScramStrength > 0f)
        {
            return def.warpScramStrength;
        }

        if (def.moduleId != null && IsWarpScramModuleId(def.moduleId))
        {
            return DefaultWarpScramStrength;
        }

        return 0f;
    }

    private static bool HasWarpScramModule(BattlefieldUnit u, ModuleRegistry modules) =>
        SumWarpScramStrength(u, modules) > 0f;
}
