using TopDog.Sim.Skirmish;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class SkirmishMapGeneratorTests
{
    [Test]
    public void Generate_SingleSystemWithSevenCombatRegionsPlusPlanets()
    {
        var map = SkirmishMapGenerator.Generate(10, 42);
        Assert.That(map.Project.systems, Has.Count.EqualTo(1));
        var sys = map.Project.systems[0];
        Assert.That(sys.eventRegions.Count, Is.EqualTo(9));
    }

    [Test]
    public void StructureHp_ScalesWithMatchSize()
    {
        var small = SkirmishMapGenerator.StructureHpFor("LEGION_FORTRESS", 10);
        var large = SkirmishMapGenerator.StructureHpFor("LEGION_FORTRESS", 100);
        Assert.That(large, Is.GreaterThan(small));
    }
}
