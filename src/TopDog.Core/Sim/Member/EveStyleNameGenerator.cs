namespace TopDog.Sim.Member;

public static class EveStyleNameGenerator
{
    private static readonly string[] First =
    {
        "Hakodan", "Suolen", "Arkan", "Breia", "Chande", "Duon", "Eifyr", "Friban",
        "Galm", "Helen", "Iiris", "Jandice", "Kador", "Lai", "Merol", "Nakam",
        "Olo", "Perrin", "Quafe", "Roden", "Sais", "Tash", "Uri", "Vilam",
        "Warr", "Yani", "Zainou", "Cal", "Dar", "Hel", "Jas", "Kal", "Mer", "Nav", "Sol", "Tor", "Vel", "Zek",
    };

    private static readonly string[] Last =
    {
        "Ishukori", "Malait", "Tendren", "Vilamoen", "Erkinen", "Saissore", "Duvolle",
        "Kaunokka", "Seitu", "Tash-Murkon", "Korako", "Sarpati", "Mordok", "Vherok",
        "ion", "ius", "eth", "dan", "tor", "vek", "mon", "kin", "ara", "oth",
    };

    private static readonly string[] Mid =
    {
        "aen", "ain", "ara", "eis", "ian", "ius", "ora", "uen", "eth", "oth", "el", "or", "an", "en",
    };

    public static string RollAccountName(Random rng)
    {
        var style = rng.Next(100);
        if (style < 40)
        {
            return Pick(First, rng) + " " + Pick(Last, rng);
        }
        if (style < 70)
        {
            var bas = Pick(First, rng);
            var take = Math.Min(4, bas.Length);
            return char.ToUpper(bas[0]) + bas[1..take] + Pick(Mid, rng) + Pick(Last, rng);
        }
        return Pick(First, rng) + "-" + (1000 + rng.Next(9000));
    }

    private static string Pick(string[] pool, Random rng) => pool[rng.Next(pool.Length)];
}
