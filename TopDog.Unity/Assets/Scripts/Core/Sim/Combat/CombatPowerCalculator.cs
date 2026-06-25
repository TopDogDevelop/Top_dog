using TopDog.Content.Ships;
using TopDog.Sim.State;

namespace TopDog.Sim.Combat;

public static class CombatPowerCalculator
{
    private static readonly Dictionary<string, int> TonnageRank = new(StringComparer.Ordinal)
    {
        ["BATTLECRUISER"] = 7,
        ["DREADNOUGHT"] = 9,
        ["CARRIER"] = 10,
    };

    public static int AccountQuality(MemberState? m)
    {
        if (m == null)
        {
            return 0;
        }
        return RarityBase(m.rarity) + m.energy * 3 + m.wisdom * 3;
    }

    public static int TonnageRankOf(string? tonnageClass) =>
        tonnageClass != null && TonnageRank.TryGetValue(tonnageClass, out var r) ? r : 5;

    public static int SpecLevel(MemberState? m, string? tonnageClass)
    {
        if (m == null || tonnageClass == null)
        {
            return 1;
        }
        return m.tonnageSpec.TryGetValue(tonnageClass, out var lv) && lv > 0 ? lv : 1;
    }

    public static float MemberPower(MemberState? m, ShipRegistry? ships)
    {
        if (m?.equippedHullId == null || ships == null)
        {
            return 0f;
        }
        var hull = ships.FindHull(m.equippedHullId);
        if (hull?.tonnageClass == null)
        {
            return 0f;
        }
        return AccountQuality(m) * (float)TonnageRankOf(hull.tonnageClass) * SpecLevel(m, hull.tonnageClass);
    }

    public static float EnemyLinePower(string? hullId, int accountQuality, int specLevel, ShipRegistry? ships)
    {
        if (hullId == null || ships == null)
        {
            return 0f;
        }
        var hull = ships.FindHull(hullId);
        if (hull?.tonnageClass == null)
        {
            return 0f;
        }
        var q = Math.Max(1, accountQuality);
        var spec = Math.Max(1, specLevel);
        return q * (float)TonnageRankOf(hull.tonnageClass) * spec;
    }

    public static HullDef? MaxTonnageHull(ShipRegistry? ships)
    {
        if (ships == null)
        {
            return null;
        }
        HullDef? best = null;
        var bestRank = -1;
        foreach (var h in ships.AllHulls())
        {
            var r = TonnageRankOf(h.tonnageClass);
            if (r > bestRank)
            {
                bestRank = r;
                best = h;
            }
        }
        return best;
    }

    private static int RarityBase(string? rarity) => rarity?.ToUpperInvariant() switch
    {
        "S" => 100,
        "A" => 80,
        "B" => 60,
        "C" => 45,
        _ => 50,
    };
}
