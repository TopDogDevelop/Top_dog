using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Operations;

public static class DispatchIncomeHelper
{
    public static bool IsMiningTask(string? task) =>
        MemberDispatchService.TaskMining.Equals(task, StringComparison.Ordinal)
        || "挖矿".Equals(task, StringComparison.Ordinal);

    public static bool IsBountyTask(string? task) =>
        MemberDispatchService.TaskBounty.Equals(task, StringComparison.Ordinal)
        || "刷赏".Equals(task, StringComparison.Ordinal);

    public static bool IsLegionIncome(MemberState m) =>
        m.playerDispatchActive
        && m.playerChoseDeployRegion
        && !string.IsNullOrWhiteSpace(m.opsDeployEventRegionId)
        && (IsMiningTask(m.assignedTask) || IsBountyTask(m.assignedTask));

    public static void CreditLegion(GameState state, string itemId, int qty)
    {
        if (qty <= 0)
        {
            return;
        }
        LegionRegistry.CreditLocal(state, itemId, qty);
    }
}
