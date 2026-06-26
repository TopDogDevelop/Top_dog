using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class SalvoCombatTests
{
    [Test]
    public void TenSecondCycle_FiresTwiceInTwentySeconds()
    {
        var bf = new BattlefieldState { timeSec = 0f };
        var attacker = new BattlefieldUnit
        {
            unitId = "atk",
            side = UnitSide.FRIENDLY,
            salvoRoundDmg = 100f,
            fireCycleSec = 10f,
            fireCooldownSec = 0f,
            attackRangeM = 9000f,
            structureHp = 100f,
            structureMax = 100f,
        };
        var target = new BattlefieldUnit
        {
            unitId = "tgt",
            side = UnitSide.ENEMY,
            structureHp = 1000f,
            structureMax = 1000f,
            x = 100f,
        };
        bf.units.Add(attacker);
        bf.units.Add(target);
        attacker.targetUnitId = target.unitId;

        for (var i = 0; i < 1000; i++)
        {
            bf.timeSec += 0.02f;
            TickSalvoOnly(bf, attacker, 0.02f);
        }

        Assert.That(target.structureHp, Is.EqualTo(800f).Within(0.01f));
        Assert.That(bf.pendingHpDeltas.Count, Is.EqualTo(2));
    }

    [Test]
    public void BuildingDamage_RespectsOnePercentPerSecondCap()
    {
        var bf = new BattlefieldState { timeSec = 0f };
        var building = new BattlefieldUnit
        {
            unitId = "bld",
            isBuilding = true,
            structureHp = 40_000f,
            structureMax = 40_000f,
        };
        bf.units.Add(building);

        var roundDmg = 5000f;
        var applied1 = BuildingCombatRules.ClampBuildingDamage(bf, building, roundDmg);
        Assert.That(applied1, Is.EqualTo(400f).Within(0.01f));

        bf.timeSec = 0.5f;
        var applied2 = BuildingCombatRules.ClampBuildingDamage(bf, building, roundDmg);
        Assert.That(applied2, Is.EqualTo(0f));

        bf.timeSec = 1.1f;
        var applied3 = BuildingCombatRules.ClampBuildingDamage(bf, building, roundDmg);
        Assert.That(applied3, Is.EqualTo(400f).Within(0.01f));
    }

    private static void TickSalvoOnly(BattlefieldState bf, BattlefieldUnit u, float dtSec)
    {
        var target = bf.units.FirstOrDefault(x => x.unitId == u.targetUnitId);
        if (target == null)
        {
            return;
        }
        u.fireCooldownSec -= dtSec;
        if (u.fireCooldownSec > 0f)
        {
            return;
        }
        BattlefieldSystem.ApplyDamage(bf, target, u.salvoRoundDmg);
        u.fireCooldownSec = u.fireCycleSec;
    }
}
