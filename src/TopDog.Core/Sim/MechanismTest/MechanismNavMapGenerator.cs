using TopDog.Content.Map;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_SCENARIOS.md · mt_nav_rally
 * 本文件: MechanismNavMapGenerator.cs — 10 星座程序化详测星图
 * ══
 */

namespace TopDog.Sim.MechanismTest;

public static class MechanismNavMapGenerator
{
    public const int ConstellationCount = 10;
    public const int SystemCount = 80;

    public static LoadedMap Generate(int seed)
    {
        var options = new ProceduralMapOptions
        {
            Seed = seed == 0 ? 1 : seed,
            SystemCount = SystemCount,
            BridgeDensity = 1.25f,
        };
        return ProceduralMapGenerator.Generate(options);
    }
}
