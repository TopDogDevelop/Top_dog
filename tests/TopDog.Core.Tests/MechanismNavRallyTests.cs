using TopDog.App;
using TopDog.Content.Map;
using TopDog.Sim.MechanismTest;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MechanismNavRallyTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void NavMapGenerator_TenConstellations_IsConnected()
    {
        var map = MechanismNavMapGenerator.Generate(1009);
        Assert.That(map.Project.systems.Count, Is.GreaterThanOrEqualTo(40));
        Assert.That(ProceduralMapGenerator.IsConnected(map.Project), Is.True);
        var constellationIds = map.Project.constellations
            .Select(c => c.constellationId)
            .Distinct()
            .Count();
        Assert.That(constellationIds, Is.GreaterThanOrEqualTo(10));
    }

    [Test]
    public void NavRallySpawn_OnePineapplePerScene()
    {
        var core = CampaignBootstrap.CreateFromMechanismTest("mt_nav_rally");
        var state = core.State;
        Assert.That(state.battlefields.Count, Is.GreaterThan(1));
        foreach (var bf in state.battlefields)
        {
            var pineapples = bf.units.Count(u =>
                u.side == UnitSide.FRIENDLY
                && "hull_frigate_pineapple".Equals(u.hullId, StringComparison.Ordinal));
            Assert.That(pineapples, Is.LessThanOrEqualTo(1));
        }
    }
}
