using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Economy;

public static class MarketPriceService
{
    public static int RollMarketPrice(string itemId, ModuleRegistry? modules, ShipRegistry? ships, Random rng)
    {
        var baseVal = AssetValuation.ItemStarCoinValue(itemId, ships, modules);
        if (baseVal <= 0)
        {
            return 0;
        }
        var pct = SampleMultiplierPercent(rng);
        var up = rng.Next(2) == 0;
        var mult = up ? pct / 100.0 : pct / 100.0;
        return Math.Max(1, (int)Math.Round(baseVal * mult));
    }

    private static int SampleMultiplierPercent(Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        var raw = 20.0 + z * 8.0;
        if (rng.NextDouble() < 0.009)
        {
            raw = 300.0;
        }
        return (int)Math.Clamp(Math.Round(raw), 1, 300);
    }
}
