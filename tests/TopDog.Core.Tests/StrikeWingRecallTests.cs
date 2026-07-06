using TopDog.Content.Modules;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class StrikeWingRecallTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();
    [Test]
    public void IdleCarrier_RecallsStrikeCraft()
    {
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 1f };
        var carrier = new BattlefieldUnit
        {
            unitId = "c1",
            tonnageClass = "CARRIER",
            side = UnitSide.FRIENDLY,
            aiOrder = UnitAiOrder.IDLE,
            arrivalAtSec = 0f,
        };
        bf.units.Add(carrier);
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "w1",
            parentUnitId = "c1",
            tonnageClass = "STRIKE_CRAFT",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
        });

        StrikeWingRecallService.Tick(bf, ModuleRegistry.LoadDefault(), new Random(1));

        Assert.That(bf.units.Exists(u => u.unitId == "w1"), Is.False);
    }

    [Test]
    public void FocusCommand_DeploysStrikeCraft()
    {
        var modules = ModuleRegistry.LoadDefault();
        var modId = "mod_strike_wing_a_l";
        Assert.That(modules.Resolve(modId), Is.Not.Null);

        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 1f };
        var carrier = new BattlefieldUnit
        {
            unitId = "c1",
            tonnageClass = "CARRIER",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            fittedModules = new Dictionary<string, string> { { "tube_1", modId } },
        };
        LaunchTubeStateService.InitTubeStates(carrier, modules);
        bf.units.Add(carrier);
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "e1",
            side = UnitSide.ENEMY,
            alive = true,
            arrivalAtSec = 0f,
        });

        StrikeWingSpawnService.DeployForFocusCommand(bf, new[] { "c1" }, modules, new Random(2));

        Assert.That(bf.units.Exists(u =>
            "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
            && "c1".Equals(u.parentUnitId, StringComparison.Ordinal)), Is.True);
        Assert.That(carrier.tubeStates["tube_1"], Is.EqualTo(LaunchTubeState.Activated));
    }

    [Test]
    public void EngagedWithoutFocus_DoesNotAutoDeploy()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 1f };
        var carrier = new BattlefieldUnit
        {
            unitId = "c1",
            tonnageClass = "CARRIER",
            side = UnitSide.FRIENDLY,
            aiOrder = UnitAiOrder.APPROACH,
            targetUnitId = "e1",
            arrivalAtSec = 0f,
            fittedModules = new Dictionary<string, string> { { "tube_1", "mod_strike_wing_a_l" } },
        };
        bf.units.Add(carrier);

        StrikeWingRecallService.Tick(bf, modules, new Random(2));

        Assert.That(bf.units.Exists(u =>
            "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)), Is.False);
    }
}
