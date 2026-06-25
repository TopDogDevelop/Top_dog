namespace TopDog.Sim.State;

public sealed class TradeListing
{
    public string? listingId;
    public string sellerKind = "legion";
    public string? sellerId;
    public string? itemId;
    public int quantity = 1;
    public int priceStarCoin;
    public string priceKind = "star_coin";
    public int attributePrice;
    /// <summary>挂牌时已按奉献规则定价（市场单价 ×0.25）。</summary>
    public bool devotionListing;
    /// <summary>挂牌参照的市场单价（UI 展示用）。</summary>
    public int referenceMarketPrice;
}

public sealed class MarketState
{
    public Dictionary<string, int> priceByItemId = new();
    public Dictionary<string, int> npcStock = new();
    public List<TradeListing> legionListings = new();
    public List<TradeListing> playerListings = new();
}
