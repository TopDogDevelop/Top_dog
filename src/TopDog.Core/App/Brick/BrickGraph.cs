using TopDog.Sim.State;

namespace TopDog.App.Brick;

public sealed class BrickGraph
{
    private readonly List<IBrick> _bricks = new();

    public void Add(IBrick? brick)
    {
        if (brick != null)
        {
            _bricks.Add(brick);
        }
    }

    public IReadOnlyList<IBrick> Bricks => _bricks;

    public void RegisterAll(BrickContext ctx)
    {
        foreach (var b in _bricks)
        {
            b.OnRegister(ctx);
        }
    }

    public void TickAll(BrickContext ctx, float dtSec)
    {
        foreach (var b in _bricks)
        {
            var id = b.Id();
            BrickDebugLog.TickBegin(id, dtSec);
            b.Tick(ctx, dtSec);
            BrickDebugLog.TickEnd(id);
        }
    }

    public void NotifyPhase(BrickContext ctx, GamePhase phase)
    {
        foreach (var b in _bricks)
        {
            b.OnPhaseChanged(ctx, phase);
        }
    }
}
