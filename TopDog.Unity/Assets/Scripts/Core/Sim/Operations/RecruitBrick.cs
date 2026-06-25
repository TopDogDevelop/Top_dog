using TopDog.App.Brick;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Operations;

public sealed class RecruitBrick : IBrick
{
    public string Id() => "operations.recruit";

    public void Tick(BrickContext ctx, float dtSec)
    {
        if (ctx.State.recruitProgressSec <= 0f)
        {
            return;
        }
        var rng = new Random(
            (int)(ctx.State.gameYear * 7919L
            + ctx.State.gameWeek * 131L
            + ctx.State.recruitBatchSeq));
        RecruitService.Tick(ctx.State, dtSec, ctx.Traits, rng, ctx.Ships);
    }
}
