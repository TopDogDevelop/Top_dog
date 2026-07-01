namespace TopDog.Sim.Banter;

/// <summary>绵羊伸腿（identity 10001001）专属酱鸭语录；选中其任一号即覆盖正文。</summary>
public static class BanterSheepDuckPhrases
{
    public const string IdentityCode = "10001001";

    public static readonly string[] Phrases =
    {
        "酱鸭！",
        "一群酱鸭！",
        "你这酱鸭！",
        "我不想吃鸭！",
    };

    public static bool IsSheepIdentity(string? identityCode) =>
        !string.IsNullOrWhiteSpace(identityCode)
        && string.Equals(identityCode, IdentityCode, StringComparison.Ordinal);

    /// <summary>伪随机：四句洗牌袋，用尽后重洗。</summary>
    public static string DrawNext(List<int> bag, Random rng)
    {
        if (bag.Count == 0)
        {
            Refill(bag, rng);
        }

        var idx = bag[^1];
        bag.RemoveAt(bag.Count - 1);
        return Phrases[idx];
    }

    public static void Refill(List<int> bag, Random rng)
    {
        bag.Clear();
        for (var i = 0; i < Phrases.Length; i++)
        {
            bag.Add(i);
        }

        for (var i = bag.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }
    }
}
