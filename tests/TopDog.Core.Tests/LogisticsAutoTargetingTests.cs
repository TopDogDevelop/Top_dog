using TopDog.Content.Modules;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class LogisticsAutoTargetingTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();

    [Test]
    public void IdleProducer_ApproachesNearestFieldAlly()
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
        var fieldHolder = new BattlefieldUnit
        {
            unitId = "field",
            side = UnitSide.FRIENDLY,
            x = 50_000f,
            fieldAuraEnabledAtSec = 1f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        var farHolder = new BattlefieldUnit
        {
            unitId = "far",
            side = UnitSide.FRIENDLY,
            x = 200_000f,
            fieldAuraEnabledAtSec = 1f,
            fittedModules = { ["fn_1"] = "mod_shield_fusion_s" },
        };
        bf.units.Add(producer);
        bf.units.Add(fieldHolder);
        bf.units.Add(farHolder);

        LogisticsAutoTargetingService.Tick(bf, producer, modules);

        Assert.That(producer.logisticsAutoAimActive, Is.True);
        Assert.That(producer.aiOrder, Is.EqualTo(UnitAiOrder.APPROACH));
        Assert.That(producer.approachTargetUnitId, Is.EqualTo("field"));
        Assert.That(producer.commandMaintainDistM, Is.EqualTo(15_000f * 0.85f).Within(1f));
        Assert.That(producer.fieldAuraEnabledAtSec, Is.GreaterThan(0f));
    }

    [Test]
    public void PlayerStop_ClearsLogisticsAutoAim()
    {
        var modules = FieldNavTestContent.LoadModules();
        var bf = new BattlefieldState { timeSec = 5f };
        var producer = new BattlefieldUnit
        {
            unitId = "logi",
            side = UnitSide.FRIENDLY,
            fittedModules = { ["fn_1"] = "mod_drone_queen_l" },
            logisticsAutoAimActive = true,
            aiOrder = UnitAiOrder.APPROACH,
            approachTargetUnitId = "field",
        };
        bf.units.Add(producer);

        LogisticsAutoTargetingService.SuppressForPlayerOrder(producer);
        producer.aiOrder = UnitAiOrder.STOP;

        LogisticsAutoTargetingService.Tick(bf, producer, modules);

        Assert.That(producer.logisticsAutoAimActive, Is.False);
        Assert.That(producer.aiOrder, Is.EqualTo(UnitAiOrder.STOP));
    }

    [Test]
    public void NonProducer_DoesNotAutoAim()
    {
        var modules = FieldNavTestContent.LoadModules();
        var bf = new BattlefieldState();
        var fighter = new BattlefieldUnit
        {
            unitId = "fight",
            side = UnitSide.FRIENDLY,
            aiOrder = UnitAiOrder.IDLE,
        };
        var fieldHolder = new BattlefieldUnit
        {
            unitId = "field",
            side = UnitSide.FRIENDLY,
            x = 10_000f,
            fieldAuraEnabledAtSec = 1f,
            fittedModules = { ["fn_1"] = "mod_armor_link_s" },
        };
        bf.units.Add(fighter);
        bf.units.Add(fieldHolder);

        LogisticsAutoTargetingService.Tick(bf, fighter, modules);

        Assert.That(fighter.aiOrder, Is.EqualTo(UnitAiOrder.IDLE));
        Assert.That(fighter.logisticsAutoAimActive, Is.False);
    }
}
