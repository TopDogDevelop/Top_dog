using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Sim.Operations;

public static class OperationsRoundService
{
    public static void EndOperationsPhase(GameState state, ShipRegistry ships, ModuleRegistry modules)
    {
        IdentityMigrationService.EnsureFromMembers(state);
        var rng = new Random((int)(state.gameYear * 1000L + state.gameWeek * 17L + state.storyRound));
        TraitResolutionService.ResolveWindow(state, "post_ops_pre_combat", modules);
        foreach (var member in state.members)
        {
            if (DispatchIncomeHelper.IsLegionIncome(member))
            {
                if (!DispatchStatCostService.ApplyRoundCost(state, member, rng))
                {
                    PushAlert(state, Display(member) + " 本回合采矿/赏金跳过（精力/归属感不足）");
                    continue;
                }
            }
        }
        MiningSettlementService.SettleOperationPhase(state, ships, modules);
        BountySettlementService.SettleOperationPhase(state, ships, modules);
        AutonomousIncomeService.SettleOperationPhase(state, ships, modules);
        PersonalFortressAutonomyService.TryAutonomousAnchors(state, rng);
        PersonalFortressIncomeService.SettleOperationPhase(state);
        IdentityStatService.RegenEnergyAllMembers(state);
        DockingPenaltyService.Refresh(state, ships);
        CampaignOutcomeService.Evaluate(state);
    }

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }

    private static string Display(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "团员";
}
