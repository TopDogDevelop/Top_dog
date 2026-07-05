using TopDog.Content.Balance;
using TopDog.Content.Map;
using TopDog.Sim.Building;
using TopDog.Sim.State;

namespace TopDog.Sim.Skirmish;

public static class SkirmishMapGenerator
{
    public const string SystemId = "skirmish_sys";
    public const string StarRegionId = "skirmish_er_star";

    private static readonly float[] AxisHops = { -3f, -2f, -1f, 0f, 1f, 2f, 3f };
    private static readonly string[] RegionNames =
    {
        "A 个堡 2", "A 军堡", "A 个堡 1", "恒星", "B 个堡 2", "B 军堡", "B 个堡 1",
    };
    private static readonly string[] RegionKinds =
    {
        EventRegionKinds.LegionStructure,
        EventRegionKinds.LegionStructure,
        EventRegionKinds.LegionStructure,
        EventRegionKinds.Star,
        EventRegionKinds.LegionStructure,
        EventRegionKinds.LegionStructure,
        EventRegionKinds.LegionStructure,
    };

    public static LoadedMap Generate(int scale, int seed)
    {
        var rng = new Random(seed);
        var axis = RandomUnitVector(rng);
        var perp = Perpendicular(axis, rng);
        var balance = SkirmishBalanceConfig.LoadDefault();
        var scaleCfg = balance.ResolveScale(scale);

        var regions = new List<EventRegionDef>();
        for (var i = 0; i < AxisHops.Length; i++)
        {
            var anchor = ScaleAu(axis, AxisHops[i] * balance.eventRegionSpacingAu);
            regions.Add(new EventRegionDef
            {
                eventRegionId = $"skirmish_er_{i + 1}",
                kind = RegionKinds[i],
                name = RegionNames[i],
                radiusKm = i == 3 ? 2000 : 800,
                anchorAu = anchor,
            });
        }

        var planetAngle = (float)(rng.NextDouble() * Math.PI * 2);
        var planetOffset = RotateAroundAxis(perp, axis, planetAngle);
        var p1 = ScaleAu(planetOffset, balance.planetDistanceAu);
        var p2 = ScaleAu(planetOffset, -balance.planetDistanceAu);
        regions.Add(new EventRegionDef
        {
            eventRegionId = "skirmish_planet_1",
            kind = EventRegionKinds.Planet,
            name = "行星 1",
            radiusKm = 1200,
            anchorAu = p1,
        });
        regions.Add(new EventRegionDef
        {
            eventRegionId = "skirmish_planet_2",
            kind = EventRegionKinds.Planet,
            name = "行星 2",
            radiusKm = 1200,
            anchorAu = p2,
        });

        var project = new MapProject
        {
            projectName = "Legion Skirmish",
            systems =
            {
                new SolarSystemDef
                {
                    solarSystemId = SystemId,
                    name = "约战星系",
                    eventRegions = regions,
                },
            },
        };

        return new LoadedMap(project, null);
    }

    public static void SeedBuildings(GameState state, IReadOnlyList<string> legionIds, int scale)
    {
        if (legionIds.Count < 2)
        {
            return;
        }

        var balance = SkirmishBalanceConfig.LoadDefault();
        var scaleCfg = balance.ResolveScale(scale);
        var legionHp = balance.legionFortressBaseStructureHp * scaleCfg.legionFortressHpMultiplier;
        var personalHp = scaleCfg.personalFortressStructureHp;
        var a = legionIds[0];
        var b = legionIds[1];

        AddFort(state, $"bld_{a}_pf2", BuildingService.PersonalFortress, a, "skirmish_er_1", false);
        AddFort(state, $"bld_{a}_legion", BuildingService.LegionFortress, a, "skirmish_er_2", false);
        AddFort(state, $"bld_{a}_pf1", BuildingService.PersonalFortress, a, "skirmish_er_3", false);
        AddFort(state, $"bld_{b}_pf2", BuildingService.PersonalFortress, b, "skirmish_er_5", false);
        AddFort(state, $"bld_{b}_legion", BuildingService.LegionFortress, b, "skirmish_er_6", false);
        AddFort(state, $"bld_{b}_pf1", BuildingService.PersonalFortress, b, "skirmish_er_7", false);
    }

    private static void AddFort(
        GameState state,
        string buildingId,
        string buildingType,
        string legionId,
        string eventRegionId,
        bool playerOwned)
    {
        state.buildings.Add(new BuildingState
        {
            buildingId = buildingId,
            buildingType = buildingType,
            solarSystemId = SystemId,
            eventRegionId = eventRegionId,
            legionId = legionId,
            playerOwned = playerOwned,
            displayName = buildingType == BuildingService.LegionFortress ? "军堡" : "个堡",
            status = BuildingService.Normal,
        });
    }

    public static float StructureHpFor(string? buildingType, int scale)
    {
        var balance = SkirmishBalanceConfig.LoadDefault();
        var scaleCfg = balance.ResolveScale(scale);
        return string.Equals(buildingType, BuildingService.LegionFortress, StringComparison.Ordinal)
            ? balance.legionFortressBaseStructureHp * scaleCfg.legionFortressHpMultiplier
            : scaleCfg.personalFortressStructureHp;
    }

    private static float[] ScaleAu(float[] v, float scale)
    {
        return new[] { v[0] * scale, v[1] * scale, v[2] * scale };
    }

    private static float[] RandomUnitVector(Random rng)
    {
        var z = (float)(rng.NextDouble() * 2 - 1);
        var t = (float)(rng.NextDouble() * Math.PI * 2);
        var r = MathF.Sqrt(Math.Max(0f, 1f - z * z));
        return new[] { r * MathF.Cos(t), r * MathF.Sin(t), z };
    }

    private static float[] Perpendicular(float[] axis, Random rng)
    {
        var refVec = MathF.Abs(axis[2]) < 0.9f ? new[] { 0f, 0f, 1f } : new[] { 0f, 1f, 0f };
        var cross = Cross(axis, refVec);
        var len = Length(cross);
        if (len < 0.001f)
        {
            return new[] { 1f, 0f, 0f };
        }

        return new[] { cross[0] / len, cross[1] / len, cross[2] / len };
    }

    private static float[] RotateAroundAxis(float[] v, float[] axis, float angleRad)
    {
        var c = MathF.Cos(angleRad);
        var s = MathF.Sin(angleRad);
        var dot = v[0] * axis[0] + v[1] * axis[1] + v[2] * axis[2];
        return new[]
        {
            v[0] * c + (axis[1] * v[2] - axis[2] * v[1]) * s + axis[0] * dot * (1 - c),
            v[1] * c + (axis[2] * v[0] - axis[0] * v[2]) * s + axis[1] * dot * (1 - c),
            v[2] * c + (axis[0] * v[1] - axis[1] * v[0]) * s + axis[2] * dot * (1 - c),
        };
    }

    private static float[] Cross(float[] a, float[] b) =>
        new[]
        {
            a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0],
        };

    private static float Length(float[] v) => MathF.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
}
