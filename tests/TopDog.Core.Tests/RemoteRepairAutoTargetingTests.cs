using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class RemoteRepairAutoTargetingTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void IdleHealer_AutoRepairsNearestFieldHolder()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var state = new GameState();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 10f };
        state.battlefields.Add(bf);

        var healer = new BattlefieldUnit
        {
            unitId = "healer",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = 0f,
            aiOrder = UnitAiOrder.IDLE,
            pendingRepairRounds = 0,
            fittedModules = { ["fn_1"] = "mod_remote_shield_repair_s" },
        };
        var nearHolder = new BattlefieldUnit
        {
            unitId = "near",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = 1_000f,
            shieldHp = 50f,
            shieldMax = 100f,
            fieldAuraEnabledAtSec = 1f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        var farHolder = new BattlefieldUnit
        {
            unitId = "far",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = 4_000f,
            shieldHp = 50f,
            shieldMax = 100f,
            fieldAuraEnabledAtSec = 1f,
            fittedModules = { ["fn_1"] = "mod_shield_fusion_s" },
        };
        bf.units.Add(healer);
        bf.units.Add(nearHolder);
        bf.units.Add(farHolder);
        ModuleRuntime.ApplyToUnit(healer, ships.FindHull("hull_frigate_pineapple")!, modules);

        RemoteRepairAutoTargetingService.Tick(bf, healer, modules);

        Assert.That(healer.remoteRepairAutoActive, Is.True);
        Assert.That(healer.targetUnitId, Is.EqualTo("near"));
        Assert.That(healer.pendingRepairRounds, Is.EqualTo(1));

        for (var i = 0; i < 3; i++)
        {
            RemoteRepairAutoTargetingService.Tick(bf, healer, modules);
            RemoteRepairSalvoService.Tick(state, bf, modules, ships, 3f);
        }

        Assert.That(nearHolder.shieldHp, Is.GreaterThan(50f),
            "Auto repair should heal nearest field holder without player OrderRepairTarget");
        Assert.That(farHolder.shieldHp, Is.EqualTo(50f).Within(0.01f));
    }

    [Test]
    public void FieldHolder_WithoutFieldActive_IsNotAutoRepaired()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var state = new GameState();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 10f };
        state.battlefields.Add(bf);

        var healer = new BattlefieldUnit
        {
            unitId = "healer",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = 0f,
            aiOrder = UnitAiOrder.IDLE,
            fittedModules = { ["fn_1"] = "mod_remote_shield_repair_s" },
        };
        var holder = new BattlefieldUnit
        {
            unitId = "holder",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = 500f,
            shieldHp = 50f,
            shieldMax = 100f,
            fieldAuraEnabledAtSec = 0f,
            fittedModules = { ["fn_4"] = "mod_shield_fusion_l" },
        };
        bf.units.Add(healer);
        bf.units.Add(holder);

        for (var i = 0; i < 5; i++)
        {
            RemoteRepairAutoTargetingService.Tick(bf, healer, modules);
            RemoteRepairSalvoService.Tick(state, bf, modules, ships, 2f);
        }

        Assert.That(healer.remoteRepairAutoActive, Is.False);
        Assert.That(holder.shieldHp, Is.EqualTo(50f).Within(0.01f));
    }

    [Test]
    public void PlayerOrder_SuppressesAutoRepair()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { timeSec = 5f };
        var healer = new BattlefieldUnit
        {
            unitId = "healer",
            side = UnitSide.FRIENDLY,
            remoteRepairAutoActive = true,
            targetUnitId = "field",
            pendingRepairRounds = 1,
            aiOrder = UnitAiOrder.IDLE,
            fittedModules = { ["fn_1"] = "mod_remote_shield_repair_s" },
        };
        bf.units.Add(healer);

        RemoteRepairAutoTargetingService.SuppressForPlayerOrder(healer);
        healer.aiOrder = UnitAiOrder.STOP;

        RemoteRepairAutoTargetingService.Tick(bf, healer, modules);

        Assert.That(healer.remoteRepairAutoActive, Is.False);
    }

    [Test]
    public void LogisticsProducer_ApproachesFieldHolder_ButDoesNotRepairHp()
    {
        var modules = FieldNavTestContent.LoadModules();
        var bf = new BattlefieldState { timeSec = 10f };
        var producer = new BattlefieldUnit
        {
            unitId = "logi",
            side = UnitSide.FRIENDLY,
            x = 0f,
            fittedModules = { ["fn_1"] = "mod_strike_assembly_l" },
            aiOrder = UnitAiOrder.IDLE,
        };
        var holder = new BattlefieldUnit
        {
            unitId = "field",
            side = UnitSide.FRIENDLY,
            x = 5_000f,
            fieldAuraEnabledAtSec = 1f,
            fieldAuraArmorDominant = true,
            armorHp = 100f,
            armorMax = 1000f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        bf.units.Add(producer);
        bf.units.Add(holder);

        LogisticsAutoTargetingService.Tick(bf, producer, modules);
        LogisticsProducerService.Tick(bf, modules, 16f);

        Assert.That(producer.logisticsAutoAimActive, Is.True);
        Assert.That(holder.armorHp, Is.EqualTo(100f).Within(0.01f),
            "Producer auto-approach resets tubes in radius; it does not repair hull HP");
    }
}
