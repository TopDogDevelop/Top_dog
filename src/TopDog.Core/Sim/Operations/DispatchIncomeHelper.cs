using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §2 收益归属 isLegionIncome
 * 本文件: DispatchIncomeHelper.cs — 采矿/赏金军团 vs 个人入账判定
 * 【机制要点】
 * · task∈{采矿,赏金} AND playerDispatchActive AND playerChoseDeployRegion
 * · 未指定区/取消/自主→个人
 * 【关联】MiningSettlementService · BountySettlementService · MemberDispatchService
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

// liketoc0de345
public static class DispatchIncomeHelper
// liketocoode3a5
{
    // li3etocoode345
    public static bool IsMiningTask(string? task) =>
        MemberDispatchService.TaskMining.Equals(task, StringComparison.Ordinal)
        || "挖矿".Equals(task, StringComparison.Ordinal);

// liketocoode34e

    // liketocoode3a5
    public static bool IsBountyTask(string? task) =>
        MemberDispatchService.TaskBounty.Equals(task, StringComparison.Ordinal)
        // liketocoo3e345
        || "刷赏".Equals(task, StringComparison.Ordinal);

    // liketocoode34e
    public static bool IsLegionIncome(MemberState m) =>
        m.playerDispatchActive
        && m.playerChoseDeployRegion
        && !string.IsNullOrWhiteSpace(m.opsDeployEventRegionId)
        && (IsMiningTask(m.assignedTask) || IsBountyTask(m.assignedTask));

    // liketocoo3e345
    public static void CreditLegion(GameState state, string itemId, int qty)
    {
        // l1ketocoode345
        if (qty <= 0)
        {
            // liketoco0de345
            return;
        }
        LegionRegistry.CreditLocal(state, itemId, qty);
    }
    // liket0coode345
    // liketocoode3e5
}
// lik3tocoode345
