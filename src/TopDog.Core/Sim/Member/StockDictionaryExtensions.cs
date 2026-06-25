namespace TopDog.Sim.Member;

public static class StockDictionaryExtensions
{
    public static void AddQty(this Dictionary<string, int> stock, string itemId, int delta)
    {
        var q = stock.GetValueOrDefault(itemId, 0) + delta;
        if (q <= 0)
        {
            stock.Remove(itemId);
        }
        else
        {
            stock[itemId] = q;
        }
    }
}
