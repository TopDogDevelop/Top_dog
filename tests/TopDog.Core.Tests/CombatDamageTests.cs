using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatDamageTests
{
    [Test]
    public void ModuleRuntime_WithGun_IncreasesDps()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var hull = ships.FindHull("hull_bc_spear");
        Assert.That(hull, Is.Not.Null);

        var bare = new BattlefieldUnit
        {
            fittedModules = new Dictionary<string, string>(),
        };
        ModuleRuntime.ApplyToUnit(bare, hull!, modules);
        Assert.That(bare.salvoRoundDmg, Is.EqualTo(0f));
        var bareDps = bare.damagePerSec;

        var armed = new BattlefieldUnit
        {
            fittedModules = { ["attack_m1"] = "mod_hybrid_gun_m" },
        };
        ModuleRuntime.ApplyToUnit(armed, hull!, modules);

        Assert.That(armed.damagePerSec, Is.GreaterThan(bareDps));
        Assert.That(armed.attackRangeM, Is.GreaterThanOrEqualTo(8000f));
    }

    [Test]
    public void MoveAndFire_WithinRange_AppliesDamage()
    {
        var state = new GameState { combatRealtimeActive = true, autoFireEnabled = true };
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        state.battlefields.Add(bf);
        state.activeBattlefieldId = bf.battlefieldId;

        var attacker = new BattlefieldUnit
        {
            unitId = "atk",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = 0f,
            y = 0f,
            targetUnitId = "def",
            salvoRoundDmg = 100f,
            fireCycleSec = 10f,
            attackRangeM = 10_000f,
            fireCooldownSec = 0f,
            structureHp = 100f,
            structureMax = 100f,
        };
        var defender = new BattlefieldUnit
        {
            unitId = "def",
            side = UnitSide.ENEMY,
            alive = true,
            arrivalAtSec = 0f,
            x = 500f,
            y = 0f,
            structureHp = 200f,
            structureMax = 200f,
            shieldHp = 0f,
            armorHp = 0f,
        };
        bf.units.Add(attacker);
        bf.units.Add(defender);

        var hpBefore = defender.structureHp;
        BattlefieldSystem.Tick(state, 1f);
        Assert.That(defender.structureHp, Is.LessThan(hpBefore));
    }
}
