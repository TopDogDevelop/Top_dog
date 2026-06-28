using TopDog.Content.Map;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class BattlefieldSceneProxyServiceTests
{
    [Test]
    public void SyncForBattlefield_CreatesProxyForMapRegionWithoutLoadedBattlefield()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            map = SampleMap(),
        };
        var bf1 = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "star-a",
            anchorAu = new[] { 0f, 0f, 0f },
        };
        state.battlefields.Add(bf1);
        state.activeBattlefieldId = "bf-a";

        BattlefieldSceneProxyService.SyncForBattlefield(state, bf1);

        var proxy = bf1.units.Find(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(proxy, Is.Not.Null);
        Assert.That(proxy!.sceneProxyTargetSystemId, Is.EqualTo("sys-a"));
        Assert.That(proxy.sceneProxyTargetEventRegionId, Is.EqualTo("belt-a"));
        Assert.That(proxy.displayName, Does.Contain("矿带"));
        var dist = MathF.Sqrt(proxy.x * proxy.x + proxy.y * proxy.y + proxy.z * proxy.z);
        var expectedR = DistanceUnits.KmToMeters(100);
        Assert.That(dist, Is.EqualTo(expectedR).Within(1f));
        Assert.That(proxy.z, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void SyncForBattlefield_UsesVerticalAngleFromAuDelta()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            map = SampleMap(withElevatedRegion: true),
        };
        var bf1 = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "star-a",
            anchorAu = new[] { 0f, 0f, 0f },
        };
        state.battlefields.Add(bf1);
        state.activeBattlefieldId = "bf-a";

        BattlefieldSceneProxyService.SyncForBattlefield(state, bf1);

        var elevated = bf1.units.Find(u =>
            BattlefieldSceneProxyService.IsSceneProxy(u)
            && "gate-a".Equals(u.sceneProxyTargetEventRegionId, StringComparison.Ordinal));
        Assert.That(elevated, Is.Not.Null);
        var dist = MathF.Sqrt(elevated!.x * elevated.x + elevated.y * elevated.y + elevated.z * elevated.z);
        var expectedR = DistanceUnits.KmToMeters(100);
        Assert.That(dist, Is.EqualTo(expectedR).Within(1f));
        Assert.That(MathF.Abs(elevated.z), Is.GreaterThan(1000f));
    }

    [Test]
    public void SyncForBattlefield_StoresAzimuthElevationOnProxy()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            map = SampleMap(withElevatedRegion: true),
        };
        var bf1 = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "star-a",
            anchorAu = new[] { 0f, 0f, 0f },
        };
        state.battlefields.Add(bf1);

        BattlefieldSceneProxyService.SyncForBattlefield(state, bf1);

        var belt = bf1.units.Find(u =>
            BattlefieldSceneProxyService.IsSceneProxy(u)
            && "belt-a".Equals(u.sceneProxyTargetEventRegionId, StringComparison.Ordinal));
        var gate = bf1.units.Find(u =>
            BattlefieldSceneProxyService.IsSceneProxy(u)
            && "gate-a".Equals(u.sceneProxyTargetEventRegionId, StringComparison.Ordinal));
        Assert.That(belt, Is.Not.Null);
        Assert.That(belt!.sceneProxyAzimuthRad, Is.EqualTo(0f).Within(0.01f));
        Assert.That(belt.sceneProxyElevationRad, Is.EqualTo(0f).Within(0.01f));
        Assert.That(gate, Is.Not.Null);
        Assert.That(MathF.Abs(gate!.sceneProxyElevationRad), Is.GreaterThan(0.01f));
    }

    [Test]
    public void ComputePerspectiveScreenPlacement_OnScreenAtTopDownHorizonRing()
    {
        var (_, _, cx, cy, _, _, onScreen) = BattlefieldSceneProxyService.ComputePerspectiveScreenPlacement(
            0f, 0f, 0f, MathF.PI * 0.5f, 72f, 800f, 600f, 6f, 16f);
        Assert.That(onScreen, Is.True);
        Assert.That(cx, Is.GreaterThan(700f));
        Assert.That(cy, Is.EqualTo(300f).Within(2f));
    }

    [Test]
    public void ComputePerspectiveScreenPlacement_OnScreenWhenInPerspectiveFrustum()
    {
        var (_, _, cx, cy, _, _, onScreen) = BattlefieldSceneProxyService.ComputePerspectiveScreenPlacement(
            MathF.PI * 0.5f, 0.2f, 0f, 1f, 72f, 800f, 600f, 6f, 16f);
        Assert.That(onScreen, Is.True);
        Assert.That(cx, Is.EqualTo(400f).Within(40f));
        Assert.That(cy, Is.GreaterThan(80f));
        Assert.That(cy, Is.LessThan(520f));
    }

    [Test]
    public void ComputePerspectiveScreenEdge_MovesWithOrbitYaw()
    {
        var (left0, _, _, _) = BattlefieldSceneProxyService.ComputePerspectiveScreenEdge(
            0f, 0f, 0f, MathF.PI * 0.5f, 72f, 800f, 600f, 6f, 16f);
        var (left1, _, _, _) = BattlefieldSceneProxyService.ComputePerspectiveScreenEdge(
            0f, 0f, MathF.PI * 0.5f, MathF.PI * 0.5f, 72f, 800f, 600f, 6f, 16f);
        Assert.That(left0, Is.Not.EqualTo(left1).Within(1f));
    }

    [Test]
    public void ComputeScreenEdgeDirection_RotatesWithOrbitYaw()
    {
        var (x0, _) = BattlefieldSceneProxyService.ComputeScreenEdgeDirection(0f, 0f, 0f, MathF.PI * 0.5f);
        var (x1, _) = BattlefieldSceneProxyService.ComputeScreenEdgeDirection(0f, 0f, MathF.PI * 0.5f, MathF.PI * 0.5f);
        Assert.That(x0, Is.GreaterThan(0.9f));
        Assert.That(x1, Is.EqualTo(0f).Within(0.05f));
    }

    [Test]
    public void OrderFocus_RejectsSceneProxyTarget()
    {
        var state = new GameState();
        var bf = new BattlefieldState { battlefieldId = "bf-a", systemId = "sys-a" };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "scene-proxy-sys-a-belt-a",
            isSceneProxy = true,
            sceneProxyTargetSystemId = "sys-a",
            sceneProxyTargetEventRegionId = "belt-a",
            tonnageClass = BattlefieldSceneProxyService.TonnageClass,
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
        });
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "f1",
            side = UnitSide.FRIENDLY,
            memberId = "m1",
            alive = true,
            arrivalAtSec = 0f,
        });

        var msg = FleetOrderService.OrderFocus(state, bf, "scene-proxy-sys-a-belt-a", new[] { "f1" });
        Assert.That(msg, Does.StartWith("已下令"));
        Assert.That(bf.units.First(u => u.unitId == "f1").targetUnitId, Is.EqualTo("scene-proxy-sys-a-belt-a"));
    }

    [Test]
    public void TryResolveWarpTargetScene_FromSelectedProxy()
    {
        var bf = new BattlefieldState { battlefieldId = "bf-a", systemId = "sys-a" };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "scene-proxy-sys-a-belt-a",
            isSceneProxy = true,
            sceneProxyTargetSystemId = "sys-a",
            sceneProxyTargetEventRegionId = "belt-a",
            tonnageClass = BattlefieldSceneProxyService.TonnageClass,
        });

        Assert.That(
            FleetOrderService.TryResolveWarpTargetScene(bf, "scene-proxy-sys-a-belt-a", out var sys, out var region),
            Is.True);
        Assert.That(sys, Is.EqualTo("sys-a"));
        Assert.That(region, Is.EqualTo("belt-a"));
    }

    [Test]
    public void EnsureSceneBattlefield_LazilyCreatesEmptyBattlefield()
    {
        var state = new GameState { map = SampleMap() };
        var bf = TacticalSceneBattlefieldService.EnsureSceneBattlefield(state, "sys-a", "belt-a");
        Assert.That(bf.battlefieldId, Is.Not.Null);
        Assert.That(bf.systemId, Is.EqualTo("sys-a"));
        Assert.That(bf.eventRegionId, Is.EqualTo("belt-a"));
        Assert.That(bf.units, Is.Empty);
        Assert.That(state.battlefields, Has.Count.EqualTo(1));
    }

    private static LoadedMap SampleMap(bool withElevatedRegion = false)
    {
        var regions = new List<EventRegionDef>
        {
            new()
            {
                eventRegionId = "star-a",
                kind = EventRegionKinds.Star,
                anchorAu = new[] { 0f, 0f, 0f },
                radiusKm = 100,
            },
            new()
            {
                eventRegionId = "belt-a",
                kind = EventRegionKinds.OreBelt,
                name = "矿带",
                anchorAu = new[] { 2f, 0f, 0f },
                radiusKm = 500,
            },
        };
        if (withElevatedRegion)
        {
            regions.Add(new EventRegionDef
            {
                eventRegionId = "gate-a",
                kind = EventRegionKinds.JumpBridge,
                name = "跃迁门",
                anchorAu = new[] { 1f, 0f, 1f },
                radiusKm = 500,
            });
        }

        return new LoadedMap(
            new MapProject
            {
                systems =
                {
                    new SolarSystemDef
                    {
                        solarSystemId = "sys-a",
                        name = "Alpha",
                        eventRegions = regions,
                    },
                },
            },
            securityBands: null);
    }
}
