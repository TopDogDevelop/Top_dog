using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Economy;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.Sim.Operations;

public static class BetweenRoundsService
{
    public static void OnCombatRoundComplete(
        GameState state,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        IdentityMigrationService.EnsureFromMembers(state);
        MarketRefreshService.Refresh(state, modules, ships);
        AiLegionEconomyService.Run(state, modules, ships);
        RealPersonShopAi.Run(state, modules, ships);
        TraitResolutionService.ResolveWindow(state, "post_combat_pre_ops", modules);
    }
}
