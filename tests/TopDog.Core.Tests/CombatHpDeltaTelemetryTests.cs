using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatHpDeltaTelemetryTests
{
    [SetUp]
    public void SetUp()
    {
        CombatTelemetryLog.Clear();
    }

    [Test]
    public void Enqueue_LogsShieldArmorStructureLayers()
    {
        var bf = new BattlefieldState { timeSec = 12.5f };
        var target = new BattlefieldUnit
        {
            unitId = "u1",
            x = 100f,
            y = 200f,
            z = 300f,
        };

        CombatHpDeltaQueue.Enqueue(bf, target, 50f, 30f, 10f, isHeal: false);

        Assert.That(bf.pendingHpDeltas, Has.Count.EqualTo(1));
        var dump = CombatTelemetryLog.DumpRecent(8);
        Assert.That(dump, Does.Contain("[combat.float-damage]"));
        Assert.That(dump, Does.Contain("u1"));
        Assert.That(dump, Does.Contain("shield=-50"));
        Assert.That(dump, Does.Contain("armor=-30"));
        Assert.That(dump, Does.Contain("struct=-10"));
    }

    [Test]
    public void Enqueue_HealUsesFloatHealTag()
    {
        var bf = new BattlefieldState { timeSec = 1f };
        var target = new BattlefieldUnit { unitId = "heal", shieldMax = 100f };

        CombatHpDeltaQueue.Enqueue(bf, target, 25f, 0f, 0f, isHeal: true);

        var dump = CombatTelemetryLog.DumpRecent(4);
        Assert.That(dump, Does.Contain("[combat.float-heal]"));
        Assert.That(dump, Does.Contain("shield=+25"));
    }

    [Test]
    public void MaybeLogPositions_IncludesShieldAndArmor()
    {
        CombatTelemetryLog.Clear();
        var bf = new BattlefieldState { timeSec = 2f };
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "snap",
            x = 1f,
            y = 2f,
            z = 3f,
            shieldHp = 40f,
            shieldMax = 100f,
            armorHp = 80f,
            armorMax = 200f,
            structureHp = 15f,
            structureMax = 50f,
        });

        CombatTelemetryLog.MaybeLogPositions(bf, 2f);

        var dump = CombatTelemetryLog.DumpRecent(4);
        Assert.That(dump, Does.Contain("shield=40/100"));
        Assert.That(dump, Does.Contain("armor=80/200"));
        Assert.That(dump, Does.Contain("struct=15/50"));
    }
}
