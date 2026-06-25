namespace TopDog.Sim.Member;

public static class ProceduralBioGenerator
{
    private static readonly string[] Openings =
    {
        "曾在边境舰队服役", "出身工业殖民星", "自学成才的战术分析员", "退役后转入私人军事承包",
        "家族世代经营矿业", "青年时期加入学员军团", "擅长多开协同作战",
    };

    private static readonly string[] Traits =
    {
        "习惯在倒计时最后几秒决策", "对矿带与赏金任务有独特嗅觉", "与军团归属感波动较大",
        "偏好战巡吨位", "常把星币投入账号建设",
    };

    public static string Generate(Random rng) =>
        Pick(Openings, rng) + "；" + Pick(Traits, rng) + "。";

    private static string Pick(string[] pool, Random rng) => pool[rng.Next(pool.Length)];
}
