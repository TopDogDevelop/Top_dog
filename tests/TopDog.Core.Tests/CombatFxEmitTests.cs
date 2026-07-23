using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class CombatFxEmitTests
{
    [Test]
    public void HybridGunTracer_Enqueues_WithoutChangingUnits()
    {
        var bf = new BattlefieldState { timeSec = 12.5f };
        var firer = new BattlefieldUnit { unitId = "a", x = 0f, y = 0f, z = 0f };
        var target = new BattlefieldUnit { unitId = "b", x = 30_000f, y = 0f, z = 0f };
        bf.units.Add(firer);
        bf.units.Add(target);

        CombatFxEmit.HybridGunTracer(bf, firer, target, 30_000f);

        Assert.That(bf.pendingCombatFx.Count, Is.EqualTo(1));
        var ev = bf.pendingCombatFx[0];
        Assert.That(ev.kind, Is.EqualTo(CombatFxEvent.KindHybridGunTracer));
        Assert.That(ev.firerUnitId, Is.EqualTo("a"));
        Assert.That(ev.targetUnitId, Is.EqualTo("b"));
        Assert.That(ev.distAtSpawnM, Is.EqualTo(30_000f));
        Assert.That(ev.battleTimeSec, Is.EqualTo(12.5f));
        Assert.That(firer.shieldHp, Is.EqualTo(0f));
        Assert.That(target.shieldHp, Is.EqualTo(0f));
    }

    [TestCase(10_000f, 0.2f)]
    [TestCase(25_000f, 0.5f)]
    [TestCase(50_000f, 1f)]
    [TestCase(100_000f, 1f)]
    public void ResolveTracerDurationSec_CapsAtOneSecond(float dist, float expected)
    {
        Assert.That(CombatFxEmit.ResolveTracerDurationSec(dist), Is.EqualTo(expected).Within(1e-4f));
    }
}
