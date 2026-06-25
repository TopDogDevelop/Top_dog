using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class LegionDepartureService
{
    public static void Depart(GameState state, string identityCode)
    {
        Depart(state, identityCode, null);
    }

    public static void Depart(GameState state, string identityCode, ShipRegistry? ships)
    {
        if (LegionCommanderService.IsCommanderIdentity(state, identityCode))
        {
            return;
        }
        BuildingService.DestroyPersonalFortressesForIdentity(state, identityCode, ships);
        var toRemove = new List<MemberState>();
        foreach (var m in state.members)
        {
            if (IdentityCodes.Of(m) != identityCode)
            {
                continue;
            }
            toRemove.Add(m);
            if (m.memberId != null)
            {
                state.memberFittedModules.Remove(m.memberId);
                state.memberActivePropulsionSlot.Remove(m.memberId);
            }
            var group = MemberAssetService.StockGroupKey(m);
            state.personalStockByGroup.Remove(group);
        }
        foreach (var m in toRemove)
        {
            LegionPlayerRegistry.RemoveMember(state, m);
        }
        state.identities.Remove(identityCode);
        PushAlert(state, "现实人 " + identityCode + " 归属感耗尽，已退团（资产清空）");
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
}
