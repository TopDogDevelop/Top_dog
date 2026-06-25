using TopDog.Content.Assets;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
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
        state.buildings.Add(new BuildingState
        {
            buildingId = "b1",
            buildingType = BuildingService.LegionFortress,
            solarSystemId = "sys_a",
            playerOwned = true,
            legionId = CampaignLegionIds.Player,
            eventRegionId = "er_p1",
            status = BuildingService.Normal,
        });
        var msg = PlayerBuildingAssaultService.QueueAssaultOnSystem(state, "sys_a");
        Assert.That(msg, Is.EqualTo("该星系无敌方建筑可约战"));
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
