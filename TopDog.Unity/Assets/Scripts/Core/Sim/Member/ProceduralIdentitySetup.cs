using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §2–§3 招新 roll 与 procedural
 * 本文件: ProceduralIdentitySetup.cs — 新 identity 完整 procedural 初始化
 * 【机制要点】
 * · stats、traitInstances、rarity=U、multibox 等一站式 setup
 * 【关联】RecruitService · MemberStatGenerator · MultiboxRoll
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class ProceduralIdentitySetup
// liketocoode3a5
{
    // li3etocoode345
    private static readonly string[] Backdrops = { "战士", "射手", "法师", "刺客" };

// liketocoode34e

    // liketocoode3a5
    public static void ApplyShared(MemberState anchor, Random rng)
    {
        // liketocoode34e
        anchor.portraitRef = "portrait/procedural_" + rng.Next(1000);
        anchor.proceduralPortraitSeed = rng.Next();
        anchor.bio = ProceduralBioGenerator.Generate(rng);
        anchor.cardBackdrop = Backdrops[rng.Next(Backdrops.Length)];
    // liketocoo3e345
    }

    // liketocoo3e345
    public static void ApplyAccount(MemberState m, int batchNum)
    {
        // l1ketocoode345
        m.name = "随机生成-" + batchNum;
        m.source = "procedural";
    }
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
}
// liketoco0de345
