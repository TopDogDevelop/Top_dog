using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class TacticalWarpServiceTests
{
    [Test]
    public void BeginWarpSetsEtaFromDistanceAndSpeed()
    {
        var from = new BattlefieldState
        {
            battlefieldId = "bf-a",
            anchorAu = new[] { 0f, 0f, 0f },
        };
        var to = new BattlefieldState
        {
            battlefieldId = "bf-b",
            anchorAu = new[] { 2f, 0f, 0f },
        };
        var unit = new BattlefieldUnit { unitId = "u1", side = UnitSide.FRIENDLY };

        TacticalWarpService.BeginWarp(unit, from, to, new Content.Ships.HullDef { warpSpeedAups = 4f });

        Assert.That(unit.inTacticalWarp, Is.True);
        Assert.That(unit.warpTargetBfId, Is.EqualTo("bf-b"));
        Assert.That(unit.warpEtaSec, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void TickCompletesWarpAndMovesUnitToTargetBattlefield()
    {
        var state = new GameState { combatRealtimeActive = true };
        var from = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys1",
            anchorAu = new[] { 0f, 0f, 0f },
        };
        var to = new BattlefieldState
        {
            battlefieldId = "bf-b",
            systemId = "sys1",
            anchorAu = new[] { 1f, 0f, 0f },
        };
        var unit = new BattlefieldUnit
        {
            unitId = "u1",
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            structureHp = 100f,
            structureMax = 100f,
        };
        from.units.Add(unit);
        state.battlefields.Add(from);
        state.battlefields.Add(to);
        TacticalWarpService.BeginWarp(unit, from, to, null);

        TacticalWarpService.Tick(state, from, unit.warpEtaSec + 0.01f);

        Assert.That(from.units, Is.Empty);
        Assert.That(to.units, Has.Count.EqualTo(1));
        Assert.That(to.units[0].unitId, Is.EqualTo("u1"));
        Assert.That(unit.inTacticalWarp, Is.False);
    }

    [Test]
    public void GateJumpTransfersAcrossSystemsInstantly()
    {
        var state = new GameState();
        var from = new BattlefieldState { battlefieldId = "bf-a", systemId = "sys-a" };
        var to = new BattlefieldState { battlefieldId = "bf-b", systemId = "sys-b" };
        var unit = new BattlefieldUnit { unitId = "u1", side = UnitSide.FRIENDLY };
        from.units.Add(unit);
        state.battlefields.Add(from);
        state.battlefields.Add(to);

        TacticalWarpService.GateJump(state, unit, from, to);

        Assert.That(from.units, Is.Empty);
        Assert.That(to.units, Has.Count.EqualTo(1));
        Assert.That(unit.inTacticalWarp, Is.False);
    }
}
