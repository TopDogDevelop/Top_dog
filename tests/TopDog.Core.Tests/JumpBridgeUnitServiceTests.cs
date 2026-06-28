using TopDog.Content.Map;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class JumpBridgeUnitServiceTests
{
    [Test]
    public void SyncForBattlefield_OnlySpawnsCurrentRegionBridge()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            map = new LoadedMap(
                new MapProject
                {
                    systems =
                    {
                        new SolarSystemDef
                        {
                            solarSystemId = "sys-a",
                            eventRegions =
                            {
                                new EventRegionDef
                                {
                                    eventRegionId = "gate-a",
                                    kind = EventRegionKinds.JumpBridge,
                                    bridgeId = "br-a",
                                    anchorAu = new[] { 0f, 0f, 0f },
                                },
                                new EventRegionDef
                                {
                                    eventRegionId = "gate-b",
                                    kind = EventRegionKinds.JumpBridge,
                                    bridgeId = "br-b",
                                    anchorAu = new[] { 1f, 0f, 0f },
                                },
                            },
                        },
                    },
                },
                securityBands: null),
        };
        var bf = new BattlefieldState
        {
            battlefieldId = "bf1",
            systemId = "sys-a",
            eventRegionId = "gate-a",
            anchorAu = new[] { 0f, 0f, 0f },
        };

        JumpBridgeUnitService.SyncForBattlefield(state, bf);

        Assert.That(bf.units.Count(u => JumpBridgeUnitService.IsJumpBridgeBuilding(u)), Is.EqualTo(1));
        Assert.That(bf.units[0].bridgeId, Is.EqualTo("br-a"));
    }
}
