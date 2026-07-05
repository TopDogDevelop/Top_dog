using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Map;
using TopDog.Lobby;
using TopDog.Sim.Combat;
using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using TopDog.Sim.Vision;

namespace TopDog.Core.Tests;

public sealed class SkirmishWarpIntegrationTests
{
    [Test]
    public void CreateFromSkirmishLobby_DoesNotSeedTutorialMembers()
    {
        var lobby = BuildLobby();
        var core = CampaignBootstrap.CreateFromSkirmishLobby(lobby);
        Assert.That(core.State.members.Exists(m => m.name == "林准将"), Is.False);
    }

    [Test]
    public void SkirmishBootstrap_ActiveBattlefield_AlwaysHasSceneProxiesAfterLabelSync()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var from = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(from, Is.Not.Null);

        BattlefieldSceneProxyService.SyncForBattlefield(state, from!);
        var count = from!.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(count, Is.GreaterThan(0), "post-label sync must keep off-scene proxies on active battlefield");
    }

    [Test]
    public void SkirmishBootstrap_OrderWarpFromSwitchedBattlefield_UsesFriendlyShipBattlefield()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var shipBf = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(shipBf, Is.Not.Null);
        Assert.That(shipBf!.units.Exists(u => u.memberId == "m1"), Is.True);

        var emptyBf = state.battlefields.Find(b =>
            b.battlefieldId != null
            && !b.battlefieldId.Equals(state.activeBattlefieldId, StringComparison.Ordinal)
            && b.systemId != null
            && b.systemId.Equals(shipBf.systemId, StringComparison.Ordinal)
            && !b.units.Exists(u => u.memberId == "m1"));
        Assert.That(emptyBf, Is.Not.Null, "need another scene without player ship");

        state.activeBattlefieldId = emptyBf!.battlefieldId;
        BattlefieldSceneProxyService.SyncForBattlefield(state, emptyBf);

        var proxy = emptyBf.units.Find(u =>
            BattlefieldSceneProxyService.IsSceneProxy(u)
            && u.sceneProxyTargetEventRegionId != null
            && !u.sceneProxyTargetEventRegionId.Equals(shipBf.eventRegionId, StringComparison.Ordinal)
            && !u.sceneProxyTargetEventRegionId.Equals(emptyBf.eventRegionId, StringComparison.Ordinal));
        Assert.That(proxy, Is.Not.Null, "switched battlefield should show proxy to a third scene");

        state.possessingMemberId = "m1";
        var ships = ShipRegistry.LoadDefault();
        var ack = FleetOrderService.OrderWarpToSceneTarget(
            state,
            emptyBf,
            proxy!.unitId,
            ships,
            allFriendly: true);
        Assert.That(ack, Does.StartWith("已下令"), () => $"warp from empty scene failed: {ack}");

        var unit = shipBf.units.Find(u => u.memberId == "m1");
        Assert.That(unit, Is.Not.Null);
        Assert.That(unit!.warpPhase, Is.Not.EqualTo(TacticalWarpPhase.None));
        Assert.That(state.activeBattlefieldId, Is.EqualTo(shipBf.battlefieldId));
    }

    [Test]
    public void SkirmishBootstrap_OrderWarpFromSwitchedBattlefield_ShipMovesAfterTicks()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var shipBf = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(shipBf, Is.Not.Null);

        var emptyBf = state.battlefields.Find(b =>
            b.battlefieldId != null
            && !b.battlefieldId.Equals(state.activeBattlefieldId, StringComparison.Ordinal)
            && b.systemId != null
            && b.systemId.Equals(shipBf!.systemId, StringComparison.Ordinal)
            && !b.units.Exists(u => u.memberId == "m1"));
        Assert.That(emptyBf, Is.Not.Null);

        state.activeBattlefieldId = emptyBf!.battlefieldId;
        BattlefieldSceneProxyService.SyncForBattlefield(state, emptyBf);
        var proxy = emptyBf.units.Find(u =>
            BattlefieldSceneProxyService.IsSceneProxy(u)
            && u.sceneProxyTargetEventRegionId != null
            && !u.sceneProxyTargetEventRegionId.Equals(shipBf!.eventRegionId, StringComparison.Ordinal)
            && !u.sceneProxyTargetEventRegionId.Equals(emptyBf.eventRegionId, StringComparison.Ordinal));
        Assert.That(proxy, Is.Not.Null);

        state.possessingMemberId = "m1";
        var ships = ShipRegistry.LoadDefault();
        var ack = FleetOrderService.OrderWarpToSceneTarget(
            state, emptyBf, proxy!.unitId, ships, allFriendly: true);
        Assert.That(ack, Does.StartWith("已下令"));

        var unit = shipBf!.units.Find(u => u.memberId == "m1");
        Assert.That(unit, Is.Not.Null);
        var startX = unit!.x;
        var startY = unit.y;
        for (var i = 0; i < 120; i++)
        {
            BattlefieldSystem.Tick(state, ModuleRegistry.LoadDefault(), ships, 0.05f);
        }

        Assert.That(
            MathF.Abs(unit.x - startX) + MathF.Abs(unit.y - startY),
            Is.GreaterThan(0.5f),
            "ship should steer toward warp proxy after order");
    }

    [Test]
    public void SkirmishBootstrap_OrderWarpToSceneTarget_KeepsProxiesAndStartsWarp()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var from = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(from, Is.Not.Null);

        var proxy = from!.units.Find(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(proxy, Is.Not.Null, "expected scene proxy on active battlefield");
        Assert.That(proxy!.unitId, Is.Not.Null);

        var proxyCountBefore = from.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(proxyCountBefore, Is.GreaterThan(0));

        state.possessingMemberId = "m1";
        var ships = ShipRegistry.LoadDefault();
        var ack = FleetOrderService.OrderWarpToSceneTarget(
            state,
            from,
            proxy.unitId,
            ships,
            allFriendly: true);
        Assert.That(ack, Does.StartWith("已下令"), () => $"OrderWarpToSceneTarget failed: {ack}");

        var proxyCountAfter = from.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(
            proxyCountAfter,
            Is.EqualTo(proxyCountBefore),
            "scene proxies should remain after warp order");

        var unit = from.units.Find(u => u.memberId == "m1");
        Assert.That(unit, Is.Not.Null);
        Assert.That(unit!.warpPhase, Is.Not.EqualTo(TacticalWarpPhase.None));
    }

    [Test]
    public void SkirmishBootstrap_OrderWarp_ReachesTransit()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var from = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(from, Is.Not.Null);

        var to = state.battlefields.Find(b =>
            b.battlefieldId != null
            && !b.battlefieldId.Equals(state.activeBattlefieldId, StringComparison.Ordinal)
            && b.systemId != null
            && b.systemId.Equals(from!.systemId, StringComparison.Ordinal));
        Assert.That(to, Is.Not.Null, "skirmish map should have another scene in same system");

        Assert.That(
            from!.units.Exists(u => BattlefieldSceneProxyService.IsSceneProxy(u)),
            Is.True,
            "bootstrap should sync scene proxies on active battlefield");

        var unit = from.units.Find(u => u.memberId == "m1");
        Assert.That(unit, Is.Not.Null);

        state.possessingMemberId = "m1";
        var ships = ShipRegistry.LoadDefault();
        var ack = FleetOrderService.OrderWarp(state, from, to!.battlefieldId!, ships, allFriendly: true);
        Assert.That(ack, Does.StartWith("已下令"), () => $"OrderWarp failed: {ack}");

        unit = from.units.Find(u => u.memberId == "m1");
        Assert.That(unit, Is.Not.Null);
        Assert.That(unit!.warpPhase, Is.Not.EqualTo(TacticalWarpPhase.None));

        for (var i = 0; i < 500 && from.units.Contains(unit); i++)
        {
            TacticalWarpService.Tick(state, from, 0.05f);
        }

        Assert.That(
            state.tacticalWarpInTransit.Exists(e => e.unit.memberId == "m1"),
            Is.True,
            $"expected AU transit; warpPhase={unit.warpPhase} stillOnFrom={from.units.Contains(unit)}");
    }

    [Test]
    public void SkirmishBootstrap_RosterInheritsTemplateTraitsForDescentList()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var entries = VisionLocationService.ListDescentEntries(core.State);
        Assert.That(entries.Exists(e => e.MemberId == "m1"), Is.True, "template_1 roster should appear in descent rail");
    }

    [Test]
    public void SeedSceneProxies_Sealed_NoOpOnSecondCall()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var bf = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(bf, Is.Not.Null);
        Assert.That(bf!.sceneProxiesSealed, Is.True);
        var before = bf.units.Count(BattlefieldSceneProxyService.IsSceneProxy);

        BattlefieldSceneProxyService.SeedSceneProxies(state, bf);
        var after = bf.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public void SyncForBattlefield_EmptyPlacements_KeepsExistingProxies()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var bf = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(bf, Is.Not.Null);
        var before = bf!.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(before, Is.GreaterThan(0));

        var map = state.map;
        state.map = null;
        BattlefieldSceneProxyService.SyncForBattlefield(state, bf);
        var after = bf.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        state.map = map;
        Assert.That(after, Is.EqualTo(before), "map unavailable should not wipe sealed scene proxies");
    }

    [Test]
    public void ListBattlefieldVisionGroups_SkipsEmptyBattlefields()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var groups = VisionLocationService.ListBattlefieldVisionGroups(core.State);
        Assert.That(groups.Count, Is.GreaterThan(0));
        Assert.That(groups.Exists(g => g.Characters.Exists(c => c.MemberId == "m1")), Is.True);
        foreach (var g in groups)
        {
            Assert.That(g.Characters.Count, Is.GreaterThan(0));
        }
    }

    [Test]
    public void FleetOrders_DoNotWipeSceneProxiesOnActiveBattlefield()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var bf = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(bf, Is.Not.Null);
        var before = bf!.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(before, Is.GreaterThan(0));

        FleetOrderService.OrderStop(state, bf, true);
        var afterStop = bf.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(afterStop, Is.EqualTo(before), "OrderStop should keep scene proxies");

        FleetOrderService.EnsureCommandSceneReady(state, bf);
        var afterEnsure = bf.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(afterEnsure, Is.EqualTo(before), "EnsureCommandSceneReady should keep scene proxies");

        var proxy = bf.units.Find(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(proxy?.unitId, Is.Not.Null);
        FleetOrderService.OrderApproach(state, bf, proxy!.unitId, null);
        var afterApproach = bf.units.Count(BattlefieldSceneProxyService.IsSceneProxy);
        Assert.That(afterApproach, Is.EqualTo(before), "OrderApproach should keep scene proxies");
    }

    [Test]
    public void Bootstrap_AllSkirmishBattlefields_HaveSceneProxiesAfterSpawn()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        foreach (var bf in core.State.battlefields)
        {
            if (bf.finished || bf.eventRegionId == null
                || EventRegionKinds.IsStar(
                    core.State.map?.Project?.systems[0].eventRegions
                        .Find(er => er.eventRegionId == bf.eventRegionId)?.kind))
            {
                continue;
            }

            Assert.That(
                bf.units.Count(BattlefieldSceneProxyService.IsSceneProxy),
                Is.GreaterThan(0),
                () => $"battlefield {bf.battlefieldId} should expose off-scene markers");
        }
    }

    [Test]
    public void ListBattlefieldVisionGroups_IneligibleUnitDoesNotBlockRosterFallback()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var bf = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(bf, Is.Not.Null);
        var localLegion = state.legions.Find(l => l.isLocal);
        Assert.That(localLegion?.legionId, Is.Not.Null);

        state.members.Add(new MemberState
        {
            memberId = "nox",
            name = "NoTrait",
            legionId = localLegion!.legionId,
        });
        bf!.units.Add(new BattlefieldUnit
        {
            unitId = "u-nox",
            memberId = "nox",
            side = UnitSide.FRIENDLY,
            hullId = "hull_frigate_pineapple",
            legionId = localLegion.legionId,
        });

        var groups = VisionLocationService.ListBattlefieldVisionGroups(state);
        Assert.That(groups.Count, Is.GreaterThan(0));
        Assert.That(groups.Exists(g => g.Characters.Exists(c => c.MemberId == "m1")), Is.True);
    }

    [Test]
    public void ResolveCommandTargets_SceneMembers_OneShipPerLocalMember()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var bf = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(bf, Is.Not.Null);

        var sceneTargets = FleetOrderService.ResolveCommandTargets(state, bf!, null).ToList();
        var allFriendly = FleetOrderService.ResolveCommandTargets(bf!, null).ToList();
        Assert.That(sceneTargets.Count, Is.GreaterThan(0));
        Assert.That(sceneTargets.Count, Is.LessThanOrEqualTo(allFriendly.Count));
        Assert.That(sceneTargets.Select(u => u.memberId).Distinct().Count(), Is.EqualTo(sceneTargets.Count));
    }

    [Test]
    public void ApplyFortressSpawnOffset_DistributesWithinSphereVolume()
    {
        var rng = new Random(42);
        const float radius = 1000f;
        var maxR = 0f;
        var sawNonZeroZ = false;
        for (var i = 0; i < 32; i++)
        {
            var u = new BattlefieldUnit();
            SkirmishSpawnService.ApplyFortressSpawnOffset(u, rng, radius);
            var r = MathF.Sqrt(u.x * u.x + u.y * u.y + u.z * u.z);
            maxR = MathF.Max(maxR, r);
            if (MathF.Abs(u.z) > 1f)
            {
                sawNonZeroZ = true;
            }
        }

        Assert.That(maxR, Is.LessThanOrEqualTo(radius + 0.01f));
        Assert.That(sawNonZeroZ, Is.True);
    }

    [Test]
    public void CheckVictory_FriendlyOnlyField_DoesNotFinishActiveSkirmishBattlefield()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            activeBattlefieldId = "bf1",
        };
        var bf = new BattlefieldState
        {
            battlefieldId = "bf1",
            timeSec = 1f,
        };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "u1",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            alive = true,
            throttleOn = true,
            maxSpeedMps = 100f,
            accelMps2 = 10f,
        });
        state.battlefields.Add(bf);

        BattlefieldSystem.Tick(
            state,
            ModuleRegistry.LoadDefault(),
            ShipRegistry.LoadDefault(),
            0.5f);

        Assert.That(bf.finished, Is.False);
        Assert.That(bf.units[0].SpeedMps(), Is.GreaterThan(0f));
    }

    [Test]
    public void HealActiveSkirmishBattlefield_ReopensMisfinishedFriendlyWin()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            activeBattlefieldId = "bf1",
        };
        var bf = new BattlefieldState
        {
            battlefieldId = "bf1",
            timeSec = 1f,
            finished = true,
            winnerSide = UnitSide.FRIENDLY,
        };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "u1",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            alive = true,
            throttleOn = true,
            maxSpeedMps = 100f,
            accelMps2 = 10f,
        });
        state.battlefields.Add(bf);

        BattlefieldSystem.Tick(
            state,
            ModuleRegistry.LoadDefault(),
            ShipRegistry.LoadDefault(),
            0.5f);

        Assert.That(bf.finished, Is.False);
        Assert.That(bf.units[0].SpeedMps(), Is.GreaterThan(0f));
    }

    [Test]
    public void NonActiveBattlefieldWithShips_StillAdvancesTimeSec()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            activeBattlefieldId = "bf_active",
        };
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf_active",
            timeSec = 0f,
            finished = true,
            winnerSide = UnitSide.FRIENDLY,
        });
        var other = new BattlefieldState
        {
            battlefieldId = "bf_other",
            timeSec = 2f,
            finished = true,
            winnerSide = UnitSide.ENEMY,
        };
        other.units.Add(new BattlefieldUnit
        {
            unitId = "enemy1",
            side = UnitSide.ENEMY,
            arrivalAtSec = 0f,
            alive = true,
        });
        state.battlefields.Add(other);

        BattlefieldSystem.Tick(
            state,
            ModuleRegistry.LoadDefault(),
            ShipRegistry.LoadDefault(),
            0.5f);

        Assert.That(other.timeSec, Is.EqualTo(2.5f).Within(0.001f));
        Assert.That(BattlefieldSystem.HasShipCombatPresence(state, other), Is.True);
    }

    [Test]
    public void CreateFromSkirmishLobby_SkipsOperationsAndCombatPrep()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;

        Assert.That(state.phase, Is.EqualTo(GamePhase.COMBAT));
        Assert.That(state.combatRealtimeActive, Is.True);
        Assert.That(state.combatQueue, Is.Empty);
        Assert.That(SkirmishPhaseRules.IsActiveMatch(state), Is.True);

        core.SetPhase(GamePhase.COMBAT_PREP);
        Assert.That(state.phase, Is.EqualTo(GamePhase.COMBAT));

        CombatPhaseService.EnterCombatPrep(state, ShipRegistry.LoadDefault());
        Assert.That(state.phase, Is.EqualTo(GamePhase.COMBAT));

        CombatPhaseService.BeginOperationsRound(state, ShipRegistry.LoadDefault());
        Assert.That(state.phase, Is.EqualTo(GamePhase.COMBAT));
    }

    [Test]
    public void Skirmish_FinishedEmptyBattlefield_IsNotPrunedDuringTick()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var emptyBf = state.battlefields.Find(b =>
            b.battlefieldId != null
            && !b.units.Exists(u => u.memberId == "m1" && !u.isBuilding));
        Assert.That(emptyBf, Is.Not.Null);
        emptyBf!.finished = true;

        var countBefore = state.battlefields.Count;
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        for (var i = 0; i < 20; i++)
        {
            BattlefieldSystem.Tick(state, modules, ships, 0.05f);
        }

        Assert.That(state.battlefields.Count, Is.EqualTo(countBefore));
        Assert.That(state.battlefields.Contains(emptyBf), Is.True);
    }

    [Test]
    public void Skirmish_InTransit_ArrivesAtFinishedEmptyBattlefield()
    {
        var core = CampaignBootstrap.CreateFromSkirmishLobby(BuildLobby());
        var state = core.State;
        var shipBf = state.battlefields.Find(b => b.battlefieldId == state.activeBattlefieldId);
        Assert.That(shipBf, Is.Not.Null);
        var targetBf = state.battlefields.Find(b =>
            b.battlefieldId != null
            && !b.battlefieldId.Equals(shipBf!.battlefieldId, StringComparison.Ordinal));
        Assert.That(targetBf, Is.Not.Null);
        targetBf!.finished = true;

        var unit = shipBf!.units.Find(u => u.memberId == "m1");
        Assert.That(unit, Is.Not.Null);
        shipBf.units.Remove(unit!);
        unit!.warpPhase = TacticalWarpPhase.InTransit;
        unit.inTacticalWarp = true;
        unit.warpTargetBfId = targetBf.battlefieldId;
        unit.warpFromBfId = shipBf.battlefieldId;
        state.tacticalWarpInTransit.Add(new TacticalWarpTransitEntry
        {
            unit = unit,
            fromBattlefieldId = shipBf.battlefieldId,
            toBattlefieldId = targetBf.battlefieldId,
            remainingSec = 0.01f,
            landingDistM = 25_000f,
        });

        BattlefieldSystem.Tick(state, ModuleRegistry.LoadDefault(), ShipRegistry.LoadDefault(), 0.05f);

        Assert.That(state.tacticalWarpInTransit, Is.Empty);
        Assert.That(targetBf.units.Contains(unit), Is.True);
        Assert.That(unit.warpPhase, Is.EqualTo(TacticalWarpPhase.EntryBurst));
    }

    private static SkirmishLobbyState BuildLobby()
    {
        var lobby = new SkirmishLobbyState { scale = 10, seed = 99 };
        var human = new LobbyPlayer { local = true, displayName = "P", memberTemplateId = "template_1" };
        var ai = new LobbyPlayer { kind = LobbyPlayerKind.AI, displayName = "AI" };
        lobby.players.Add(human);
        lobby.players.Add(ai);
        lobby.rosterByPlayerId[human.playerId] =
        [
            new SkirmishRosterSlot
            {
                memberTemplateId = "template_1",
                memberTemplateRowId = "template_1:10000001:01",
                memberId = "m1",
                displayName = "Pilot",
                hullId = "hull_frigate_pineapple",
            },
        ];
        return lobby;
    }
}
