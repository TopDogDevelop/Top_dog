using TopDog.App;
using TopDog.Content.Ships;
using TopDog.Content.Modules;
using TopDog.Lobby;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using TopDog.Sim.Vision;

namespace TopDog.Core.Tests;

public sealed class SkirmishPresetSchemeTests
{
    [Test]
    public void ExtractAndApply_PreservesRosterShape()
    {
        var lobby = new SkirmishLobbyState { scale = 30, mode = SkirmishLobbyMode.VsAi };
        var local = new LobbyPlayer { local = true, displayName = "P" };
        lobby.players.Add(local);
        lobby.rosterByPlayerId[local.playerId] =
        [
            new SkirmishRosterSlot
            {
                memberId = "m1",
                displayName = "Alpha",
                hullId = "hull_frigate_pineapple",
            },
        ];

        var scheme = SkirmishPresetScheme.Extract(lobby);
        Assert.That(scheme, Is.Not.Null);
        Assert.That(scheme!.RosterLines, Has.Count.EqualTo(1));
        Assert.That(SkirmishPresetScheme.FormatSummary(scheme), Does.Contain("Alpha"));

        var target = new SkirmishLobbyState();
        target.players.Add(new LobbyPlayer { local = true, displayName = "P2" });
        SkirmishPresetScheme.ApplyToLobby(target, scheme);
        Assert.That(target.scale, Is.EqualTo(30));
        Assert.That(target.rosterByPlayerId[target.FindLocal()!.playerId], Has.Count.EqualTo(1));
        Assert.That(target.rosterByPlayerId[target.FindLocal()!.playerId][0].displayName, Is.EqualTo("Alpha"));
    }
}

public sealed class VisionLocationServiceTests
{
    [Test]
    public void ListDescentEntries_ExcludesNonVisionAnchorTrait()
    {
        var state = new GameState { combatRealtimeActive = true, activeBattlefieldId = "bf-a" };
        state.members.Add(new MemberState
        {
            memberId = "m-board",
            name = "董事会",
            traitIds = { "trait_board_summon" },
            equippedHullId = "hull_frigate_pineapple",
        });
        var bf = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "reg-a",
            subLocation = "矿带",
            units =
            {
                new BattlefieldUnit
                {
                    unitId = "u1",
                    memberId = "m-board",
                    side = UnitSide.FRIENDLY,
                    arrivalAtSec = 0f,
                },
            },
        };
        state.battlefields.Add(bf);

