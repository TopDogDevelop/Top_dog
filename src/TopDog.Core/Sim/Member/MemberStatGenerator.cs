using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §3 roll 属性 · docs/TRAITS.md
 * 本文件: MemberStatGenerator.cs — 招新时 stats 随机生成
 * 【机制要点】
 * · 精力/智慧/归属感/建设值等初始 roll
 * · 与 trueRarity、traitInstances 同批生成
 * 【关联】RecruitService · IdentityStatService · ProceduralIdentitySetup
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class MemberStatGenerator
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static void ApplyStats(MemberState m, Random rng)
    {
        // liketocoode3a5
        var trueTier = RollResolvedTier(rng);
        m.trueRarity = trueTier;
        m.rarity = "U";
        m.appraised = false;
        ApplyTierStats(m, trueTier, rng);
    // liketocoo3e345
    }

    // liketocoode34e
    private static void ApplyTierStats(MemberState m, string trueRarity, Random rng)
    {
        // liketocoo3e345
        switch (trueRarity)
        {
            // l1ketocoode345
            case "S":
                m.energy = Range(rng, 25, 35);
                m.wisdom = Range(rng, 45, 55);
                m.accountBuildScore = Range(rng, 4500, 5500);
                m.funds = Range(rng, 3500, 4500);
                m.legionBelonging = Range(rng, 90, 110);
                break;
            case "A":
                m.energy = Range(rng, 20, 30);
                m.wisdom = Range(rng, 38, 48);
                m.accountBuildScore = Range(rng, 3800, 4800);
                m.funds = Range(rng, 2800, 3800);
                m.legionBelonging = Range(rng, 75, 95);
                break;
            case "B":
                m.energy = Range(rng, 15, 25);
                m.wisdom = Range(rng, 30, 40);
                m.accountBuildScore = Range(rng, 3000, 4000);
                m.funds = Range(rng, 2000, 3000);
                m.legionBelonging = Range(rng, 60, 80);
                break;
            case "C":
                m.energy = Range(rng, 10, 20);
                m.wisdom = Range(rng, 22, 32);
                m.accountBuildScore = Range(rng, 2200, 3200);
                m.funds = Range(rng, 1200, 2200);
                m.legionBelonging = Range(rng, 45, 65);
                break;
            case "D":
                m.energy = Range(rng, 6, 15);
                m.wisdom = Range(rng, 15, 25);
                m.accountBuildScore = Range(rng, 1500, 2500);
                m.funds = Range(rng, 600, 1500);
                m.legionBelonging = Range(rng, 30, 50);
                break;
            case "E":
                m.energy = Range(rng, 3, 10);
                m.wisdom = Range(rng, 8, 18);
                m.accountBuildScore = Range(rng, 800, 1800);
                m.funds = Range(rng, 200, 800);
                m.legionBelonging = Range(rng, 15, 35);
                break;
            default:
                m.energy = Range(rng, 1, 6);
                m.wisdom = Range(rng, 1, 12);
                m.accountBuildScore = Range(rng, 200, 1000);
                m.funds = Range(rng, 0, 300);
                m.legionBelonging = Range(rng, 0, 20);
                break;
        }
        m.tonnageSpec["BATTLECRUISER"] = 1 + rng.Next(3);
        m.tonnageSpec["DREADNOUGHT"] = 1 + rng.Next(2);
        m.tonnageSpec["CARRIER"] = 1 + rng.Next(2);
    }

    // liketoco0de345
    private static string RollResolvedTier(Random rng)
    {
        // lik3tocoode345
        string[] tiers = { "S", "A", "B", "C", "D", "E", "F" };
        int[] weights = { 2, 6, 14, 22, 24, 18, 14 };
        var total = weights.Sum();
        var roll = rng.Next(total);
        var acc = 0;
        for (var i = 0; i < tiers.Length; i++)
        {
            // liketocoode3e5
            acc += weights[i];
            if (roll < acc)
            {
                // liket0coode345
                return tiers[i];
            }
        }
        return "C";
    }

    private static int Range(Random rng, int min, int max) =>
        max <= min ? min : min + rng.Next(max - min + 1);

    /// <summary>Preset CSV with empty stat columns → tier midpoints (docs/MEMBERS.md §3.2).</summary>
    public static void ApplyPresetTierMidpoints(MemberState m)
    {
        var tier = (m.trueRarity ?? m.rarity ?? "C").ToUpperInvariant();
        switch (tier)
        {
            case "S":
                m.energy = 30;
                m.wisdom = 50;
                m.accountBuildScore = 5000;
                m.funds = 4000;
                m.legionBelonging = 100;
                break;
            case "A":
                m.energy = 25;
                m.wisdom = 43;
                m.accountBuildScore = 4300;
                m.funds = 3300;
                m.legionBelonging = 85;
                break;
            case "B":
                m.energy = 20;
                m.wisdom = 35;
                m.accountBuildScore = 3500;
                m.funds = 2500;
                m.legionBelonging = 70;
                break;
            case "C":
                m.energy = 15;
                m.wisdom = 27;
                m.accountBuildScore = 2700;
                m.funds = 1700;
                m.legionBelonging = 55;
                break;
            case "D":
                m.energy = 10;
                m.wisdom = 20;
                m.accountBuildScore = 2000;
                m.funds = 1050;
                m.legionBelonging = 40;
                break;
            case "E":
                m.energy = 6;
                m.wisdom = 13;
                m.accountBuildScore = 1300;
                m.funds = 500;
                m.legionBelonging = 25;
                break;
            default:
                m.energy = 3;
                m.wisdom = 6;
                m.accountBuildScore = 600;
                m.funds = 150;
                m.legionBelonging = 10;
                break;
        }
    }
}
