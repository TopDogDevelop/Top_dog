using TopDog.Content.Map;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MECHANISM_TEST_SCENARIOS.md §地图
 * 本文件: MechanismMapGenerator.cs — 单恒星+单矿带详测星系
 * ══
 */

namespace TopDog.Sim.MechanismTest;

public static class MechanismMapGenerator
{
    public const string SystemId = "mt_sys";
    public const string StarRegionId = "mt_star";
    public const string BeltRegionId = "mt_belt";
    public const string BeltRegionIdB = "mt_belt_b";

    public static LoadedMap Generate(int seed) => GenerateInternal(seed, dualBelt: false);

    public static LoadedMap GenerateDualBelt(int seed) => GenerateInternal(seed, dualBelt: true);

    private static LoadedMap GenerateInternal(int seed, bool dualBelt)
    {
        _ = seed;
        var regions = new List<EventRegionDef>
        {
            new()
            {
                eventRegionId = StarRegionId,
                kind = EventRegionKinds.Star,
                name = "恒星",
                radiusKm = 2000,
                anchorAu = new[] { 0f, 0f, 0f },
            },
            new()
            {
                eventRegionId = BeltRegionId,
                kind = EventRegionKinds.OreBelt,
                name = "矿带甲",
                radiusKm = 1200,
                anchorAu = new[] { 2f, 0f, 0f },
            },
        };
        if (dualBelt)
        {
            regions.Add(new EventRegionDef
            {
                eventRegionId = BeltRegionIdB,
                kind = EventRegionKinds.OreBelt,
                name = "矿带乙",
                radiusKm = 1200,
                anchorAu = new[] { -2f, 0f, 0f },
            });
        }

        var project = new MapProject
        {
            projectName = "机制详测",
            systems =
            {
                new SolarSystemDef
                {
                    solarSystemId = SystemId,
                    name = "详测星系",
                    eventRegions = regions,
                },
            },
        };

        return new LoadedMap(project, null);
    }
}
