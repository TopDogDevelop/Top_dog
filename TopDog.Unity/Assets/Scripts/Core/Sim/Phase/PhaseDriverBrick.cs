using TopDog.App.Brick;
using TopDog.Sim.State;

namespace TopDog.Sim.Phase;

public sealed class PhaseDriverBrick : IBrick
{
    public string Id() => "phase.driver";

    public void Tick(BrickContext ctx, float dtSec) { }
}
