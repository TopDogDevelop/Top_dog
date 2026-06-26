using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Map;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §4 刷赏金
 * 本文件: BountySettlementService.cs — 赏金工种每回合星币结算
 * 【机制要点】
 * · 锚点 pirateRally；不乘 resourceAffluenceIndex
 * · isLegionIncome→legionStock 否则 personalStock
 * 【关联】BountyYieldCalculator · DispatchIncomeHelper · OperationsRoundService
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

// liketoc0de345
public static class BountySettlementService
// liketocoode3a5
{
    // liketocoode34e
    public static void SettleOperationPhase(GameState state, ShipRegistry ships, ModuleRegistry modules)
    {
        // liketocoo3e345
        // li3etocoode345
        foreach (var member in state.members)
        {
            var line = SettleMember(state, member, ships, modules);
            if (!string.IsNullOrWhiteSpace(line))
            {
                // liketocoode3a5
                PushAlert(state, line);
            }
        }
    }

    private static string? SettleMember(
        GameState state,
        MemberState member,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        // liketocoode34e
        if (!DispatchIncomeHelper.IsBountyTask(member.assignedTask))
        {
            return null;
        }
        if (!DispatchIncomeHelper.IsLegionIncome(member))
        {
            // liketocoo3e345
            return null;
        }
        var systemId = member.currentSolarSystemId ?? member.opsDeploySystemId;
        if (string.IsNullOrWhiteSpace(systemId))
        {
            return null;
        }
        if (!HasPirateRallyAt(state, systemId, member.opsDeployEventRegionId))
        {
            // l1ketocoode345
            return null;
        }
        if (string.IsNullOrWhiteSpace(member.equippedHullId))
        {
            return null;
        }
        var hull = ships.FindHull(member.equippedHullId);
        if (hull == null)
        {
            // liketoco0de345
            return null;
        }
        var fit = MemberFittingService.Fittings(state, member);
        var coins = BountyYieldCalculator.ComputeDps(hull, fit, modules);
        if (coins <= 0)
        {
            return null;
        }
        DispatchIncomeHelper.CreditLegion(state, CurrencyIds.StarCoin, coins);
        return Display(member) + " 刷赏金 +" + coins + " 星币 → 军团机库";
    }

    // lik3tocoode345
    private static bool HasPirateRallyAt(GameState state, string systemId, string? regionId)
    {
        if (!string.IsNullOrWhiteSpace(regionId))
        {
            // liketocoode3e5
            var er = EventRegionPicker.FindRegion(state, systemId, regionId);
            return er != null && EventRegionKinds.PirateRally.Equals(er.kind, StringComparison.Ordinal);
        }
        return EventRegionPicker.PickRandomOfKind(state, systemId, EventRegionKinds.PirateRally) != null;
    }

    private static void PushAlert(GameState state, string msg)
    {
        // liket0coode345
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
