using TopDog.Content.Modules;
using TopDog.Sim.State;

namespace TopDog.Sim.Persist;

/// <summary>将持久化库存与配装中的旧模块 ID 收敛到显式 alias 目标。</summary>
public static class ModuleIdSaveMigration
{
    public static void Apply(GameState state, ModuleRegistry modules)
    {
        CanonicalizeQuantities(state.legionStock, modules);
        foreach (var stock in state.personalStockByGroup.Values)
        {
            CanonicalizeQuantities(stock, modules);
        }
        foreach (var legion in state.legions)
        {
            CanonicalizeQuantities(legion.legionStock, modules);
        }
        foreach (var player in state.legionPlayers.Values)
        {
            CanonicalizeQuantities(player.legionStock, modules);
        }
        foreach (var fit in state.memberFittedModules.Values)
        {
            CanonicalizeFit(fit, modules);
        }
        foreach (var ship in state.shipInstances)
        {
            CanonicalizeFit(ship.fittedModules, modules);
        }
        foreach (var battlefield in state.battlefields)
        {
            foreach (var unit in battlefield.units)
            {
                CanonicalizeFit(unit.fittedModules, modules);
            }
        }
        foreach (var baseline in state.matchMemberBaselines.Values)
        {
            CanonicalizeNullableFit(baseline.fittedModules, modules);
        }
        foreach (var entry in state.combatQueue)
        {
            foreach (var line in entry.enemyRoster)
            {
                CanonicalizeFit(line.fittedModules, modules);
            }
        }
    }

    private static void CanonicalizeQuantities(
        Dictionary<string, int> stock,
        ModuleRegistry modules)
    {
        foreach (var pair in stock.ToArray())
        {
            var canonical = modules.ResolveAlias(pair.Key);
            if (canonical.Equals(pair.Key, StringComparison.Ordinal))
            {
                continue;
            }
            stock.Remove(pair.Key);
            stock[canonical] = stock.GetValueOrDefault(canonical) + pair.Value;
        }
    }

    private static void CanonicalizeFit(
        Dictionary<string, string> fit,
        ModuleRegistry modules)
    {
        foreach (var slot in fit.Keys.ToArray())
        {
            fit[slot] = modules.ResolveAlias(fit[slot]);
        }
    }

    private static void CanonicalizeNullableFit(
        Dictionary<string, string?> fit,
        ModuleRegistry modules)
    {
        foreach (var slot in fit.Keys.ToArray())
        {
            if (fit[slot] is { } moduleId)
            {
                fit[slot] = modules.ResolveAlias(moduleId);
            }
        }
    }
}
