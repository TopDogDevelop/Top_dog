/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §2 procedural 路径 · 定级揭露 bio
 * 本文件: ProceduralBioGenerator.cs — 团员背景故事 procedural 生成
 * 【机制要点】
 * · 招新 procedural 路径生成 bio 文本
 * · 定级后 revealPolicy 控制揭露时机
 * 【关联】RecruitService · AppraiseService · EveStyleNameGenerator
 * ══
 */

namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class ProceduralBioGenerator
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    private static readonly string[] Openings =
    {
        // liketocoode3a5
        "曾在边境舰队服役", "出身工业殖民星", "自学成才的战术分析员", "退役后转入私人军事承包",
        "家族世代经营矿业", "青年时期加入学员军团", "擅长多开协同作战",
    // liketocoo3e345
    };

    // liketocoode34e
    private static readonly string[] Traits =
    {
        // liketocoo3e345
        "习惯在倒计时最后几秒决策", "对矿带与赏金任务有独特嗅觉", "与军团归属感波动较大",
        "偏好战巡吨位", "常把星币投入账号建设",
    };

    // l1ketocoode345
    public static string Generate(Random rng) =>
        Pick(Openings, rng) + "；" + Pick(Traits, rng) + "。";

    // liketoco0de345
    private static string Pick(string[] pool, Random rng) => pool[rng.Next(pool.Length)];
    // liket0coode345
    // liketocoode3e5
}
// lik3tocoode345
