using TopDog.Content;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>伴聊随机名池：优先全表均匀抽样，军团库存加权（避免仅库存一件时永远同名）。</summary>
internal static class BanterFlavorPools
{
    public static string RollModuleName(GameState state, ModuleRegistry modules, Random rng)
    {
        var pool = BuildModulePool(state, modules);
        if (pool.Count == 0)
        {
            return "装备";
        }

        return ModuleRegistry.Bilingual(modules.Find(pool[rng.Next(pool.Count)]));
    }

    public static string RollHullName(GameState state, ShipRegistry ships, Random rng)
    {
        var pool = BuildHullPool(state, ships);
        if (pool.Count == 0)
        {
            return "舰船";
        }

        return DisplayLabels.HullBilingual(ships.FindHull(pool[rng.Next(pool.Count)]));
    }

    private static List<string> BuildModulePool(GameState state, ModuleRegistry modules)
    {
        var pool = new List<string>();
        foreach (var kv in modules.All())
        {
            if (kv.Key.StartsWith("mod_", StringComparison.Ordinal))
            {
                pool.Add(kv.Key);
            }
        }

        if (pool.Count == 0)
        {
            return pool;
        }

        WeightStockEntries(state, pool, "mod_", maxCopies: 2);
        return pool;
    }

    private static List<string> BuildHullPool(GameState state, ShipRegistry ships)
    {
        var pool = new List<string>();
        foreach (var hull in ships.AllHulls())
        {
            if (!string.IsNullOrWhiteSpace(hull.hullId))
            {
                pool.Add(hull.hullId);
            }
        }

        if (pool.Count == 0)
        {
            return pool;
        }

        WeightStockEntries(state, pool, "hull_", maxCopies: 2);
        return pool;
    }

    private static void WeightStockEntries(
        GameState state,
        List<string> pool,
        string idPrefix,
        int maxCopies)
    {
        var stock = LegionRegistry.MutableLocalStock(state);
        foreach (var kv in stock)
        {
            if (kv.Value <= 0 || !kv.Key.StartsWith(idPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (!pool.Contains(kv.Key, StringComparer.Ordinal))
            {
                pool.Add(kv.Key);
            }

            var copies = Math.Min(maxCopies, Math.Max(1, kv.Value));
            for (var i = 0; i < copies; i++)
            {
                pool.Add(kv.Key);
            }
        }
    }
}
