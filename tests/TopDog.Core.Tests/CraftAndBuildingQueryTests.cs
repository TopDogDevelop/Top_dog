using TopDog.Content.Map;
using TopDog.Content.Assets;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Economy;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class BuildingQueryServiceTests
{
    [Test]
    public void Summarize_EmptySystem_HasNoBuildings()
    {
        var state = new GameState();
        var summary = BuildingQueryService.SummarizeSystemBuildings(state, "sys_a");
        Assert.That(summary.AnyBuilding, Is.False);
    }

    [Test]
    public void QueueAssault_NoBuildings_ReturnsNoBuildingMessage()
    {
        var state = new GameState();
        var msg = PlayerBuildingAssaultService.QueueAssaultOnSystem(state, "sys_a");
        Assert.That(msg, Is.EqualTo("该星系无建筑"));
    }

    [Test]
    public void QueueAssault_OnlyPlayerFort_ReturnsNoEnemyMessage()
    {
        var state = new GameState();
        var localId = "host-uuid";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        state.buildings.Add(new BuildingState
        {
            buildingId = "b1",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_a",
            playerOwned = true,
            legionId = localId,
            eventRegionId = "er_p1",
            status = BuildingService.Normal,
        });
        var msg = PlayerBuildingAssaultService.QueueAssaultOnSystem(state, "sys_a", localId);
        Assert.That(msg, Is.EqualTo("该星系无敌方建筑可约战"));
    }

    [Test]
    public void QueueAssault_EnemyFort_CompilesToCombatQueue()
    {
        var state = new GameState();
        state.map = new LoadedMap(new MapProject
        {
            systems =
            {
                new SolarSystemDef { solarSystemId = "sys_a", name = "Test" },
            },
        }, null);
        var localId = "host-uuid";
        var aiId = "ai-uuid";
        state.legions.Add(new LegionState { legionId = localId, isLocal = true });
        state.legions.Add(new LegionState { legionId = aiId, isAiControlled = true });
        state.buildings.Add(new BuildingState
        {
            buildingId = "b_enemy",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_a",
            playerOwned = false,
            legionId = aiId,
            status = BuildingService.Normal,
        });
        LegionPlayerRegistry.AddMemberToLegion(state, localId, new MemberState
        {
            memberId = "m1",
            legionId = localId,
            isPlayer = true,
            currentSolarSystemId = "sys_a",
        });

        var msg = PlayerBuildingAssaultService.QueueAssaultOnSystem(state, "sys_a", localId);
        Assert.That(msg, Does.Contain("已发起"));
        Assert.That(state.playerPendingAssaults, Has.Count.EqualTo(1));

        CombatQueueCompiler.Compile(state, ShipRegistry.LoadDefault(), ModuleRegistry.LoadDefault());

        Assert.That(state.combatQueue, Has.Count.EqualTo(1));
        Assert.That(state.combatQueue[0].combatSubtype, Is.EqualTo(CombatSubtype.BUILDING_ASSAULT));
        Assert.That(state.combatQueue[0].attackerLegionId, Is.EqualTo(localId));
        Assert.That(state.combatQueue[0].defenderLegionId, Is.EqualTo(aiId));
        Assert.That(state.playerPendingAssaults, Is.Empty);
    }
}

public sealed class CraftServiceTests
{
    [SetUp]
    public void SetUp() => Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

    [Test]
    public void CraftHull_DebitsInorganicAndAddsHull()
    {
        var state = new GameState();
        StartingAssetLoader.ApplyToState(state, "assets_default");
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var before = state.legionStock.GetValueOrDefault(ResourceIds.Inorganic, 0);
        var msg = CraftService.TryCraftHull(state, "hull_bc_spear", ships, modules);
        Assert.That(msg, Does.Contain("制造完成"));
        Assert.That(state.legionStock.GetValueOrDefault("hull_bc_spear"), Is.GreaterThan(2));
        Assert.That(state.legionStock.GetValueOrDefault(ResourceIds.Inorganic), Is.LessThan(before));
    }
}
