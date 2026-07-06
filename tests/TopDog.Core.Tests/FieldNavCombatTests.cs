using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class RemoteRepairSalvoTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();
    [Test]
    public void OrderRepairTarget_IncrementsPendingRounds_Max20()
    {
        var state = new GameState();
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        state.battlefields.Add(bf);
        state.activeBattlefieldId = bf.battlefieldId;

        var healer = new BattlefieldUnit
        {
            unitId = "heal",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            fittedModules = { ["atk_1"] = "mod_remote_shield_repair_s" },
        };
        var target = new BattlefieldUnit
        {
            unitId = "tgt",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
        };
        bf.units.Add(healer);
        bf.units.Add(target);
        healer.pendingRepairRounds = 19;

        RemoteRepairSalvoService.OrderRepairTarget(state, bf, "tgt", new[] { "heal" });
        Assert.That(healer.pendingRepairRounds, Is.EqualTo(20));

        RemoteRepairSalvoService.OrderRepairTarget(state, bf, "tgt", new[] { "heal" });
        Assert.That(healer.pendingRepairRounds, Is.EqualTo(20));
    }

    [Test]
    public void OrderRepairTarget_SkipsUnitsWithoutRepairModule()
    {
        var state = new GameState();
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        state.battlefields.Add(bf);
        state.activeBattlefieldId = bf.battlefieldId;

        var gunship = new BattlefieldUnit
        {
            unitId = "gun",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            fittedModules = { ["atk_1"] = "mod_anti_missile_laser_s" },
        };
        var healer = new BattlefieldUnit
        {
            unitId = "heal",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            fittedModules = { ["atk_1"] = "mod_remote_shield_repair_s" },
        };
        var target = new BattlefieldUnit
        {
            unitId = "tgt",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
        };
        bf.units.Add(gunship);
        bf.units.Add(healer);
        bf.units.Add(target);

        RemoteRepairSalvoService.OrderRepairTarget(state, bf, "tgt", new[] { "gun", "heal" });
        Assert.That(gunship.pendingRepairRounds, Is.EqualTo(0));
        Assert.That(gunship.targetUnitId, Is.Null);
        Assert.That(healer.pendingRepairRounds, Is.EqualTo(1));
        Assert.That(healer.targetUnitId, Is.EqualTo("tgt"));
        Assert.That(RemoteRepairSalvoService.CountIncomingRepairRounds(bf, target), Is.EqualTo(1));
    }
}

[TestFixture]
public sealed class AntiMissileLaserTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();
    [Test]
    public void Tick_FiresOnlyAtMissiles()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var state = new GameState { combatRealtimeActive = true };
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        state.battlefields.Add(bf);

        var laser = new BattlefieldUnit
        {
            unitId = "laser",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = 0f,
            y = 0f,
            fittedModules = { ["def_1"] = "mod_anti_missile_laser_s" },
        };
        var missile = new BattlefieldUnit
        {
            unitId = "msl",
            side = UnitSide.ENEMY,
            tonnageClass = "MISSILE",
            alive = true,
            arrivalAtSec = 0f,
            x = 1000f,
            y = 0f,
            structureHp = 500f,
            structureMax = 500f,
        };
        var frigate = new BattlefieldUnit
        {
            unitId = "frig",
            side = UnitSide.ENEMY,
            tonnageClass = "FRIGATE",
            alive = true,
            arrivalAtSec = 0f,
            x = 1000f,
            y = 100f,
            structureHp = 500f,
            structureMax = 500f,
            targetUnitId = "laser",
        };
        laser.targetUnitId = "msl";
        bf.units.Add(laser);
        bf.units.Add(missile);
        bf.units.Add(frigate);

        var mslHp = missile.structureHp;
        SpecializedSalvoService.Tick(state, bf, laser, 1f, ships, modules);
        Assert.That(missile.structureHp, Is.LessThan(mslHp));
        Assert.That(frigate.structureHp, Is.EqualTo(500f));
    }
}

[TestFixture]
public sealed class DeterrenceCannonTests
{
    [SetUp]
    public void SetUp() => FieldNavTestContent.PinRepoContentRoot();
    [Test]
    public void Tick_SkipsLowTonnageTarget()
    {
        var modules = ModuleRegistry.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        var state = new GameState { combatRealtimeActive = true, autoFireEnabled = true };
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        state.battlefields.Add(bf);

        var gun = new BattlefieldUnit
        {
            unitId = "gun",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            x = 0f,
            y = 0f,
            fittedModules = { ["atk_1"] = "mod_deterrence_gun_yl" },
            targetUnitId = "cruiser",
        };
        var cruiser = new BattlefieldUnit
        {
            unitId = "cruiser",
            side = UnitSide.ENEMY,
            tonnageClass = "CRUISER",
            alive = true,
            arrivalAtSec = 0f,
            x = 5000f,
            y = 0f,
            shieldHp = 100f,
            shieldMax = 100f,
            armorHp = 100f,
            armorMax = 100f,
            structureHp = 100f,
            structureMax = 100f,
        };
        bf.units.Add(gun);
        bf.units.Add(cruiser);

        var hp = cruiser.structureHp;
        SpecializedSalvoService.Tick(state, bf, gun, 1f, ships, modules);
        Assert.That(cruiser.structureHp, Is.EqualTo(hp));
    }
}
