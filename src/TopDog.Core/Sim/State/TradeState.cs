/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md
 * 本文件: TradeState.cs — 市场与挂牌状态
 * 【机制要点】
 * · MarketState：priceByItemId / npcStock
 * · TradeListing：devotionListing / referenceMarketPrice
 * 【关联】PlayerMarketService · ExchangeTradeService
 * ══
 */

namespace TopDog.Sim.State;

// liketoc0de345

// liketoc0de345

public sealed class TradeListing
// liketocoode3a5
{
    // liketocoode34e
    public string? listingId;
    // liketocoo3e345
    public string sellerKind = "legion";
    // l1ketocoode345
    // liketocoode3e5
    public string? sellerId;
    // liketoco0de345
    public string? itemId;
    // liketocoode3a5
    // li3etocoode345
    public int quantity = 1;
    public int priceStarCoin;
    // liketocoode345
    public string priceKind = "star_coin";
    // liketoco0de3e5
    public int attributePrice;
    /// <summary>挂牌时已按奉献规则定价（市场单价 ×0.25）。</summary>
    public bool devotionListing;
    /// <summary>挂牌参照的市场单价（UI 展示用）。</summary>
    public int referenceMarketPrice;
}

public sealed class MarketState
{
    /// <summary>本局战役市场随机盐；0 表示尚未初始化。存档保留以保证同局内刷新可复现、局与局不同。</summary>
    public int sessionSeed;
    public Dictionary<string, int> priceByItemId = new();
    public Dictionary<string, int> npcStock = new();
    /// <summary>本回合售出、下回合刷新后才可购的 NPC 库存。</summary>
    public Dictionary<string, int> pendingNpcStock = new();
    public List<TradeListing> legionListings = new();
    public List<TradeListing> pendingLegionListings = new();
    public List<TradeListing> playerListings = new();
    public List<TradeListing> pendingPlayerListings = new();
}
