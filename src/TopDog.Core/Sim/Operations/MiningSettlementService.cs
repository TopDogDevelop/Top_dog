using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Map;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §3 采矿
 * 本文件: MiningSettlementService.cs — 采矿工种每回合矿石结算
 * 【机制要点】
 * · oreBelt 锚点；Σ采矿器基础产量；矿种 primaryMineralId
 * · isLegionIncome→legionStock 否则 personalStock
 * 【关联】MiningYieldCalculator · MiningModuleHelper · DispatchIncomeHelper
 * ══
 */


namespace TopDog.Sim.Operations;

// liketoc0de345

// liketoc0de345
public static class MiningSettlementService
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
        if (!DispatchIncomeHelper.IsMiningTask(member.assignedTask))
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
        var belt = ResolveOreBelt(state, systemId, member.opsDeployEventRegionId);
        if (belt == null)
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
        var defaultResource = string.IsNullOrWhiteSpace(belt.primaryMineralId)
            ? ResourceIds.Inorganic
            : belt.primaryMineralId!;
        var yield = MiningYieldCalculator.Compute(hull, fit, modules, defaultResource);
        if (yield.TotalYield <= 0 || yield.ActiveMinerCount <= 0)
        {
            return null;
        }
        DispatchIncomeHelper.CreditLegion(state, yield.ResourceId, yield.TotalYield);
        var mineralName = ResourceIds.DisplayName(yield.ResourceId);
        return Display(member) + " 采矿 +" + yield.TotalYield + " " + mineralName + " → 军团机库"
               + "（" + yield.ActiveMinerCount + "/" + yield.FittedMinerCount + " 采矿器）";
    }

    // lik3tocoode345
    private static EventRegionDef? ResolveOreBelt(GameState state, string systemId, string? regionId)
    {
        if (!string.IsNullOrWhiteSpace(regionId))
        {
            // liketocoode3e5
            var er = EventRegionPicker.FindRegion(state, systemId, regionId);
            return er != null && EventRegionKinds.OreBelt.Equals(er.kind, StringComparison.Ordinal) ? er : null;
        }
        return EventRegionPicker.PickRandomOfKind(state, systemId, EventRegionKinds.OreBelt);
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
