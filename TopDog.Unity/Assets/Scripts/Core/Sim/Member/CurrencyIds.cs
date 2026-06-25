namespace TopDog.Sim.Member;

public static class CurrencyIds
{
    public const string StarCoin = "item_star_coin";

    public static bool IsCurrency(string? itemId) => StarCoin.Equals(itemId, StringComparison.Ordinal);

    public static string DisplayName(string itemId) =>
        StarCoin.Equals(itemId, StringComparison.Ordinal) ? "星币" : itemId;
}
