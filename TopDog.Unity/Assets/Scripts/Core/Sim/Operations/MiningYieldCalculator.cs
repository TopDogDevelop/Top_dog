using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;

namespace TopDog.Sim.Operations;

public readonly struct MiningYieldResult
{
    public readonly int FittedMinerCount;
    public readonly int ActiveMinerCount;
    public readonly int SimultaneousEnableCap;
    public readonly int TotalYield;
    public readonly string ResourceId;

    public MiningYieldResult(
        int fittedMinerCount,
        int activeMinerCount,
        int simultaneousEnableCap,
        int totalYield,
        string resourceId)
    {
        FittedMinerCount = fittedMinerCount;
        ActiveMinerCount = activeMinerCount;
        SimultaneousEnableCap = simultaneousEnableCap;
        TotalYield = totalYield;
        ResourceId = resourceId;
    }
}

public static class MiningYieldCalculator
{
    /// <summary>
    /// Active miners = min(fitted mining modules, hull simultaneous-enable cap).
    /// Total yield = sum of each active miner's per-phase output.
    /// </summary>
    public static MiningYieldResult Compute(
        HullDef? hull,
        IReadOnlyDictionary<string, string> fit,
        ModuleRegistry modules,
        string? defaultResourceId = null)
    {
        if (hull == null)
        {
            return Empty(defaultResourceId);
        }

        var miners = CollectFittedMiners(hull, fit, modules);
        if (miners.Count == 0)
        {
            return Empty(defaultResourceId);
        }

        var slotCount = MemberFittingService.ListOpenSlots(hull).Count;
        var enableCap = FittingEnableSummary.SimultaneousEnableLimit(hull, slotCount);
        var activeCount = Math.Min(miners.Count, enableCap);
        var resourceId = string.IsNullOrWhiteSpace(defaultResourceId)
            ? ResourceIds.Inorganic
            : defaultResourceId!;
        var totalYield = 0;
        for (var i = 0; i < activeCount; i++)
        {
            totalYield += MiningModuleHelper.YieldPerOpsPhase(miners[i]);
            resourceId = MiningModuleHelper.ResourceId(miners[i]);
        }

        return new MiningYieldResult(miners.Count, activeCount, enableCap, totalYield, resourceId);
    }

    private static List<ModuleDef> CollectFittedMiners(
        HullDef hull,
        IReadOnlyDictionary<string, string> fit,
        ModuleRegistry modules)
    {
        var miners = new List<ModuleDef>();
        foreach (var slotKey in MemberFittingService.ListOpenSlots(hull))
        {
            if (!fit.TryGetValue(slotKey, out var modId) || string.IsNullOrWhiteSpace(modId))
            {
                continue;
            }

            var mod = modules.Resolve(modId);
            if (MiningModuleHelper.IsMiningModule(mod))
            {
                miners.Add(mod!);
            }
        }

        return miners;
    }

    private static MiningYieldResult Empty(string? defaultResourceId) =>
        new(0, 0, 0, 0, defaultResourceId ?? ResourceIds.Inorganic);
}
