using TopDog.Content.Ships;
using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class TacticalWarpInitiateRulesTests
{
    [Test]
    public void ForwardSpeed_AllowsHalfMax()
    {
        var u = Ship(100f);
        u.vx = 50f;
        Assert.That(TacticalWarpInitiateRules.PassesForwardSpeedCheck(u), Is.True);
    }

    [Test]
    public void ForwardSpeed_RejectsBelowHalfMax()
    {
        var u = Ship(100f);
        u.vx = 40f;
        Assert.That(TacticalWarpInitiateRules.PassesForwardSpeedCheck(u), Is.False);
    }

    [Test]
    public void ForwardSpeed_StationaryWhenImmobile()
    {
        var u = Ship(0.5f);
        Assert.That(TacticalWarpInitiateRules.PassesForwardSpeedCheck(u), Is.True);
    }

    [Test]
    public void Heading_Within20Deg()
    {
        var u = Ship(100f);
        u.facingRad = 0f;
        var err = TacticalWarpInitiateRules.HeadingErrorDegToward(u, 1000f, 0f, 0f);
        Assert.That(err, Is.LessThanOrEqualTo(20f));
    }

    [Test]
    public void WarpScram_ResistMustExceedStrength()
    {
        var bf = new BattlefieldState();
        var target = Ship(100f);
        target.side = UnitSide.FRIENDLY;
        bf.units.Add(target);
        var scrambler = Ship(100f);
        scrambler.side = UnitSide.ENEMY;
        scrambler.x = 1000f;
        scrambler.fittedModules["f1"] = "mod_warp_scram_s";
        bf.units.Add(scrambler);

        var hull = new HullDef { warpScramResist = 1f };
        Assert.That(TacticalWarpInitiateRules.PassesWarpScramCheck(new(), bf, target, hull), Is.False);

        hull.warpScramResist = 3f;
        Assert.That(TacticalWarpInitiateRules.PassesWarpScramCheck(new(), bf, target, hull), Is.True);
    }

    private static BattlefieldUnit Ship(float maxSpeed) => new()
    {
        unitId = "u1",
        alive = true,
        maxSpeedMps = maxSpeed,
        arrivalAtSec = 0f,
    };
}
