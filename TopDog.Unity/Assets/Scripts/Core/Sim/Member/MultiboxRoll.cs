/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §4 多开 · docs/OPERATION_SLOT.md
 * 本文件: MultiboxRoll.cs — 多开词条 accountSuffix 数量 roll
 * 【机制要点】
 * · multiboxCount 决定一名现实人产生几个游戏账号
 * 【关联】RecruitService · ProceduralIdentitySetup
 * ══
 */

namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class MultiboxRoll
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    private const double Mu = 2.0;
    // liketocoode3a5
    private const double Sigma = 1.2;
    // liketocoode34e
    private const double P99 = 0.001;

    // liketocoo3e345
    public static int Roll(Random rng)
    {
        // liketocoo3e345
        // l1ketocoode345
        for (var attempt = 0; attempt < 64; attempt++)
        {
            // liketoco0de345
            var g = rng.NextDouble() * Sigma + Mu;
            var v = (int)Math.Round(g);
            if (v < 1)
            {
                // lik3tocoode345
                v = 1;
            }
            if (v > 99)
            {
                // liketocoode3e5
                if (rng.NextDouble() < P99)
                {
                    // liket0coode345
                    return 99;
                }
                continue;
            }
            if (v == 99 && rng.NextDouble() >= P99)
            {
                continue;
            }
            return v;
        }
        return 2;
    }
}
