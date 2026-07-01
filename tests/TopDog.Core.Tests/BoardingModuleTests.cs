using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.Ship;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class BoardingModuleTests
{
    [Test]
    public void Fitting_LeechHull_AllowsBoardingModule_PineappleDoesNot()
    {
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var boarding = modules.Resolve("mod_boarding_s");
        var leech = ships.FindHull("hull_frigate_leech_landing");
        var pineapple = ships.FindHull("hull_frigate_pineapple");
        Assert.That(boarding, Is.Not.Null);
        Assert.That(leech, Is.Not.Null);
        Assert.That(pineapple, Is.Not.Null);

        Assert.That(
            FittingValidator.ModuleFitsSlot("fn_0", boarding, leech),
            Is.True);
        Assert.That(
            FittingValidator.ModuleFitsSlot("fn_0", boarding, pineapple),
            Is.False);
    }

    [Test]
    public void Combat_BoardingModule_SeizesEnemyHullAfterHold()
    {
        var state = new GameState
        {
            phase = GamePhase.COMBAT,
            combatRealtimeActive = true,
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var boarding = modules.Resolve("mod_boarding_s");
        Assert.That(boarding, Is.Not.Null);

        var bf = new BattlefieldState
        {
            battlefieldId = "bf-board",
            timeSec = 0f,
        };
        var attacker = new BattlefieldUnit
        {
            unitId = "u-leech",
            hullId = "hull_frigate_leech_landing",
            tonnageClass = "FRIGATE",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            structureHp = 500f,
            structureMax = 500f,
            targetUnitId = "u-victim",
            fittedModules = { ["fn_0"] = "mod_boarding_s" },
        };
        var victim = new BattlefieldUnit
        {
            unitId = "u-victim",
            hullId = "hull_frigate_shortlegwolf",
            tonnageClass = "FRIGATE",
            side = UnitSide.ENEMY,
            arrivalAtSec = 0f,
            structureHp = 100f,
            structureMax = 100f,
            fittedModules = { ["atk_0"] = "mod_hybrid_gun_xl" },
        };
        bf.units.Add(attacker);
        bf.units.Add(victim);
        state.battlefields.Add(bf);

        var holdSec = boarding!.boardingHoldSec > 0f ? boarding.boardingHoldSec : 100f;
        var ticks = (int)Math.Ceiling(holdSec + 2f);
        for (var i = 0; i < ticks; i++)
        {
            BoardingModuleService.Tick(state, bf, modules, ships, 1f);
        }

        Assert.That(victim.IsDestroyed(), Is.True);
        Assert.That(attacker.hullId, Is.EqualTo("hull_frigate_shortlegwolf"));
        Assert.That(attacker.fittedModules["atk_0"], Is.EqualTo("mod_hybrid_gun_xl"));
        Assert.That(attacker.structureHp, Is.EqualTo(100f));
    }

    [Test]
    public void Combat_BoardingChargeResetsWhenOutOfRange()
    {
        var state = new GameState { combatRealtimeActive = true };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf-range", timeSec = 0f };
        var attacker = new BattlefieldUnit
        {
            unitId = "u-a",
            hullId = "hull_frigate_leech_landing",
            side = UnitSide.FRIENDLY,
            arrivalAtSec = 0f,
            structureHp = 500f,
            structureMax = 500f,
            targetUnitId = "u-b",
            x = 0f,
            fittedModules = { ["fn_0"] = "mod_boarding_s" },
        };
        var victim = new BattlefieldUnit
        {
            unitId = "u-b",
            hullId = "hull_frigate_shortlegwolf",
            side = UnitSide.ENEMY,
            arrivalAtSec = 0f,
            structureHp = 100f,
            structureMax = 100f,
            x = 500f,
        };
        bf.units.Add(attacker);
        bf.units.Add(victim);
        state.battlefields.Add(bf);

        BoardingModuleService.Tick(state, bf, modules, ships, 50f);
        Assert.That(attacker.boardingChargeSec, Is.EqualTo(0f));
        Assert.That(victim.IsDestroyed(), Is.False);
    }
}
