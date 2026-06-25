using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Economy;

/// <summary>挂牌 ID 分配与查找（避免 UI 传 itemId 误匹配、重复 listingId）。</summary>
public static class TradeListingCatalog
{
    public static string EnsureLegionListingId(GameState state, TradeListing listing, MemberState seller, string itemId)
    {
        if (!string.IsNullOrWhiteSpace(listing.listingId))
        {
            return listing.listingId!;
        }

        listing.listingId = NewLegionMemberListingId(state, seller, itemId);
        return listing.listingId;
    }

    public static string NewLegionMemberListingId(GameState state, MemberState seller, string itemId) =>
        "legion_" + seller.memberId + "_" + itemId + "_" + NextSeq(state);

    public static string NewPlayerMarketListingId(GameState state, string legionId, string itemId) =>
        "player_" + legionId + "_" + itemId + "_" + NextSeq(state);

    private static int NextSeq(GameState state) =>
        state.market.legionListings.Count + state.market.playerListings.Count + state.storyRound + 1;

    public static TradeListing? FindLegionListing(GameState state, string? listingId)
    {
        if (string.IsNullOrWhiteSpace(listingId))
        {
            return null;
        }

        TradeListing? match = null;
        foreach (var l in state.market.legionListings)
        {
            if (listingId.Equals(l.listingId, StringComparison.Ordinal))
            {
                return l;
            }
        }

        foreach (var l in state.market.legionListings)
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

    public static TradeListing? FindPlayerListing(GameState state, string? listingId)
    {
        if (string.IsNullOrWhiteSpace(listingId))
        {
            return null;
        }

        TradeListing? match = null;
        foreach (var l in state.market.playerListings)
        {
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
