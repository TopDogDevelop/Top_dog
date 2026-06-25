using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Operations;
using TopDog.Sim.Ship;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MiningSettlementTests
{
    [Test]
    public void SettleOperationPhase_GrantsInorganicToMiningMember()
    {
        var state = new GameState { phase = GamePhase.OPERATIONS };
        MapTestFixtures.AttachMineSystem(state);
        var m = new MemberState
        {
            memberId = "1000000101",
            name = "Miner",
            equippedHullId = "hull_carrier_stellar_collector",
            assignedTask = MemberDispatchService.TaskMining,
            currentSolarSystemId = "sys_mine",
            opsDeploySystemId = "sys_mine",
            opsDeployEventRegionId = "er_sys_mine_belt",
            playerDispatchActive = true,
            playerChoseDeployRegion = true,
        };
        state.members.Add(m);
        MemberAssetService.PersonalStock(state, m).AddQty("mod_ore_mining_beam_s", 2);
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var hull = ships.FindHull("hull_carrier_stellar_collector");
        MemberFittingService.EquipModule(state, m, "atk_0", "mod_ore_mining_beam_s", null, hull, modules);
        MemberFittingService.EquipModule(state, m, "atk_1", "mod_ore_mining_beam_s", null, hull, modules);

        MiningSettlementService.SettleOperationPhase(state, ships, modules);

        Assert.That(state.legionStock.GetValueOrDefault(ResourceIds.Inorganic, 0), Is.EqualTo(1000));
        Assert.That(MemberAssetService.PersonalQty(state, m, ResourceIds.Inorganic), Is.EqualTo(0));
    }

    [Test]
    public void ComputeYield_CappedBySimultaneousEnableLimit()
    {
        var hull = new HullDef
        {
            attackSlots = 10,
            defaultSlotSize = ModuleSize.Small,
            simultaneousEnableLimit = 3,
        };
        var modules = ModuleRegistry.LoadDefault();
        var fit = new Dictionary<string, string>();
        for (var i = 0; i < 5; i++)
        {
            fit["atk_" + i] = "mod_ore_mining_beam_s";
        }

        var result = MiningYieldCalculator.Compute(hull, fit, modules);

        Assert.That(result.FittedMinerCount, Is.EqualTo(5));
        Assert.That(result.SimultaneousEnableCap, Is.EqualTo(3));
        Assert.That(result.ActiveMinerCount, Is.EqualTo(3));
        Assert.That(result.TotalYield, Is.EqualTo(1500));
    }

    [Test]
    public void ComputeYield_ScalesWithPerModuleYield()
    {
        var hull = new HullDef { attackSlots = 2, defaultSlotSize = ModuleSize.Small, simultaneousEnableLimit = 2 };
        var modules = ModuleRegistry.LoadDefault();
        var fit = new Dictionary<string, string>
        {
            ["atk_0"] = "mod_ore_mining_beam_s",
            ["atk_1"] = "mod_ore_mining_beam_s",
        };

        var result = MiningYieldCalculator.Compute(hull, fit, modules);

        Assert.That(result.ActiveMinerCount, Is.EqualTo(2));
        Assert.That(result.TotalYield, Is.EqualTo(1000));
    }

    [Test]
    public void CollectorHull_EnableCapMatchesSimultaneousLimit()
    {
        var ships = ShipRegistry.LoadDefault();
        var hull = ships.FindHull("hull_carrier_stellar_collector");
        Assert.That(hull, Is.Not.Null);
        var modules = ModuleRegistry.LoadDefault();
        var fit = new Dictionary<string, string>();
        for (var i = 0; i < 45; i++)
        {
            fit["atk_" + i] = "mod_ore_mining_beam_s";
        }

        var result = MiningYieldCalculator.Compute(hull, fit, modules);

        Assert.That(result.FittedMinerCount, Is.EqualTo(45));
        Assert.That(result.SimultaneousEnableCap, Is.EqualTo(45));
        Assert.That(result.ActiveMinerCount, Is.EqualTo(45));
        Assert.That(result.TotalYield, Is.EqualTo(45 * 500));
    }
}
