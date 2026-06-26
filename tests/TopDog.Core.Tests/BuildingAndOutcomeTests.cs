using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class BuildingAndOutcomeTests
{
    private static GameState StateWithPlanetSystem()
    {
        var state = new GameState
        {
            phase = GamePhase.OPERATIONS,
            currentSolarSystemId = "sys_a",
            legionStock = { [CurrencyIds.StarCoin] = 10_000 },
        };
        var project = new MapProject();
        project.systems.Add(new SolarSystemDef
        {
            solarSystemId = "sys_a",
            name = "Alpha",
            eventRegions = new List<EventRegionDef>
            {
                new() { eventRegionId = "er_planet_1", kind = EventRegionKinds.Planet, name = "Planet One" },
            },
        });
        state.map = new LoadedMap(project, null);
        state.members.Add(new MemberState
        {
            memberId = "m1",
            identityCode = "id1",
            name = "Tester",
            multiboxGroupId = "mb_id1",
            currentSolarSystemId = "sys_a",
            energy = 5,
        });
        state.personalStockByGroup["mb_id1"] = new Dictionary<string, int>
        {
            [CurrencyIds.StarCoin] = 5_000,
        };
        return state;
    }

    [Test]
    public void SeedCampaignFortresses_TwoAiLegions_SameSpawn_OnlyOneFortPerSystem()
    {
        var state = new GameState { currentSolarSystemId = "sys_a" };
        var project = new MapProject();
        project.systems.Add(new SolarSystemDef { solarSystemId = "sys_a", name = "Alpha" });
        project.systems.Add(new SolarSystemDef { solarSystemId = "sys_b", name = "Beta" });
        state.map = new LoadedMap(project, null);
        state.legions.Add(new LegionState
        {
            legionId = "legion-ai-1",
            displayName = "AI One",
            isAiControlled = true,
            spawnSolarSystemId = "sys_a",
        });
        state.legions.Add(new LegionState
        {
            legionId = "legion-ai-2",
            displayName = "AI Two",
            isAiControlled = true,
            spawnSolarSystemId = "sys_a",
        });

        BuildingService.SeedCampaignFortresses(state, new Random(3));

        var forts = state.buildings.Where(b => BuildingService.LegionFortress.Equals(b.buildingType)).ToList();
        Assert.That(forts, Has.Count.EqualTo(2));
        Assert.That(forts.Select(f => f.solarSystemId).Distinct().Count(), Is.EqualTo(2));
        Assert.That(BuildingService.HasLegionFortressInSystem(state, "sys_a"), Is.True);
        Assert.That(BuildingService.HasLegionFortressInSystem(state, "sys_b"), Is.True);
    }

    [Test]
    public void CreateLegionFortress_DebitsLegionCoins()
    {
        var state = StateWithPlanetSystem();
        var err = BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        Assert.That(err, Is.Null);
        Assert.That(state.legionStock[CurrencyIds.StarCoin], Is.EqualTo(7_000));
        Assert.That(state.buildings, Has.Count.EqualTo(1));
        Assert.That(state.buildings[0].buildingType, Is.EqualTo(BuildingService.LegionFortress));
        Assert.That(state.buildings[0].eventRegionId, Is.EqualTo("er_planet_1"));
    }

    [Test]
    public void CreateLegionFortress_SecondInSystemRejected()
    {
        var state = StateWithPlanetSystem();
        Assert.That(BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1"), Is.Null);
        var err = BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        Assert.That(err, Does.Contain("已有军团堡垒"));
        Assert.That(state.buildings, Has.Count.EqualTo(1));
    }

    [Test]
    public void DispatchAnchor_SetsMemberPlanetAndLegionFortress()
    {
        var state = StateWithPlanetSystem();
        var ships = ShipRegistry.LoadDefault();
        var msg = MemberDispatchService.DispatchToSystem(
            state, "m1", MemberDispatchService.TaskAnchor, "sys_a",
            MemberDispatchService.AnchorModeSystem, ships, null, "er_planet_1", true);
        Assert.That(msg, Does.Contain("军堡"));
        Assert.That(state.members[0].opsDeployEventRegionId, Is.EqualTo("er_planet_1"));
        Assert.That(state.buildings[0].eventRegionId, Is.EqualTo("er_planet_1"));
    }

    [Test]
    public void TryCreatePersonalFortress_RespectsLimitsAndCost()
    {
        var state = StateWithPlanetSystem();
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        state.legionStock[CurrencyIds.StarCoin] = 10_000;
        var m = state.members[0];
        var rng = new Random(1);
        Assert.That(BuildingService.TryCreatePersonalFortress(state, m, "sys_a", rng), Is.Null);
        Assert.That(state.personalStockByGroup["mb_id1"][CurrencyIds.StarCoin], Is.EqualTo(4_000));
        Assert.That(m.anchoredPersonalBuildingId, Is.Not.Null);
        var err = BuildingService.TryCreatePersonalFortress(state, m, "sys_a", rng);
        Assert.That(err, Does.Contain("已有个堡"));
    }

    [Test]
    public void PersonalFortressAssaultWin_DestroysWithoutFragile()
    {
        var state = StateWithPlanetSystem();
        var ships = ShipRegistry.LoadDefault();
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        var m = state.members[0];
        BuildingService.TryCreatePersonalFortress(state, m, "sys_a", new Random(2));
        var id = m.anchoredPersonalBuildingId;
        BuildingService.OnAssaultResolved(state, id, attackerWon: true, attackerIsAi: true, ships);
        Assert.That(BuildingService.Find(state, id), Is.Null);
        Assert.That(state.buildings.Any(b => BuildingService.Fragile.Equals(b.status)), Is.False);
    }

    [Test]
    public void OnlyPersonalForts_StripsDreadnoughtHull()
    {
        var state = StateWithPlanetSystem();
        var ships = ShipRegistry.LoadDefault();
        state.members[0].equippedHullId = "hull_dread_ironcoffin";
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        BuildingService.TryCreatePersonalFortress(state, state.members[0], "sys_a", new Random(3));
        var legion = state.buildings.First(b => BuildingService.LegionFortress.Equals(b.buildingType));
        BuildingService.DestroyBuilding(state, legion.buildingId, ships);
        Assert.That(state.members[0].equippedHullId, Is.Null);
    }

    [Test]
    public void NoDockableBuildings_EndsMatchWhenNoLegionsRemain()
    {
        var state = StateWithPlanetSystem();
        var ships = ShipRegistry.LoadDefault();
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        CampaignOutcomeService.Evaluate(state);
        Assert.That(state.matchEnded, Is.False);
        foreach (var b in state.buildings.ToList())
        {
            BuildingService.DestroyBuilding(state, b.buildingId, ships);
        }
        Assert.That(state.matchEnded, Is.True);
        Assert.That(state.campaignOutcome, Is.EqualTo(CampaignOutcomeService.Defeated));
    }

    [Test]
    public void PlayerDefeated_MatchContinuesWhileOtherLegionsFight()
    {
        var state = StateWithPlanetSystem();
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_ai_1",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_b",
            playerOwned = false,
            legionId = "AI_ALPHA",
            status = BuildingService.Normal,
        });
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_ai_2",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_c",
            playerOwned = false,
            legionId = "AI_BETA",
            status = BuildingService.Normal,
        });
        var playerBld = state.buildings.First(b => b.playerOwned).buildingId;
        BuildingService.DestroyBuilding(state, playerBld, ShipRegistry.LoadDefault());
        Assert.That(state.campaignOutcome, Is.EqualTo(CampaignOutcomeService.Defeated));
        Assert.That(state.matchEnded, Is.False);
        Assert.That(CampaignOutcomeService.ShouldOfferDefeatChoice(state), Is.True);
        BuildingService.DestroyBuilding(state, "bld_ai_1", ShipRegistry.LoadDefault());
        Assert.That(state.matchEnded, Is.True);
        Assert.That(state.matchWinnerLegionId, Is.EqualTo("AI_BETA"));
    }

    [Test]
    public void OnlyPlayerLegionRemaining_IsVictory()
    {
        var state = StateWithPlanetSystem();
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_ai",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_b",
            playerOwned = false,
            legionId = CampaignLegionIds.Ai,
            status = BuildingService.Normal,
        });
        state.peakLegionCount = 2;
        BuildingService.DestroyBuilding(state, "bld_ai", ShipRegistry.LoadDefault());
        CampaignOutcomeService.Evaluate(state);
        Assert.That(state.matchEnded, Is.True);
        Assert.That(state.campaignOutcome, Is.EqualTo(CampaignOutcomeService.Victory));
    }

    [Test]
    public void ShouldOfferDefeatChoice_WhenDefeatedButMatchActive()
    {
        var state = StateWithPlanetSystem();
        state.campaignOutcome = CampaignOutcomeService.Defeated;
        state.peakLegionCount = 3;
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_ai_1",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_b",
            playerOwned = false,
            legionId = "AI_ALPHA",
            status = BuildingService.Normal,
        });
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_ai_2",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_c",
            playerOwned = false,
            legionId = "AI_BETA",
            status = BuildingService.Normal,
        });
        Assert.That(CampaignOutcomeService.ShouldOfferDefeatChoice(state), Is.True);
        SpectatorModeService.EnterSpectator(state);
        Assert.That(CampaignOutcomeService.ShouldOfferDefeatChoice(state), Is.False);
    }

    [Test]
    public void MutualAssault_NoPersonalForts_EndsInDraw()
    {
        var state = StateWithPlanetSystem();
        state.phase = GamePhase.COMBAT;
        state.peakLegionCount = 3;
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_ai_1",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_b",
            playerOwned = false,
            legionId = "AI_ALPHA",
            status = BuildingService.Normal,
        });
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_ai_2",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_c",
            playerOwned = false,
            legionId = "AI_BETA",
            status = BuildingService.Normal,
        });
        var ships = ShipRegistry.LoadDefault();
        foreach (var b in state.buildings.ToList())
        {
            BuildingService.DestroyBuilding(state, b.buildingId, ships);
        }
        state.phase = GamePhase.OPERATIONS;
        CampaignOutcomeService.Evaluate(state);
        Assert.That(state.matchEnded, Is.True);
        Assert.That(state.campaignOutcome, Is.EqualTo(CampaignOutcomeService.Draw));
        Assert.That(state.matchWinnerLegionId, Is.Null);
        Assert.That(CampaignOutcomeService.DistinctEliminatedLegionsThisCombatRound(state), Is.EqualTo(3));
    }

    [Test]
    public void TotalWipe_OneLegionEliminatedThisRound_IsNotDraw()
    {
        var state = StateWithPlanetSystem();
        state.phase = GamePhase.COMBAT;
        state.peakLegionCount = 2;
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_ai",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_b",
            playerOwned = false,
            legionId = CampaignLegionIds.Ai,
            status = BuildingService.Normal,
        });
        BuildingService.DestroyBuilding(state, "bld_ai", ShipRegistry.LoadDefault());
        state.legionFortressEliminatedLegionIdsThisCombatRound.Clear();
        var playerId = state.buildings.First(b => b.playerOwned).buildingId;
        BuildingService.DestroyBuilding(state, playerId, ShipRegistry.LoadDefault());
        state.phase = GamePhase.OPERATIONS;
        CampaignOutcomeService.Evaluate(state);
        Assert.That(state.matchEnded, Is.True);
        Assert.That(state.campaignOutcome, Is.EqualTo(CampaignOutcomeService.Defeated));
    }

    [Test]
    public void TotalWipe_WithPersonalFortOnMap_IsNotDraw()
    {
        var state = StateWithPlanetSystem();
        state.phase = GamePhase.COMBAT;
        state.peakLegionCount = 3;
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_ai_1",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_b",
            playerOwned = false,
            legionId = "AI_ALPHA",
            status = BuildingService.Normal,
        });
        state.buildings.Add(new BuildingState
        {
            buildingId = "bld_pers",
            buildingType = BuildingService.PersonalFortress,
            solarSystemId = "sys_a",
            playerOwned = true,
            ownerMemberId = "m1",
            status = BuildingService.Normal,
        });
        foreach (var b in state.buildings.Where(b => BuildingService.LegionFortress.Equals(b.buildingType)).ToList())
        {
            BuildingService.DestroyBuilding(state, b.buildingId, ShipRegistry.LoadDefault());
        }
        CampaignOutcomeService.EvaluateMatchEnd(state);
        Assert.That(state.campaignOutcome, Is.Not.EqualTo(CampaignOutcomeService.Draw));
    }

    [Test]
    public void Depart_DestroyPersonalFortressesForIdentity()
    {
        var state = StateWithPlanetSystem();
        state.identities["id1"] = new IdentityState { identityCode = "id1", legionBelonging = -1 };
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        BuildingService.TryCreatePersonalFortress(state, state.members[0], "sys_a", new Random(4));
        Assert.That(state.buildings.Count(b => BuildingService.PersonalFortress.Equals(b.buildingType)), Is.EqualTo(1));
        LegionDepartureService.Depart(state, "id1", ShipRegistry.LoadDefault());
        Assert.That(state.members, Is.Empty);
        Assert.That(state.buildings.Count(b => BuildingService.PersonalFortress.Equals(b.buildingType)), Is.EqualTo(0));
    }

    [Test]
    public void CountMembersForMatchEnd_ExcludesCommander()
    {
        var state = new GameState
        {
            commanderIdentityCode = "id1",
            members =
            {
                new MemberState { memberId = "m1", identityCode = "id1", name = "Commander" },
            },
        };
        Assert.That(CampaignOutcomeService.CountMembersForMatchEnd(state), Is.EqualTo(0));
        Assert.That(state.members, Has.Count.EqualTo(1));
    }

    [Test]
    public void PersonalFortressIncome_CreditsOwnerEachRound()
    {
        var state = StateWithPlanetSystem();
        BuildingService.CreateLegionFortress(state, "sys_a", "er_planet_1");
        BuildingService.TryCreatePersonalFortress(state, state.members[0], "sys_a", new Random(5));
        var before = state.personalStockByGroup["mb_id1"][CurrencyIds.StarCoin];
        PersonalFortressIncomeService.SettleOperationPhase(state);
        Assert.That(
            state.personalStockByGroup["mb_id1"][CurrencyIds.StarCoin],
            Is.EqualTo(before + PersonalFortressIncomeService.CoinsPerRound));
    }
}
