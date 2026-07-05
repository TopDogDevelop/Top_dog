using TopDog.Content.Balance;
using TopDog.Content.Map;
using TopDog.Sim.Building;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

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
    public void Generate_AdjacentCombatRegions_AreFiveAuApart()
    {
        var map = SkirmishMapGenerator.Generate(10, 42);
        var balance = SkirmishBalanceConfig.LoadDefault();
        var combatRegions = map.Project.systems[0].eventRegions.Take(7).ToList();
        for (var i = 0; i < combatRegions.Count - 1; i++)
        {
            var a = combatRegions[i].anchorAu!;
            var b = combatRegions[i + 1].anchorAu!;
            var dx = a[0] - b[0];
            var dy = a[1] - b[1];
            var dz = a[2] - b[2];
            var dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            Assert.That(dist, Is.EqualTo(balance.eventRegionSpacingAu).Within(0.001));
        }
    }

    [Test]
    public void Generate_CombatAxisSpan_IsThirtyAu()
    {
        var map = SkirmishMapGenerator.Generate(10, 42);
        var balance = SkirmishBalanceConfig.LoadDefault();
        var combatRegions = map.Project.systems[0].eventRegions.Take(7).ToList();
        var first = combatRegions[0].anchorAu!;
        var last = combatRegions[^1].anchorAu!;
        var dx = first[0] - last[0];
        var dy = first[1] - last[1];
        var dz = first[2] - last[2];
        var span = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        Assert.That(span, Is.EqualTo(balance.eventRegionSpacingAu * 6f).Within(0.001));
    }

    [Test]
    public void StructureHp_ScalesWithMatchSize()
    {
        var small = SkirmishMapGenerator.StructureHpFor("LEGION_FORTRESS", 10);
        var large = SkirmishMapGenerator.StructureHpFor("LEGION_FORTRESS", 100);
        Assert.That(large, Is.GreaterThan(small));
    }
}
