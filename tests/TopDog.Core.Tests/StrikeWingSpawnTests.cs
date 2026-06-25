using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class StrikeWingSpawnTests
{
    [Test]
    public void CarrierWithStrikeWingModule_SpawnsStrikeCraft()
    {
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        var carrier = new BattlefieldUnit
        {
            unitId = "carrier-1",
            tonnageClass = "CARRIER",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            alive = true,
            x = 100f,
            y = 200f,
            fittedModules = { ["tube_l1"] = "mod_strike_wing_a_l" },
        };
        bf.units.Add(carrier);

        var modules = ModuleRegistry.LoadDefault();
        StrikeWingSpawnService.ExpandCarrierWings(bf, carrier, modules, new Random(3));

        var wings = bf.units.Where(u => "STRIKE_CRAFT".Equals(u.tonnageClass, StringComparison.Ordinal)).ToList();
        Assert.That(wings, Has.Count.GreaterThan(0));
        Assert.That(wings[0].parentUnitId, Is.EqualTo("carrier-1"));
    }
}
