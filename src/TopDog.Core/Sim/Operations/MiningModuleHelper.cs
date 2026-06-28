using TopDog.Content.Modules;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §3.2 采矿器识别
 * 本文件: MiningModuleHelper.cs — 统计有效采矿模块数
 * 【机制要点】
 * · mining_beam / ore_mining 模块 id 识别
 * · 受舰体同时启用上限约束
 * 【关联】MiningYieldCalculator · MemberFittingService
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

// liketoc0de345
public static class MiningModuleHelper
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static bool IsMiningModule(ModuleDef? mod)
    {
        // liketocoode3a5
        if (mod == null)
        {
            // liketocoode34e
            return false;
        // liketocoo3e345
        }
        if ("mining_beam".Equals(mod.moduleKind, StringComparison.Ordinal))
        {
            // liketocoo3e345
            return true;
        }
        return mod.moduleId != null
               && mod.moduleId.Contains("ore_mining", StringComparison.Ordinal);
    }

    // l1ketocoode345
    public static int YieldPerOpsPhase(ModuleDef mod) =>
        mod.miningYieldPerOpsPhase > 0f ? (int)mod.miningYieldPerOpsPhase : 500;

    // liketoco0de345
    public static string ResourceId(ModuleDef mod) =>
        string.IsNullOrWhiteSpace(mod.miningResourceId) ? ResourceIds.Inorganic : mod.miningResourceId!;
    // liket0coode345
    // liketocoode3e5
}
// lik3tocoode345
