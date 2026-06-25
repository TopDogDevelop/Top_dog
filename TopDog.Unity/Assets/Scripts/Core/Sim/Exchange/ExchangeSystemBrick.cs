using TopDog.App.Brick;
using TopDog.Sim.State;

namespace TopDog.Sim.Exchange;

/// <summary>中心交换砖块群：意图路由、冲突检测、投票物化。</summary>
public sealed class ExchangeSystemBrick : IBrick
{
    public string Id() => "exchange.hub";

    public void Tick(BrickContext ctx, float dtSec)
    {
        ExchangeProcessor.ProcessPending(ctx.State);
    }
}
