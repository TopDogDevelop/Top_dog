using TopDog.Sim.Combat;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;

namespace TopDog.Tests;

public sealed class VisionGateRailTests
{
    [Test]
    public void ListRailBattlefields_IncludesTransitDestinationWithoutOnFieldFriends()
    {
        var state = DualBfState();
        var from = state.battlefields[0];
        var to = state.battlefields[1];
        state.activeBattlefieldId = from.battlefieldId;

        state.tacticalWarpInTransit.Add(new TacticalWarpTransitEntry
        {
            unit = new BattlefieldUnit
            {
                unitId = "u1",
                memberId = "m1",
                side = UnitSide.FRIENDLY,
            },
            fromBattlefieldId = from.battlefieldId,
            toBattlefieldId = to.battlefieldId,
        });

        var rail = VisionGate.ListRailBattlefields(state);
        Assert.That(rail.Select(b => b.battlefieldId), Does.Contain(to.battlefieldId));
        Assert.That(VisionGate.CountFriendlyFollowScore(state, to), Is.EqualTo(1));
        Assert.That(VisionGate.CountFriendlyFollowScore(state, from), Is.EqualTo(0));
    }

    [Test]
    public void CountCombatUnits_ExcludesSceneProxy()
    {
        var bf = new BattlefieldState
        {
            battlefieldId = "bf-a",
            units =
            {
                new BattlefieldUnit { unitId = "f1", side = UnitSide.FRIENDLY },
                new BattlefieldUnit { unitId = "e1", side = UnitSide.ENEMY },
                new BattlefieldUnit
                {
                    unitId = "proxy",
                    side = UnitSide.FRIENDLY,
                    tonnageClass = BattlefieldSceneProxyService.TonnageClass,
                },
            },
        };

        var (friendly, enemy, total) = VisionGate.CountCombatUnits(bf);
        Assert.That(friendly, Is.EqualTo(1));
        Assert.That(enemy, Is.EqualTo(1));
        Assert.That(total, Is.EqualTo(2));
    }

    private static GameState DualBfState()
    {
        var state = new GameState { combatRealtimeActive = true };
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            eventRegionId = "reg-a",
        });
        state.battlefields.Add(new BattlefieldState
        {
            battlefieldId = "bf-b",
            systemId = "sys-a",
            eventRegionId = "reg-b",
        });
        return state;
    }
}

public sealed class TacticalViewportFollowServiceTests
{
    [Test]
    public void Tick_SwitchesActiveBattlefieldWhenCurrentEmptyAndTransitToOther()
    {
        var state = new GameState { combatRealtimeActive = true };
        var from = new BattlefieldState { battlefieldId = "bf-a", systemId = "sys-a" };
        var to = new BattlefieldState { battlefieldId = "bf-b", systemId = "sys-a" };
        state.battlefields.Add(from);
        state.battlefields.Add(to);
        state.activeBattlefieldId = from.battlefieldId;

        state.tacticalWarpInTransit.Add(new TacticalWarpTransitEntry
        {
            unit = new BattlefieldUnit
            {
                unitId = "u1",
                memberId = "m1",
                side = UnitSide.FRIENDLY,
            },
            fromBattlefieldId = from.battlefieldId,
            toBattlefieldId = to.battlefieldId,
        });

        TacticalViewportFollowService.Tick(state);
        Assert.That(state.activeBattlefieldId, Is.EqualTo(to.battlefieldId));
    }

    [Test]
    public void Tick_DoesNotSwitchWhenCurrentStillHasOnFieldFriendlies()
    {
        var state = new GameState { combatRealtimeActive = true };
        var from = new BattlefieldState
        {
            battlefieldId = "bf-a",
            systemId = "sys-a",
            units =
            {
                new BattlefieldUnit { unitId = "u1", memberId = "m1", side = UnitSide.FRIENDLY },
            },
        };
        var to = new BattlefieldState
        {
            battlefieldId = "bf-b",
            systemId = "sys-a",
            units =
            {
                new BattlefieldUnit { unitId = "u2", memberId = "m2", side = UnitSide.FRIENDLY },
            },
        };
        state.battlefields.Add(from);
        state.battlefields.Add(to);
        state.activeBattlefieldId = from.battlefieldId;

        TacticalViewportFollowService.Tick(state);
        Assert.That(state.activeBattlefieldId, Is.EqualTo(from.battlefieldId));
    }
}
