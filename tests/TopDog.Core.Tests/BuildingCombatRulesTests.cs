using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class BuildingCombatRulesTests
{
    [Test]
    public void ClampBuildingDamageCapsAtOnePercentPerSecond()
    {
        var bf = new BattlefieldState { timeSec = 0f };
        var building = new BattlefieldUnit
        {
            unitId = "b1",
            isBuilding = true,
            structureMax = 100_000f,
            structureHp = 100_000f,
        };
        var first = BuildingCombatRules.ClampBuildingDamage(bf, building, 50_000f, 0.5f);
        var second = BuildingCombatRules.ClampBuildingDamage(bf, building, 50_000f, 0.5f);
        Assert.That(first + second, Is.EqualTo(1000f).Within(0.01f));
    }

    [Test]
    public void TickBuildingWin_DefenderWinsAfterFifteenMinutesWithoutDamage()
    {
        var bf = new BattlefieldState
        {
            targetBuildingId = "fort1",
            timeSec = 901f,
            lastBuildingDamagedAtSec = -1f,
        };
        var building = new BuildingState { buildingId = "fort1", buildingType = "PERSONAL_FORT" };
        BuildingCombatRules.SpawnBuildingUnit(bf, building);
        BuildingCombatRules.TickBuildingWin(bf, building);
        Assert.That(bf.finished, Is.True);
        Assert.That(bf.winReason, Is.EqualTo("defend_no_attack_15m"));
    }
}
