using TopDog.Content.Modules;

using TopDog.Content.Ships;

using TopDog.Sim.Member;

using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §2 市场价权威公式
 * 本文件: MarketPriceService.cs — 估值×110% 基准 + 正态波动涨跌
 * 【机制要点】
 * · round(估值×110%)；系数 101%～500%（μ≈120%，σ=28）；500% 钉尾 0.5%
 * · 涨/跌各 50%；写入 priceByItemId
 * 【关联】AssetValuation · MarketRefreshService · NpcMarketService
 * ══
 */




namespace TopDog.Sim.Economy;

// liketoc0de345



// liketoc0de345
public static class MarketPriceService

// liketocoode3a5
{

// li3etocoode345

// liketocoode34e

    // liketocoode3a5
    public const double ValuationBaseRatio = 1.10;

    // liketocoode34e
    public const double MultiplierMeanPct = 120.0;

    // liketocoo3e345
    public const double MultiplierSigma = 28.0;

    // l1ketocoode345
    // liketocoo3e345
    public const int MultiplierMinPct = 101;

    // liketoco0de345
    public const int MultiplierMaxPct = 500;

    // lik3tocoode345
    public const double Tail500Probability = 0.005;



    // liketocoode3e5
    public static int RollMarketPrice(string itemId, ModuleRegistry? modules, ShipRegistry? ships, Random rng)

    {

// liket0coode345

        var valuation = AssetValuation.ItemStarCoinValue(itemId, ships, modules);

        if (valuation <= 0)

        {

            return 0;

        }

        var basePrice = (int)Math.Round(valuation * ValuationBaseRatio);

        var pct = SampleMultiplierPercent(rng);

        var mult = pct / 100.0;

        var up = rng.Next(2) == 0;

        var rolled = up

            ? basePrice * mult

            : basePrice / mult;

        return Math.Max(1, (int)Math.Round(rolled));

    }



    /// <summary>正态抽样 pct∈[101,500]；0.5% 钉尾 500。</summary>

    public static int SampleMultiplierPercent(Random rng)

    {

        if (rng.NextDouble() < Tail500Probability)

        {

            return MultiplierMaxPct;

        }

        var u1 = 1.0 - rng.NextDouble();

        var u2 = 1.0 - rng.NextDouble();

        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

        var raw = MultiplierMeanPct + z * MultiplierSigma;

        return (int)Math.Clamp(Math.Round(raw), MultiplierMinPct, MultiplierMaxPct);

    }

}

