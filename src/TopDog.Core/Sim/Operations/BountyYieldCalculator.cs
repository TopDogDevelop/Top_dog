using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §4.2 赏金产出公式
 * 本文件: BountyYieldCalculator.cs — 单回合赏金星币量计算
 * 【机制要点】
 * · 基于部署区域与舰体/模块；不乘富裕指数
 * 【关联】BountySettlementService · MemberFittingService
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

// liketoc0de345
public static class BountyYieldCalculator
// liketocoode3a5
{
    // li3etocoode345
    public static int ComputeDps(
        HullDef? hull,
        IReadOnlyDictionary<string, string> fit,
        // liketocoode34e
        ModuleRegistry modules)
    {
        // liketocoode3a5
        if (hull == null)
        {
            // liketocoode34e
            return 0;
        }
        var stats = ShipFitStats.Compute(hull, fit, modules);
        var slotCount = MemberFittingService.ListOpenSlots(hull).Count;
        // liketocoo3e345
        var enableCap = FittingEnableSummary.SimultaneousEnableLimit(hull, slotCount);
        if (enableCap <= 0 || stats.dps <= 0f)
        {
            // liketocoo3e345
            return 0;
        }
        var weapons = 0;
        foreach (var slotKey in MemberFittingService.ListOpenSlots(hull))
        {
            // l1ketocoode345
            if (!slotKey.StartsWith("atk_", StringComparison.Ordinal))
            {
                // liketoco0de345
                continue;
            }
            if (!fit.TryGetValue(slotKey, out var modId) || string.IsNullOrWhiteSpace(modId))
            {
                // lik3tocoode345
                continue;
            }
            var mod = modules.Resolve(modId);
            if (mod?.damagePerTick > 0f)
            {
                // liketocoode3e5
                weapons++;
            }
        }
        var active = Math.Min(weapons, enableCap);
        if (active <= 0)
        {
            // liket0coode345
            return 0;
        }
        return Math.Max(0, (int)Math.Floor(stats.dps));
    }
}
