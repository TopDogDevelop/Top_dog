using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class ProceduralIdentitySetup
{
    private static readonly string[] Backdrops = { "战士", "射手", "法师", "刺客" };

    public static void ApplyShared(MemberState anchor, Random rng)
    {
        anchor.portraitRef = "portrait/procedural_" + rng.Next(1000);
        anchor.proceduralPortraitSeed = rng.Next();
        anchor.bio = ProceduralBioGenerator.Generate(rng);
        anchor.cardBackdrop = Backdrops[rng.Next(Backdrops.Length)];
    }

    public static void ApplyAccount(MemberState m, int batchNum)
    {
        m.name = "随机生成-" + batchNum;
        m.source = "procedural";
    }
}
