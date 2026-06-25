using TopDog.Sim.State;

namespace TopDog.App.Brick;

public interface IBrick
{
    string Id();

    void OnRegister(BrickContext ctx) { }

    void Tick(BrickContext ctx, float dtSec);

    void OnPhaseChanged(BrickContext ctx, GamePhase phase) { }
}
