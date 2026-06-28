using TopDog.Content.Modules;
using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class StrikeWingRecallTests
{
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
    public void EngagedCarrier_RedeploysStrikeCraft()
    {
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 1f };
        var carrier = new BattlefieldUnit
        {
            unitId = "c1",
            tonnageClass = "CARRIER",
            side = UnitSide.FRIENDLY,
            aiOrder = UnitAiOrder.FOCUS,
            explicitFocus = true,
            arrivalAtSec = 0f,
            fittedModules = new Dictionary<string, string> { { "wing", "mod_strike_wing" } },
        };
        bf.units.Add(carrier);

        StrikeWingRecallService.Tick(bf, modules, new Random(2));

        Assert.That(bf.units.Exists(u =>
            "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)
            && "c1".Equals(u.parentUnitId, StringComparison.Ordinal)), Is.True);
    }
}
