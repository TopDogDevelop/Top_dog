using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class BuildingAssaultSalvoTests
{
    [Test]
    public void ThreeAttackers_25Sec_ReducesBuildingStructure()
    {
        var state = new GameState { combatRealtimeActive = true };
        var bf = new BattlefieldState
        {
            battlefieldId = "bf-bld",
            timeSec = 0f,
            combatSubtype = CombatSubtype.BUILDING_ASSAULT,
            targetBuildingId = "fort1",
        };
        var building = new BuildingState
        {
            buildingId = "fort1",
            buildingType = "PERSONAL_FORT",
            displayName = "测试堡",
        };
        BuildingCombatRules.SpawnBuildingUnit(bf, building);
        var bUnit = bf.units.First(u => u.isBuilding);
        bUnit.side = UnitSide.ENEMY;
        bUnit.targetUnitId = null;

        for (var i = 0; i < 3; i++)
        {
            var atk = new BattlefieldUnit
            {
                unitId = "atk-" + i,
                side = UnitSide.FRIENDLY,
                salvoRoundDmg = 1000f,
                fireCycleSec = 10f,
                fireCooldownSec = 0f,
                attackRangeM = 9000f,
                structureHp = 100f,
                structureMax = 100f,
                targetUnitId = bUnit.unitId,
                x = 100f * i,
            };
            bf.units.Add(atk);
        }
        state.battlefields.Add(bf);

        var before = bUnit.structureHp;
        for (var tick = 0; tick < 1250; tick++)
        {
            BattlefieldSystem.Tick(state, 0.02f);
        }

        Assert.That(bUnit.structureHp, Is.LessThan(before));
        Assert.That(before - bUnit.structureHp, Is.GreaterThanOrEqualTo(600f));
    }
}
