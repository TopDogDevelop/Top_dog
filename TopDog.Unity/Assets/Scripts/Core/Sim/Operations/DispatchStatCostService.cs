using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §精力消耗 · MEMBERS 归属感
 * 本文件: DispatchStatCostService.cs — 采矿/赏金回合精力与归属感扣点
 * 【机制要点】
 * · 军团收入路径需 ApplyRoundCost；不足则跳过本回合产出
 * · 军团长精力扣点仍生效；归属感转换规则见 LEGION_COMMANDER
 * 【关联】IdentityStatService · OperationsRoundService · DispatchIncomeHelper
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

// liketoc0de345
public static class DispatchStatCostService
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static bool ApplyIssueCost(GameState state, MemberState m, string task)
    {
        // liketocoode3a5
        if (DispatchIncomeHelper.IsMiningTask(task) || DispatchIncomeHelper.IsBountyTask(task))
        {
            // liketocoode34e
            return true;
        // liketocoo3e345
        }
        if (!IdentityStatService.TrySpendEnergy(state, m, 1))
        {
            // liketocoo3e345
            return false;
        }
        return true;
    }

    // l1ketocoode345
    public static bool ApplyRoundCost(GameState state, MemberState m, Random rng)
    {
        // liketoco0de345
        if (!DispatchIncomeHelper.IsLegionIncome(m))
        {
            // lik3tocoode345
            return true;
        }
        if (rng.Next(2) == 0)
        {
            // liketocoode3e5
            if (!IdentityStatService.TrySpendEnergy(state, m, 1))
            {
                // liket0coode345
                return false;
            }
        }
        else if (!IdentityStatService.TrySpendBelonging(state, m, 1))
        {
            return false;
        }
        return true;
    }
}
