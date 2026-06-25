using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Tests;

public sealed class AiRealtimePlayerBrainTests
{
    [Test]
    public void RetargetsFleetFocusEveryThirtySeconds()
    {
        var state = new GameState();
        var bf = new BattlefieldState
        {
            battlefieldId = "bf1",
            timeSec = 0f,
        };
        var possessor = Enemy("ai-big", "BATTLESHIP", 0f, 0f);
        var wingman = Enemy("ai-small", "FRIGATE", 100f, 0f);
        var far = Friendly("far", 10_000f, 0f);
        var near = Friendly("near", 100f, 0f);
        bf.units.Add(possessor);
        bf.units.Add(wingman);
        bf.units.Add(far);
        bf.units.Add(near);

        AiRealtimePlayerBrain.Tick(state, bf, 1f);
        Assert.That(wingman.targetUnitId, Is.EqualTo("near"));

        far.x = 10f;
        near.x = 10_000f;
        AiRealtimePlayerBrain.Tick(state, bf, 29f);
        Assert.That(wingman.targetUnitId, Is.EqualTo("near"), "Fleet focus should not refresh before 30s");

        AiRealtimePlayerBrain.Tick(state, bf, 2f);
        Assert.That(wingman.targetUnitId, Is.EqualTo("far"));
    }

    private static BattlefieldUnit Enemy(string id, string tonnage, float x, float y) => new()
    {
        unitId = id,
        side = UnitSide.ENEMY,
        tonnageClass = tonnage,
        alive = true,
        arrivalAtSec = 0f,
        x = x,
        y = y,
    };

    private static BattlefieldUnit Friendly(string id, float x, float y) => new()
    {
        unitId = id,
        side = UnitSide.FRIENDLY,
        alive = true,
        arrivalAtSec = 0f,
        x = x,
        y = y,
    };
}
