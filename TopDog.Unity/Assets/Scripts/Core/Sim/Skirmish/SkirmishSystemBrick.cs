using TopDog.App.Brick;
using TopDog.Sim.State;

namespace TopDog.Sim.Skirmish;

public sealed class SkirmishSystemBrick : IBrick
{
    public string Id() => "skirmish.system";

    public void Tick(BrickContext ctx, float dtSec)
    {
        if (!SkirmishBuildingRules.IsSkirmish(ctx.State))
        {
            return;
        }

        SkirmishMatchEndService.Tick(ctx.State, dtSec);
        if (ctx.State.matchEnded)
        {
            return;
        }

        var rng = new Random((int)(ctx.State.skirmish?.elapsedSec * 1000) ^ 0x5F3759DF);
        SkirmishRespawnService.Tick(ctx.State, ctx.Ships, ctx.Modules, rng);
        SkirmishAiBrain.TickAll(ctx.State, ctx.Ships, ctx.Modules, dtSec, rng);
    }
}
