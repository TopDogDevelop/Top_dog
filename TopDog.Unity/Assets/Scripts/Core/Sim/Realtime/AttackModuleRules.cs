using TopDog.Content.Modules;

namespace TopDog.Sim.Realtime;

/// <summary>攻击模块射程/跟踪默认值（FIRST_PACK_CONTENT §4–6）。</summary>
internal static class AttackModuleRules
{
    public static float ResolveAttackRangeM(ModuleDef mod)
    {
        if (mod.attackRangeM > 0.01f)
        {
            return mod.attackRangeM;
        }

        return mod.moduleSize switch
        {
            "EXTRA_LARGE" => 100_000f,
            "LARGE" => 70_000f,
            "MEDIUM" => 30_000f,
            "SMALL" => 15_000f,
            _ => 8_000f,
        };
    }

    public static float ResolveTrackingDegPerSec(ModuleDef mod)
    {
        if (mod.trackingDegPerSec > 0.001f)
        {
            return mod.trackingDegPerSec;
        }

        return mod.moduleSize switch
        {
            "EXTRA_LARGE" => 0.5f,
            "LARGE" => 3f,
            "MEDIUM" => 10f,
            "SMALL" => 15f,
            _ => 0f,
        };
    }
}
