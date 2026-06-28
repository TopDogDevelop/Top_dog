using TopDog.Content.Modules;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class MissileLaunchTests
{
    [Test]
    public void LaunchRequiresValidEnemyTarget()
    {
        var state = new GameState { autoFireEnabled = true };
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 1f };
        var launcher = new BattlefieldUnit
        {
            unitId = "bc-1",
            tonnageClass = "BATTLECRUISER",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            alive = true,
            x = 0f,
            y = 0f,
            attackRangeM = 50_000f,
            fireCycleSec = 4f,
            fittedModules = { ["tube_l1"] = "mod_chaos_missile_l" },
        };
        bf.units.Add(launcher);
        var modules = ModuleRegistry.LoadDefault();

        MissileLaunchService.TryLaunch(state, bf, launcher, modules, new Random(3), 0.1f);
        Assert.That(bf.units.Count(u => u.IsBallisticMissile()), Is.EqualTo(0));

        var enemy = new BattlefieldUnit
        {
            unitId = "e1",
            side = UnitSide.ENEMY,
            tonnageClass = "CRUISER",
            arrivalAtSec = 0f,
            alive = true,
            x = 5000f,
            y = 0f,
        };
        bf.units.Add(enemy);
        launcher.targetUnitId = "e1";

        MissileLaunchService.TryLaunch(state, bf, launcher, modules, new Random(3), 0.1f);
        Assert.That(bf.units.Count(u => u.IsBallisticMissile()), Is.EqualTo(1));
        Assert.That(bf.units.First(u => u.IsBallisticMissile()).targetUnitId, Is.EqualTo("e1"));
    }
}
