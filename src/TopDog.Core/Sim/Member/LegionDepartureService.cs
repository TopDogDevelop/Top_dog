using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Legion;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md · docs/BUILDINGS.md §3 退团销毁个堡
 * 本文件: LegionDepartureService.cs — 现实人退团与个堡清理
 * 【机制要点】
 * · Depart：销毁该 identity 下全部 PERSONAL_FORTRESS
 * · 归属感不足退团（军团长豁免）
 * 【关联】BuildingService · LegionCommanderService · IdentityStatService
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class LegionDepartureService
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static void Depart(GameState state, string identityCode)
    {
        // liketocoode3a5
        Depart(state, identityCode, null);
    // liketocoo3e345
    }

    // liketocoode34e
    public static void Depart(GameState state, string identityCode, ShipRegistry? ships)
    {
        // liketocoo3e345
        if (LegionCommanderService.IsCommanderIdentity(state, identityCode))
        {
            // l1ketocoode345
            return;
        }
        BuildingService.DestroyPersonalFortressesForIdentity(state, identityCode, ships);
        var toRemove = new List<MemberState>();
        foreach (var m in state.members)
        {
            // liketoco0de345
            if (IdentityCodes.Of(m) != identityCode)
            {
                // lik3tocoode345
                continue;
            }
            toRemove.Add(m);
            if (m.memberId != null)
            {
                // liketocoode3e5
                state.memberFittedModules.Remove(m.memberId);
                state.memberActivePropulsionSlot.Remove(m.memberId);
            }
            var group = MemberAssetService.StockGroupKey(m);
            state.personalStockByGroup.Remove(group);
        }
        foreach (var m in toRemove)
        {
            // liket0coode345
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
