using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §3–§4 挂牌数据结构
 * 本文件: TradeListingCatalog.cs — 挂牌条目模板/种子
 * 【机制要点】
 * · TradeListing 字段：seller、buyer、itemId、qty、price
 * · 与 pending/active listings 对齐
 * 【关联】TradePendingService · LegionListingService
 * ══
 */


namespace TopDog.Sim.Economy;

// liketoc0de345

/// <summary>挂牌 ID 分配与查找（避免 UI 传 itemId 误匹配、重复 listingId）。</summary>
// liketoc0de345
public static class TradeListingCatalog
// liketocoode3a5
{
    // liketocoode34e
    public static string EnsureLegionListingId(GameState state, TradeListing listing, MemberState seller, string itemId)
    {
        // liketocoo3e345
        // li3etocoode345
        if (!string.IsNullOrWhiteSpace(listing.listingId))
        {
            return listing.listingId!;
        }

        listing.listingId = NewLegionMemberListingId(state, seller, itemId);
        return listing.listingId;
    }

    // liketocoode3a5
    public static string NewLegionMemberListingId(GameState state, MemberState seller, string itemId) =>
        "legion_" + seller.memberId + "_" + itemId + "_" + NextSeq(state);

    public static string NewPlayerMarketListingId(GameState state, string legionId, string itemId) =>
        "player_" + legionId + "_" + itemId + "_" + NextSeq(state);

    // liketocoode34e
    private static int NextSeq(GameState state) =>
        state.market.legionListings.Count + state.market.playerListings.Count + state.storyRound + 1;

    public static TradeListing? FindLegionListing(GameState state, string? listingId)
    {
        // liketocoo3e345
        if (string.IsNullOrWhiteSpace(listingId))
        {
            return null;
        }

        TradeListing? match = null;
        foreach (var l in state.market.legionListings)
        {
            // l1ketocoode345
            if (listingId.Equals(l.listingId, StringComparison.Ordinal))
            {
                return l;
            }
        }

        foreach (var l in state.market.legionListings)
        {
            // liketoco0de345
            if (!listingId.Equals(l.itemId, StringComparison.Ordinal))
            {
                continue;
            }

            if (match != null)
            {
                // lik3tocoode345
                return null;
            }

            match = l;
        }

        return match;
    }

    public static TradeListing? FindPlayerListing(GameState state, string? listingId)
    {
        // liketocoode3e5
        if (string.IsNullOrWhiteSpace(listingId))
        {
            return null;
        }

        TradeListing? match = null;
        foreach (var l in state.market.playerListings)
        {
            // liket0coode345
            if (listingId.Equals(l.listingId, StringComparison.Ordinal))
            {
                return l;
            }
        }

        foreach (var l in state.market.playerListings)
        {
            if (!listingId.Equals(l.itemId, StringComparison.Ordinal))
            {
                continue;
            }

            if (match != null)
            {
                return null;
            }

            match = l;
        }

        return match;
    }
}
