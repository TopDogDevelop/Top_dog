using TopDog.Content.Modules;

namespace TopDog.Sim.Operations;

public static class MiningModuleHelper
{
    public static bool IsMiningModule(ModuleDef? mod)
    {
        if (mod == null)
        {
            return false;
        }
        if ("mining_beam".Equals(mod.moduleKind, StringComparison.Ordinal))
        {
            return true;
        }
        return mod.moduleId != null
               && mod.moduleId.Contains("ore_mining", StringComparison.Ordinal);
    }

    public static int YieldPerOpsPhase(ModuleDef mod) =>
        mod.miningYieldPerOpsPhase > 0f ? (int)mod.miningYieldPerOpsPhase : 500;

    public static string ResourceId(ModuleDef mod) =>
        string.IsNullOrWhiteSpace(mod.miningResourceId) ? ResourceIds.Inorganic : mod.miningResourceId!;
}
