using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.Ship;
using TopDog.Sim.Skirmish;
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
        var wolfHull = ships.FindHull("hull_frigate_shortlegwolf");
        Assert.That(wolfHull, Is.Not.Null);
        var wolfStructure = wolfHull!.structureHp;
        var victim = new BattlefieldUnit
        {
            unitId = "u-victim",
            hullId = "hull_frigate_shortlegwolf",
            tonnageClass = "FRIGATE",
            side = UnitSide.ENEMY,
            arrivalAtSec = 0f,
            structureHp = wolfStructure,
            structureMax = wolfStructure,
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
        Assert.That(attacker.structureHp, Is.EqualTo(wolfStructure));
        Assert.That(attacker.combatSeizedHullThisLife, Is.True);
    }

    [Test]
    public void Combat_BoardingModule_VictimPermadeadForMatch()
    {
        var state = new GameState
        {
            phase = GamePhase.COMBAT,
            combatRealtimeActive = true,
        };
        var ships = ShipRegistry.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        var bf = new BattlefieldState { battlefieldId = "bf-perma", timeSec = 0f };
        var attacker = new BattlefieldUnit
        {
            unitId = "u-leech",
            memberId = "m-attacker",
            hullId = "hull_frigate_leech_landing",
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
            memberId = "m-victim",
            hullId = "hull_frigate_shortlegwolf",
            side = UnitSide.ENEMY,
            arrivalAtSec = 0f,
            structureHp = ships.FindHull("hull_frigate_shortlegwolf")!.structureHp,
            structureMax = ships.FindHull("hull_frigate_shortlegwolf")!.structureHp,
        };
        bf.units.Add(attacker);
        bf.units.Add(victim);
        state.battlefields.Add(bf);

        var holdSec = modules.Resolve("mod_boarding_s")!.boardingHoldSec;
        for (var i = 0; i < (int)Math.Ceiling(holdSec + 1f); i++)
        {
            BoardingModuleService.Tick(state, bf, modules, ships, 1f);
        }

        Assert.That(state.boardingPermadeadMemberIds, Does.Contain("m-victim"));
    }

    [Test]
    public void Skirmish_Respawn_RevertsSeizedHullToMatchBaseline()
    {
        var state = new GameState
        {
            phase = GamePhase.COMBAT,
            combatRealtimeActive = true,
            skirmish = new SkirmishMatchState { elapsedSec = 0f },
        };
        state.worldline.type = WorldlineType.LEGION_SKIRMISH;
        var member = new MemberState
        {
            memberId = "m-leech",
            legionId = "legion-a",
            name = "Pilot",
            equippedHullId = "hull_frigate_leech_landing",
        };
        state.members.Add(member);
        MatchMemberBaselineService.EnsureSnapshot(state);

        member.equippedHullId = "hull_frigate_shortlegwolf";
        var unit = new BattlefieldUnit
        {
            unitId = "u-dead",
            memberId = "m-leech",
            legionId = "legion-a",
            hullId = "hull_frigate_shortlegwolf",
            tonnageClass = "FRIGATE",
            combatSeizedHullThisLife = true,
        };
        SkirmishRespawnService.QueueRespawn(state, unit);

        Assert.That(state.skirmish.respawnQueue, Has.Count.EqualTo(1));
        Assert.That(state.skirmish.respawnQueue[0].hullId, Is.EqualTo("hull_frigate_leech_landing"));
        Assert.That(state.skirmish.respawnQueue[0].respawnAtSec, Is.EqualTo(300f));
        Assert.That(state.alertLog.Any(m => m.Contains("还有 5 分钟重生")), Is.True);
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
        var wolfStructure = ships.FindHull("hull_frigate_shortlegwolf")!.structureHp;
        var victim = new BattlefieldUnit
        {
            unitId = "u-b",
            hullId = "hull_frigate_shortlegwolf",
            side = UnitSide.ENEMY,
            arrivalAtSec = 0f,
            structureHp = wolfStructure,
            structureMax = wolfStructure,
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
