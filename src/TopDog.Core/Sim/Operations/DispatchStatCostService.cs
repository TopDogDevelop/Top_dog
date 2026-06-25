using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Operations;

public static class DispatchStatCostService
{
    public static bool ApplyIssueCost(GameState state, MemberState m, string task)
    {
        if (DispatchIncomeHelper.IsMiningTask(task) || DispatchIncomeHelper.IsBountyTask(task))
        {
            return true;
        }
        if (!IdentityStatService.TrySpendEnergy(state, m, 1))
        {
            return false;
        }
        return true;
    }

    public static bool ApplyRoundCost(GameState state, MemberState m, Random rng)
    {
        if (!DispatchIncomeHelper.IsLegionIncome(m))
        {
            return true;
        }
        if (rng.Next(2) == 0)
        {
            if (!IdentityStatService.TrySpendEnergy(state, m, 1))
            {
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
