using TopDog.Content.Map;
using TopDog.Lobby;

namespace TopDog.Tests;

public sealed class ProceduralMapGeneratorTests
{
    [Test]
    public void PlacesSystemsOnHorizontalXzDisk()
    {
        var map = ProceduralMapGenerator.Generate(new ProceduralMapOptions
        {
            SystemCount = 30,
            BridgeDensity = 1f,
            Seed = 4242,
        });

        static float Range(IReadOnlyList<SolarSystemDef> systems, int axis)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var sys in systems)
            {
                var v = sys.starMapPositionLy[axis];
                min = MathF.Min(min, v);
                max = MathF.Max(max, v);
            }
            return max - min;
        }

        var rangeX = Range(map.Project.systems, 0);
        var rangeY = Range(map.Project.systems, 1);
        var rangeZ = Range(map.Project.systems, 2);
        Assert.That(rangeX, Is.GreaterThan(rangeY * 3f));
        Assert.That(rangeZ, Is.GreaterThan(rangeY * 3f));
    }

    [Test]
    public void GeneratesConnectedMapWithExpectedSystemCount()
    {
        var map = ProceduralMapGenerator.Generate(new ProceduralMapOptions
        {
            SystemCount = 24,
            BridgeDensity = 1f,
            Seed = 4242,
        });
        Assert.That(map.Project.systems.Count, Is.EqualTo(24));
        Assert.That(map.Project.bridges.Count, Is.GreaterThanOrEqualTo(23));
        Assert.That(ProceduralMapGenerator.IsConnected(map.Project), Is.True);
    }

    [Test]
    public void HigherDensityProducesMoreBridges()
    {
        var sparse = ProceduralMapGenerator.Generate(new ProceduralMapOptions
        {
            SystemCount = 30,
            BridgeDensity = 0.25f,
            Seed = 100,
        });
        var dense = ProceduralMapGenerator.Generate(new ProceduralMapOptions
        {
            SystemCount = 30,
            BridgeDensity = 3f,
            Seed = 100,
        });
        Assert.That(dense.Project.bridges.Count, Is.GreaterThan(sparse.Project.bridges.Count));
    }

    [Test]
    public void SameSeedIsDeterministic()
    {
        var a = ProceduralMapGenerator.Generate(new ProceduralMapOptions
        {
            SystemCount = 16,
            BridgeDensity = 1f,
            Seed = 9001,
        });
        var b = ProceduralMapGenerator.Generate(new ProceduralMapOptions
        {
            SystemCount = 16,
            BridgeDensity = 1f,
            Seed = 9001,
        });
        Assert.That(a.Project.systems[0].name, Is.EqualTo(b.Project.systems[0].name));
        Assert.That(a.Project.bridges.Count, Is.EqualTo(b.Project.bridges.Count));
    }

    [Test]
    public void ResolveLobbyMapUsesProceduralSettings()
    {
        var lobby = new CustomLobbyState
        {
            proceduralMap = true,
            mapPath = MapCatalogEntry.ProceduralMapId,
            proceduralSystemCount = 12,
            proceduralBridgeDensity = 1.5f,
            proceduralSeed = 77,
        };
        var map = ContentCatalog.ResolveLobbyMap(lobby);
        Assert.That(map.Project.systems.Count, Is.EqualTo(12));
        Assert.That(lobby.proceduralSeed, Is.EqualTo(77));
    }
}
