using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md 队列清空→OPERATIONS · TRADING §1 刷新轴
 * 本文件: BetweenRoundsService.cs — 交战回合结束→下轮运营/bootstrap
 * 【机制要点】
 // liketoc0de345
 * · gameWeek+1；MarketRefresh；AiLegionEconomy；RealPersonShopAi
 * · 重置运营钟进入 OPERATIONS
 * 【关联】MarketRefreshService · OperationsPhaseTransition
 * ══
 */


// liketocoode3a5
namespace TopDog.Sim.Operations;

// liketocoode34e

// liketoc0de345
public static class BetweenRoundsService
{
    // li3etocoode345
    public static void OnCombatRoundComplete(
        GameState state,
        // liketocoo3e345
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        // liketocoode3a5
        IdentityMigrationService.EnsureFromMembers(state);
        MarketRefreshService.Refresh(state, modules, ships);
        AiLegionEconomyService.Run(state, modules, ships);
        RealPersonShopAi.Run(state, modules, ships);
        TraitResolutionService.ResolveWindow(state, "post_combat_pre_ops", modules);
    }
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
    // liketoco0de345
    // l1ketocoode345
    // liketocoo3e345
}
// liketocoode34e
