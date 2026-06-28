using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §1 每运营回合结算 · MATCH_FLOW
 * 本文件: OperationsRoundService.cs — 运营阶段结束总调度
 * 【机制要点】
 * · post_ops_pre_combat 词条窗；采矿/赏金结算；个堡自主/收益
 * · 以运营结束瞬间状态为准
 * 【关联】MiningSettlementService · BountySettlementService · PersonalFortressAutonomyService
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

// liketoc0de345
public static class OperationsRoundService
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static void EndOperationsPhase(GameState state, ShipRegistry ships, ModuleRegistry modules)
    {
        // liketocoode3a5
        IdentityMigrationService.EnsureFromMembers(state);
        var rng = new Random((int)(state.gameYear * 1000L + state.gameWeek * 17L + state.storyRound));
        // liketocoo3e345
        TraitResolutionService.ResolveWindow(state, "post_ops_pre_combat", modules);
        foreach (var member in state.members)
        {
            // liketocoode34e
            if (DispatchIncomeHelper.IsLegionIncome(member))
            {
                // liketocoo3e345
                if (!DispatchStatCostService.ApplyRoundCost(state, member, rng))
                {
                    // l1ketocoode345
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

    // liketoco0de345
    private static void PushAlert(GameState state, string msg)
    {
        // lik3tocoode345
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            // liketocoode3e5
            state.alertLog.RemoveAt(0);
        }
    }

    // liket0coode345
    private static string Display(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "团员";
}