        var entries = VisionLocationService.ListDescentEntries(state);
        Assert.That(entries.Exists(e => e.MemberId == "m-board"), Is.False);
    }

    [Test]
    public void ListDescentEntries_IncludesLoyalAndIntelOnField()
    {
        var state = new GameState { combatRealtimeActive = true, activeBattlefieldId = "bf-a" };
        state.members.Add(new MemberState
        {
            memberId = "m-loyal",
            name = "死忠舰长",
            traitIds = { VisionLocationService.TraitPossess },
            equippedHullId = "hull_frigate_pineapple",
        });
        state.members.Add(new MemberState
        {
            memberId = "m-intel",
            name = "情报官",
            traitIds = { VisionLocationService.TraitTacticalLink },
            equippedHullId = "hull_frigate_pineapple",
        });
        var bf = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "reg-a",
            subLocation = "矿带",
            units =
            {
                new BattlefieldUnit
                {
                    unitId = "u1",
                    memberId = "m-loyal",
                    side = UnitSide.FRIENDLY,
                    arrivalAtSec = 0f,
                },
                new BattlefieldUnit
                {
                    unitId = "u2",
                    memberId = "m-intel",
                    side = UnitSide.FRIENDLY,
                    arrivalAtSec = 0f,
                },
            },
        };
        state.battlefields.Add(bf);

        var entries = VisionLocationService.ListDescentEntries(state);
        Assert.That(entries, Has.Count.EqualTo(2));
        Assert.That(entries.Exists(e => e.CanPossess && e.MemberId == "m-loyal"), Is.True);
        Assert.That(entries.Exists(e => e.CanTacticalLink && e.MemberId == "m-intel"), Is.True);
    }

    [Test]
    public void SkirmishBootstrap_TemplateRowTraitsAppearInDescentList()
    {
        var lobby = new SkirmishLobbyState { scale = 10, seed = 42 };
        var human = new LobbyPlayer { local = true, displayName = "P", memberTemplateId = "template_1" };
        var ai = new LobbyPlayer { kind = LobbyPlayerKind.AI, displayName = "AI", memberTemplateId = "template_1" };
        lobby.players.Add(human);
        lobby.players.Add(ai);
        lobby.rosterByPlayerId[human.playerId] =
        [
            new SkirmishRosterSlot
            {
                memberTemplateId = "template_1",
                memberTemplateRowId = "template_1:10000001:01",
                memberId = "sk_loyal",
                displayName = "奥法凯",
                hullId = "hull_frigate_pineapple",
            },
        ];
        lobby.rosterByPlayerId[ai.playerId] = [];

        var state = new GameState();
        SkirmishLobbyBootstrap.ApplyToState(state, lobby);
        var member = state.members.Find(m => m.memberId == "sk_loyal");
        Assert.That(member, Is.Not.Null);
        Assert.That(member!.traitIds, Does.Contain(VisionLocationService.TraitPossess));

        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        SkirmishSpawnService.BootstrapBattlefields(state, ships, modules, new Random(1));

        var entries = VisionLocationService.ListDescentEntries(state);
        Assert.That(entries.Exists(e => e.MemberId == "sk_loyal" && e.CanPossess), Is.True);
    }

    [Test]
    public void OrderApproach_AcceptsSceneProxyTarget()
    {
        var state = new GameState { combatRealtimeActive = true };
        var bf = new BattlefieldState { battlefieldId = "bf-a", systemId = "sys-a" };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "proxy",
            isSceneProxy = true,
            tonnageClass = BattlefieldSceneProxyService.TonnageClass,
            sceneProxyTargetSystemId = "sys-a",
            sceneProxyTargetEventRegionId = "belt-a",
            side = UnitSide.FRIENDLY,
            alive = true,
        });
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "f1",
            side = UnitSide.FRIENDLY,
            memberId = "m1",
            alive = true,
            arrivalAtSec = 0f,
        });

        var msg = FleetOrderService.OrderApproach(state, bf, "proxy", null);
        Assert.That(msg, Does.StartWith("已下令"));
        Assert.That(bf.units.First(u => u.unitId == "f1").approachTargetUnitId, Is.EqualTo("proxy"));
    }

    [Test]
    public void ExplainEmptyDescentList_DistinguishesNoAnchorVsNotOnField()
    {
        var state = new GameState { combatRealtimeActive = true };
        Assert.That(
            VisionLocationService.ExplainEmptyDescentList(state),
            Does.Contain("可附身或情报员"));

        state.members.Add(new MemberState
        {
            memberId = "m1",
            traitIds = { VisionLocationService.TraitPossess },
        });
        Assert.That(
            VisionLocationService.ExplainEmptyDescentList(state),
            Does.Contain("已上场"));
    }

    [Test]
    public void ListDescentEntries_IncludesAnchorInAuTransit()
    {
        var state = new GameState { combatRealtimeActive = true };
        state.members.Add(new MemberState
        {
            memberId = "m1",
            name = "跃迁舰长",
            traitIds = { VisionLocationService.TraitPossess },
        });
        var from = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "reg-a",
            subLocation = "军堡",
        };
        var to = new BattlefieldState
        {
            battlefieldId = "bf-b",
            systemId = "sys-a",
            eventRegionId = "reg-b",
            subLocation = "矿带",
        };
        state.battlefields.Add(from);
        state.battlefields.Add(to);
        state.tacticalWarpInTransit.Add(new TacticalWarpTransitEntry
        {
            unit = new BattlefieldUnit
            {
                unitId = "u-transit",
                memberId = "m1",
                side = UnitSide.FRIENDLY,
                alive = true,
            },
            fromBattlefieldId = from.battlefieldId,
            toBattlefieldId = to.battlefieldId,
        });

        var entries = VisionLocationService.ListDescentEntries(state);
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].InTransit, Is.True);
        Assert.That(entries[0].BattlefieldId, Is.EqualTo("bf-b"));
    }

    [Test]
    public void ListDescentEntries_SkipsOnFieldDuplicateWhenInTransit()
    {
        var state = new GameState { combatRealtimeActive = true };
        state.members.Add(new MemberState
        {
            memberId = "m1",
            traitIds = { VisionLocationService.TraitTacticalLink },
        });
        var from = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "reg-a",
            timeSec = 1f,
            units =
            {
                new BattlefieldUnit
                {
                    unitId = "u-old",
                    memberId = "m1",
                    side = UnitSide.FRIENDLY,
                    alive = true,
                    arrivalAtSec = 0f,
                },
            },
        };
        var to = new BattlefieldState { battlefieldId = "bf-b", systemId = "sys-a", eventRegionId = "reg-b" };
        state.battlefields.Add(from);
        state.battlefields.Add(to);
        state.tacticalWarpInTransit.Add(new TacticalWarpTransitEntry
        {
            unit = new BattlefieldUnit
            {
                unitId = "u-transit",
                memberId = "m1",
                side = UnitSide.FRIENDLY,
                alive = true,
            },
            fromBattlefieldId = from.battlefieldId,
            toBattlefieldId = to.battlefieldId,
        });

        var entries = VisionLocationService.ListDescentEntries(state);
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].UnitId, Is.EqualTo("u-transit"));
    }

    [Test]
    public void ListDescentEntries_UsesIdentityTraitsWhenMemberListCleared()
    {
        var state = new GameState { combatRealtimeActive = true };
        state.members.Add(new MemberState
        {
            memberId = "m1",
            name = "锚点",
            identityCode = "10000001",
            accountSuffix = "01",
        });
        state.identities["10000001"] = new IdentityState
        {
            identityCode = "10000001",
            traitIds = { VisionLocationService.TraitPossess },
        };
        var bf = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "reg-a",
            timeSec = 1f,
            units =
            {
                new BattlefieldUnit
                {
                    unitId = "u1",
                    memberId = "m1",
                    side = UnitSide.FRIENDLY,
                    alive = true,
                    arrivalAtSec = 0f,
                },
            },
        };
        state.battlefields.Add(bf);

        var entries = VisionLocationService.ListDescentEntries(state);
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].MemberId, Is.EqualTo("m1"));
    }

    [Test]
    public void SkirmishSpawn_SeedsInitialPossessionForAnchorMember()
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
        lobby.rosterByPlayerId[ai.playerId] = [];

        var core = CampaignBootstrap.CreateFromSkirmishLobby(lobby);
        Assert.That(
            core.State.battlefields.Exists(bf => bf.units.Exists(u => u.memberId == "m1")),
            Is.True,
            "anchor member should spawn on a battlefield");
        Assert.That(core.State.possessingMemberId, Is.EqualTo("m1"));
    }

    [Test]
    public void ListTacticalFocusCandidates_Skirmish_OnlyVisionAnchorMembers()
    {
        var state = new GameState
        {
            combatRealtimeActive = true,
            worldline = { type = WorldlineType.LEGION_SKIRMISH },
            skirmish = new SkirmishMatchState { scale = 10 },
        };
        state.members.Add(new MemberState
        {
            memberId = "m-anchor",
            traitIds = { VisionLocationService.TraitPossess },
        });
        state.members.Add(new MemberState
        {
            memberId = "m-other",
            traitIds = { "trait_board_summon" },
        });
        var bf = new BattlefieldState { battlefieldId = "bf-a", timeSec = 1f };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "u-anchor",
            memberId = "m-anchor",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
        });
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "u-other",
            memberId = "m-other",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
        });

        var list = VisionAnchorService.ListTacticalFocusCandidates(state, bf);
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0].unitId, Is.EqualTo("u-anchor"));
    }
}
