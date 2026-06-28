using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MissileSpawnTests
{
    [Test]
    public void ShipWithMissileModule_SpawnsMissileEntity()
    {
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        var ship = new BattlefieldUnit
        {
            unitId = "bc-1",
            tonnageClass = "BATTLECRUISER",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            alive = true,
            x = 50f,
            y = 80f,
            fittedModules = { ["tube_l1"] = "mod_chaos_missile_l" },
        };
        bf.units.Add(ship);

        var modules = ModuleRegistry.LoadDefault();
        MissileSpawnService.ExpandLauncherMissiles(bf, ship, modules, new Random(7));

        var missiles = bf.units.Where(u => "MISSILE".Equals(u.tonnageClass, StringComparison.Ordinal)).ToList();
        Assert.That(missiles, Has.Count.EqualTo(1));
        Assert.That(missiles[0].parentUnitId, Is.EqualTo("bc-1"));
        Assert.That(missiles[0].IsBallisticMissile(), Is.True);
        Assert.That(missiles[0].salvoRoundDmg, Is.EqualTo(0f));
        Assert.That(missiles[0].structureHp, Is.EqualTo(1000f).Within(0.01f));
    }

    [Test]
    public void MissileModule_DoesNotAddSalvoToParentShip()
    {
        var ship = new BattlefieldUnit
        {
            unitId = "bc-2",
            fittedModules = { ["tube_l1"] = "mod_chaos_missile_l", ["gun_l1"] = "mod_hybrid_gun_l" },
        };
        var modules = ModuleRegistry.LoadDefault();
        var hull = new HullDef { tonnageClass = "BATTLECRUISER" };

        SalvoProfileService.ApplyToUnit(ship, hull, modules);

        Assert.That(ship.salvoRoundDmg, Is.EqualTo(3000f).Within(0.01f));
    }
}
