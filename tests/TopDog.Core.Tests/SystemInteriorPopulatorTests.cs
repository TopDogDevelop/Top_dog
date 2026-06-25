using TopDog.Content.Map;
using TopDog.Foundation.Io;
using TopDog.Lobby;

namespace TopDog.Tests;

public sealed class SystemInteriorPopulatorTests
{
    [Test]
    public void EnsuresPlanetGatesAndRandomSites()
    {
        var project = new MapProject
        {
            projectName = "test",
            version = "1",
        };
        project.systems.Add(new SolarSystemDef
        {
            solarSystemId = "sys_a",
            name = "A",
            constellationId = "con",
            regionId = "reg",
            resourceAffluenceIndex = 50,
            developmentDifficulty = 50,
            securityLevel = 0.5f,
            eventRegions =
            {
                new EventRegionDef
                {
                    eventRegionId = "er_star",
                    kind = EventRegionKinds.Star,
                    name = "Star",
                    radiusKm = 1_000_000,
                    anchorAu = new[] { 0f, 0f, 0f },
                },
            },
        });
        project.bridges.Add(new JumpBridgeDef
        {
            bridgeId = "jb_ab",
            fromSystemId = "sys_a",
            toSystemId = "sys_b",
            garrisonTemplateId = "npc",
        });
        project.systems[0].jumpBridgeIds.Add("jb_ab");

        SystemInteriorPopulator.EnsureSystem(project.systems[0], project, 42);

        var sys = project.systems[0];
        Assert.That(CountKind(sys, EventRegionKinds.Planet), Is.EqualTo(1));
        Assert.That(CountKind(sys, EventRegionKinds.JumpBridge), Is.EqualTo(1));
        Assert.That(CountKind(sys, EventRegionKinds.PirateRally), Is.EqualTo(5));
        Assert.That(CountKind(sys, EventRegionKinds.OreBelt), Is.EqualTo(5));

        var planet = FindFirst(sys, EventRegionKinds.Planet)!;
        foreach (var er in sys.eventRegions)
        {
            if (EventRegionKinds.PirateRally.Equals(er.kind, StringComparison.Ordinal)
                || EventRegionKinds.OreBelt.Equals(er.kind, StringComparison.Ordinal)
                || EventRegionKinds.LegionStructure.Equals(er.kind, StringComparison.Ordinal))
            {
                Assert.That(
                    SystemInteriorPopulator.IsWithinPlanetShell(er.anchorAu, planet.anchorAu),
                    Is.True,
                    er.eventRegionId);
            }
        }
    }

    [Test]
    public void Bug6MapGetsInteriorSitesOnLoad()
    {
        var mapDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "TopDog.Unity", "Assets", "StreamingAssets", "maps", "eve_bug6-x.topdog-map"));
        if (!Directory.Exists(Path.Combine(mapDir, "systems")))
        {
            Assert.Ignore("BUG6 map not present");
        }
        AppRoot.SetOverrideRoot(Path.GetDirectoryName(Path.GetDirectoryName(mapDir))!);
        var map = ContentCatalog.LoadMap(mapDir);
        var sys = map.Project.systems[0];
        Assert.That(CountKind(sys, EventRegionKinds.Planet), Is.GreaterThanOrEqualTo(1));
        Assert.That(CountKind(sys, EventRegionKinds.PirateRally), Is.EqualTo(5));
        Assert.That(CountKind(sys, EventRegionKinds.OreBelt), Is.EqualTo(5));
    }

    private static int CountKind(SolarSystemDef sys, string kind)
    {
        var n = 0;
        foreach (var er in sys.eventRegions)
        {
            if (kind.Equals(er.kind, StringComparison.Ordinal))
            {
                n++;
            }
        }
        return n;
    }

    private static EventRegionDef? FindFirst(SolarSystemDef sys, string kind)
    {
        foreach (var er in sys.eventRegions)
        {
            if (kind.Equals(er.kind, StringComparison.Ordinal))
            {
                return er;
            }
        }
        return null;
    }
}
