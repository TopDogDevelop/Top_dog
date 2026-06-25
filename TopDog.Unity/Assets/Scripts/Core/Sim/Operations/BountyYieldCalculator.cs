using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;

namespace TopDog.Sim.Operations;

public static class BountyYieldCalculator
{
    public static int ComputeDps(
        HullDef? hull,
        IReadOnlyDictionary<string, string> fit,
        ModuleRegistry modules)
    {
        if (hull == null)
        {
            return 0;
        }
        var stats = ShipFitStats.Compute(hull, fit, modules);
        var slotCount = MemberFittingService.ListOpenSlots(hull).Count;
        var enableCap = FittingEnableSummary.SimultaneousEnableLimit(hull, slotCount);
        if (enableCap <= 0 || stats.dps <= 0f)
        {
            return 0;
        }
        var weapons = 0;
        foreach (var slotKey in MemberFittingService.ListOpenSlots(hull))
        {
            if (!slotKey.StartsWith("atk_", StringComparison.Ordinal))
            {
                continue;
            }
            if (!fit.TryGetValue(slotKey, out var modId) || string.IsNullOrWhiteSpace(modId))
            {
                continue;
            }
            var mod = modules.Resolve(modId);
            if (mod?.damagePerTick > 0f)
            {
                weapons++;
            }
        }
        var active = Math.Min(weapons, enableCap);
        if (active <= 0)
        {
            return 0;
        }
        return Math.Max(0, (int)Math.Floor(stats.dps));
    }
}
