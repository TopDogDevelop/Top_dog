using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Core.Tests;

[TestFixture]
public sealed class PossessionFleetOrderTests
{
    [Test]
    public void ApplyPending_DoesNotClobberApproach_WhenNoManualInput()
    {
        var state = new GameState
        {
            possessingMemberId = "m1",
            combatRealtimeActive = true,
        };
        var bf = new BattlefieldState { battlefieldId = "bf1", timeSec = 0f };
        var u = new BattlefieldUnit
        {
            unitId = "f1",
            memberId = "m1",
            side = UnitSide.FRIENDLY,
            alive = true,
            aiOrder = UnitAiOrder.APPROACH,
            approachTargetUnitId = "t1",
            throttleOn = true,
            maxSpeedMps = 200f,
            accelMps2 = 50f,
        };
        bf.units.Add(u);
        bf.units.Add(new BattlefieldUnit
        {
            unitId = "t1",
            side = UnitSide.FRIENDLY,
            alive = true,
            isBuilding = true,
            x = 5000f,
        });

        PossessionInputService.ApplyPending(state, bf, 0.05f);

        Assert.That(u.aiOrder, Is.EqualTo(UnitAiOrder.APPROACH));
    }

    [Test]
    public void ApplyPending_ForcesManual_WhenSteeringInput()
    {
        var state = new GameState
        {
            possessingMemberId = "m1",
            possessionYawInput = 1f,
        };
        var bf = new BattlefieldState { battlefieldId = "bf1" };
        var u = new BattlefieldUnit
        {
            unitId = "f1",
            memberId = "m1",
            side = UnitSide.FRIENDLY,
            alive = true,
            aiOrder = UnitAiOrder.APPROACH,
        };
        bf.units.Add(u);

        PossessionInputService.ApplyPending(state, bf, 0.05f);

        Assert.That(u.aiOrder, Is.EqualTo(UnitAiOrder.MANUAL));
    }
}
