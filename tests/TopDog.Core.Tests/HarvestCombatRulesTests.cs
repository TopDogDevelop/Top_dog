using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class HarvestCombatRulesTests
{
    [Test]
    public void Harvest_EndsWhenTargetKilled()
    {
        var bf = new BattlefieldState
        {
            combatSubtype = TopDog.Sim.Combat.CombatSubtype.HARVEST,
            capturedMemberId = "m-target",
        };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "u1",
            memberId = "m-target",
            side = UnitSide.ENEMY,
            alive = false,
            structureHp = 0f,
        });
        HarvestCombatRules.TickHarvestWin(new GameState(), bf);
        Assert.That(bf.finished, Is.True);
        Assert.That(bf.winReason, Is.EqualTo("harvest_target_killed"));
    }

    [Test]
    public void BuildingFragile_EndsRealtimeSiege()
    {
        var bf = new BattlefieldState
        {
            targetBuildingId = "fort1",
            timeSec = 10f,
            lastBuildingDamagedAtSec = 5f,
        };
        var building = new BuildingState
        {
            buildingId = "fort1",
            buildingType = "PERSONAL_FORT",
            status = "NORMAL",
        };
        BuildingCombatRules.SpawnBuildingUnit(bf, building);
        var bUnit = BuildingCombatRules.FindBuildingUnit(bf, "fort1")!;
        bUnit.structureHp = bUnit.structureMax * 0.49f;
        BuildingCombatRules.TickBuildingWin(bf, building);
        Assert.That(bf.finished, Is.True);
        Assert.That(bf.winReason, Is.EqualTo("building_fragile"));
    }
}
