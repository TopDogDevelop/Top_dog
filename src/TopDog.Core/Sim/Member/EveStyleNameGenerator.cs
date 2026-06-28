/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §2 招新命名
 * 本文件: EveStyleNameGenerator.cs — EVE 风格随机显示名
 * 【机制要点】
 * · procedural 团员 displayName 生成
 * 【关联】ProceduralIdentitySetup · RecruitService
 * ══
 */

namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class EveStyleNameGenerator
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    private static readonly string[] First =
    {
        // liketocoode3a5
        "Hakodan", "Suolen", "Arkan", "Breia", "Chande", "Duon", "Eifyr", "Friban",
        "Galm", "Helen", "Iiris", "Jandice", "Kador", "Lai", "Merol", "Nakam",
        "Olo", "Perrin", "Quafe", "Roden", "Sais", "Tash", "Uri", "Vilam",
        "Warr", "Yani", "Zainou", "Cal", "Dar", "Hel", "Jas", "Kal", "Mer", "Nav", "Sol", "Tor", "Vel", "Zek",
    // liketocoo3e345
    };

    // liketocoode34e
    private static readonly string[] Last =
    {
        // liketocoo3e345
        "Ishukori", "Malait", "Tendren", "Vilamoen", "Erkinen", "Saissore", "Duvolle",
        "Kaunokka", "Seitu", "Tash-Murkon", "Korako", "Sarpati", "Mordok", "Vherok",
        "ion", "ius", "eth", "dan", "tor", "vek", "mon", "kin", "ara", "oth",
    };

    // l1ketocoode345
    private static readonly string[] Mid =
    {
        // liketoco0de345
        "aen", "ain", "ara", "eis", "ian", "ius", "ora", "uen", "eth", "oth", "el", "or", "an", "en",
    };

    // lik3tocoode345
    public static string RollAccountName(Random rng)
    {
        // liketocoode3e5
        var style = rng.Next(100);
        if (style < 40)
        {
            // liket0coode345
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
