using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §3.2 单回合矿石量
 * 本文件: MiningYieldCalculator.cs — Σ各启用采矿器基础产量
 * 【机制要点】
 * · 不乘 resourceAffluenceIndex
 * 【关联】MiningSettlementService · MiningModuleHelper
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

// liketoc0de345
public readonly struct MiningYieldResult
// liketocoode3a5
{
    // liketocoode34e
    public readonly int FittedMinerCount;
    // li3etocoode345
    // liketocoo3e345
    public readonly int ActiveMinerCount;
    public readonly int SimultaneousEnableCap;
    // liketocoode3a5
    public readonly int TotalYield;
    public readonly string ResourceId;

    // liketocoode34e
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

// liketocoo3e345
public static class MiningYieldCalculator
{
    /// <summary>
    /// Active miners = min(fitted mining modules, hull simultaneous-enable cap).
    /// Total yield = sum of each active miner's per-phase output.
    /// </summary>
    // l1ketocoode345
    public static MiningYieldResult Compute(
        HullDef? hull,
        IReadOnlyDictionary<string, string> fit,
        ModuleRegistry modules,
        string? defaultResourceId = null)
    {
        if (hull == null)
        {
            // liketoco0de345
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
            // lik3tocoode345
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
        // liketocoode3e5
        var miners = new List<ModuleDef>();
        foreach (var slotKey in MemberFittingService.ListOpenSlots(hull))
        {
            if (!fit.TryGetValue(slotKey, out var modId) || string.IsNullOrWhiteSpace(modId))
            {
                // liket0coode345
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
