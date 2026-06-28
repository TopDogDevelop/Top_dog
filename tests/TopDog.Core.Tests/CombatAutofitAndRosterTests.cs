using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

[TestFixture]
public sealed class CombatAutofitAndRosterTests
{
    [Test]
    public void AutoFit_UsesLegionStock_AfterCommanderMerge()
    {
        var state = new GameState { phase = GamePhase.COMBAT_PREP };
        state.legions.Add(new LegionState { legionId = CampaignLegionIds.Player, isLocal = true });
        var m = new MemberState
        {
            memberId = "1000000101",
            identityCode = "10000001",
            multiboxGroupId = "mb_10000001",
            equippedHullId = "hull_bc_spear",
            legionId = CampaignLegionIds.Player,
        };
        state.members.Add(m);
        state.commanderIdentityCode = "10000001";
        state.legions[0].legionStock["mod_hybrid_gun_m"] = 5;

        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        MemberDispatchAutoFitService.TryFillEmptySlots(state, m, ships, modules, new Random(1), allowOutsideOperations: true);

        Assert.That(MemberFittingService.Fittings(state, m).Count, Is.GreaterThan(0));
        Assert.That(state.legions[0].legionStock.GetValueOrDefault("mod_hybrid_gun_m"), Is.LessThan(5));
    }

    [Test]
    public void BuildingAssault_DefenderSide_IncludesAllLegionMembersInSystem()
    {
        var state = new GameState();
        var localId = "legion-local";
        var aiId = CampaignLegionIds.Ai;
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        state.legions.Add(new LegionState { legionId = aiId, isAiControlled = true });
        var sys = "sys_hub";
        state.buildings.Add(new BuildingState
        {
            buildingId = "b1",
            solarSystemId = sys,
            legionId = localId,
            displayName = "Test Fort",
            status = BuildingService.Normal,
        });
        state.members.Add(new MemberState
        {
            memberId = "m1",
            legionId = localId,
            currentSolarSystemId = sys,
            equippedHullId = "hull_bc_spear",
        });
        state.members.Add(new MemberState
        {
            memberId = "m2",
            legionId = localId,
            currentSolarSystemId = sys,
            equippedHullId = "hull_bc_spear",
        });
        state.members.Add(new MemberState
        {
            memberId = "spy",
            legionId = localId,
            currentSolarSystemId = sys,
            rosterVisibility = MemberRosterVisibility.Infiltrating,
            equippedHullId = "hull_bc_spear",
        });
        state.members.Add(new MemberState
        {
            memberId = "away",
            legionId = localId,
            currentSolarSystemId = "sys_other",
            equippedHullId = "hull_bc_spear",
        });

        var entry = CombatQueueCompiler.BuildBuildingAssault(
            state,
            state.buildings[0],
            ShipRegistry.LoadDefault(),
            ModuleRegistry.LoadDefault(),
            new Random(1),
            aiAttacker: true,
            aiId);

        Assert.That(entry.friendlyMemberIds, Is.EquivalentTo(new[] { "m1", "m2", "away" }));
        Assert.That(entry.enemyRoster.Any(l => l.memberId != null), Is.False);
    }

    [Test]
    public void BuildingAssault_PlayerAttack_IncludesDefendersOutsideBuildingSystem()
    {
        var state = new GameState();
        var localId = "legion-local";
        var aiId = "legion-ai";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        state.legions.Add(new LegionState { legionId = aiId, isAiControlled = true });
        var fortSystem = "sys_fort";
        state.buildings.Add(new BuildingState
        {
            buildingId = "b1",
            solarSystemId = fortSystem,
            legionId = aiId,
            displayName = "AI Fort",
            status = BuildingService.Normal,
        });
        LegionPlayerRegistry.AddMemberToLegion(state, aiId, new MemberState
        {
            memberId = "def_home",
            equippedHullId = "hull_bc_spear",
            currentSolarSystemId = fortSystem,
        });
        LegionPlayerRegistry.AddMemberToLegion(state, aiId, new MemberState
        {
            memberId = "def_away",
            equippedHullId = "hull_bc_spear",
            currentSolarSystemId = "sys_mining",
        });
        LegionPlayerRegistry.AddMemberToLegion(state, localId, new MemberState
        {
            memberId = "atk1",
            equippedHullId = "hull_bc_spear",
            currentSolarSystemId = fortSystem,
        });

        var entry = CombatQueueCompiler.BuildBuildingAssault(
            state,
            state.buildings[0],
            ShipRegistry.LoadDefault(),
            ModuleRegistry.LoadDefault(),
            new Random(1),
            aiAttacker: false,
            localId);

        Assert.That(entry.enemyRoster.Select(l => l.memberId), Is.EquivalentTo(new[] { "def_home", "def_away" }));
    }

