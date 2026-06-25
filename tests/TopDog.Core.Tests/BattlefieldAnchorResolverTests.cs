using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class BattlefieldAnchorResolverTests
{
    [Test]
    public void ResolveReturnsEventRegionAnchor()
    {
        var state = new GameState
        {
            map = new Content.Map.LoadedMap(
                new Content.Map.MapProject
                {
                    systems =
                    {
                        new Content.Map.SolarSystemDef
                        {
                            solarSystemId = "sys1",
                            eventRegions =
                            {
                                new Content.Map.EventRegionDef
                                {
                                    eventRegionId = "mine_a",
                                    anchorAu = new[] { 1.2f, 0f, 0.3f },
                                },
                            },
                        },
                    },
                },
                null),
        };
        var anchor = BattlefieldAnchorResolver.Resolve(state, "sys1", "mine_a");
        Assert.That(anchor[0], Is.EqualTo(1.2f).Within(0.001f));
        Assert.That(anchor[2], Is.EqualTo(0.3f).Within(0.001f));
    }
}