    [Test]
    public void EnterCombatPrep_ManyFriendlyMembers_FillsEmptySlotsWithoutClearing()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        var localId = "legion-local";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true, legionStock = { ["mod_hybrid_gun_m"] = 200 } });
        var sys = "sys_hub";
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();

        for (var i = 0; i < 40; i++)
        {
            state.members.Add(new MemberState
            {
                memberId = "m" + i,
                legionId = localId,
                currentSolarSystemId = sys,
                equippedHullId = "hull_bc_spear",
            });
        }

        var entry = CombatQueueCompiler.BuildBuildingAssault(
            state,
            new BuildingState
            {
                buildingId = "b1",
                solarSystemId = sys,
                legionId = localId,
                displayName = "Fort",
                status = BuildingService.Normal,
            },
            ships,
            modules,
            new Random(1),
            aiAttacker: true,
            CampaignLegionIds.Ai);

        state.combatQueue.Add(entry);
        Assert.That(entry.friendlyMemberIds.Count, Is.EqualTo(40));

        Assert.DoesNotThrow(() => CombatPhaseService.EnterCombatPrep(state, ships, modules));
        Assert.That(state.phase, Is.EqualTo(GamePhase.COMBAT_PREP));
        Assert.That(entry.friendlyRosterLines.Count, Is.EqualTo(40));
        Assert.That(entry.friendlyRosterLines.Count(l => l.canParticipate), Is.EqualTo(40));
    }

    [Test]
    public void Appoint_MergesAllMultiboxPersonalGroups()
    {
        var state = new GameState();
        state.members.Add(new MemberState
        {
            memberId = "1000000101",
            identityCode = "10000001",
            multiboxGroupId = "mb_10000001",
            name = "A",
        });
        state.members.Add(new MemberState
        {
            memberId = "1000000102",
            identityCode = "10000001",
            multiboxGroupId = "mb_alt",
            name = "B",
        });
        MemberAssetService.PersonalStock(state, state.members[0]).AddQty("mod_hybrid_gun_m", 2);
        MemberAssetService.PersonalStock(state, state.members[1]).AddQty("mod_propulsion_m", 3);

        var msg = LegionCommanderService.Appoint(state, "1000000102");
        Assert.That(msg, Does.Contain("任命"));
        Assert.That(state.commanderIdentityCode, Is.EqualTo("10000001"));
        Assert.That(state.legionStock.GetValueOrDefault("mod_hybrid_gun_m"), Is.EqualTo(2));
        Assert.That(state.legionStock.GetValueOrDefault("mod_propulsion_m"), Is.EqualTo(3));
    }

    [Test]
    public void BuildingAssault_SpawnsDefendersFromLegionBucket()
    {
        var state = new GameState();
        var aiId = "legion-ai-uuid";
        state.legions.Add(new LegionState { legionId = aiId, isAiControlled = true, legionStock = { ["hull_bc_spear"] = 5 } });
        state.buildings.Add(new BuildingState
        {
            buildingId = "b1",
            solarSystemId = "sys_fort",
            legionId = aiId,
            displayName = "AI Fort",
            status = BuildingService.Normal,
        });
        LegionPlayerRegistry.AddMemberToLegion(state, aiId, new MemberState
        {
            memberId = "def1",
            name = "Defender",
            equippedHullId = "hull_bc_spear",
        });
        state.members.Clear();

        var entry = CombatQueueCompiler.BuildBuildingAssault(
            state, state.buildings[0], ShipRegistry.LoadDefault(), ModuleRegistry.LoadDefault(),
            new Random(1), aiAttacker: false, "legion-local");
        entry.friendlyMemberIds.Add("atk");
        CombatRosterPrepService.PrepareEntry(
            state, entry, ShipRegistry.LoadDefault(), ModuleRegistry.LoadDefault(), new Random(1));
        var spawned = BattlefieldSpawner.SpawnAll(
            state, entry, ShipRegistry.LoadDefault(), ModuleRegistry.LoadDefault(), new Random(1));
        Assert.That(spawned, Has.Count.EqualTo(1));
        var enemies = spawned[0].units.Where(u => u.side == UnitSide.ENEMY && !u.isBuilding).ToList();
        Assert.That(enemies, Has.Count.EqualTo(1));
        Assert.That(enemies[0].memberId, Is.EqualTo("def1"));
    }

    [Test]
    public void HybridGunXl_ValuationOneBelowIronCoffin()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var xl = modules.Resolve("mod_hybrid_gun_xl");
        var dread = ships.FindHull("hull_dread_ironcoffin");
        Assert.That(xl, Is.Not.Null);
        Assert.That(AssetValuation.ModuleStarCoinValue(xl), Is.EqualTo(49_999));
        Assert.That(AssetValuation.HullStarCoinValue(dread), Is.EqualTo(50_000));
        Assert.That(AssetValuation.ModuleStarCoinValue(modules.Resolve("mod_hybrid_gun_l")), Is.EqualTo(6_000));
    }
}
