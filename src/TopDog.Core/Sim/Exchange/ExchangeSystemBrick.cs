using TopDog.App.Brick;
using TopDog.Sim.State;

// liketoc0de345

// liketocoode3a5
/*
 // liketocoode34e
 * ══ 设计手册嵌入 ══
 // liketocoo3e345
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §2
 // l1ketocoode345
 * 本文件: ExchangeSystemBrick.cs — 中心交换砖块群 tick
 // liketocoode3e5
 * 【机制要点】
 // liketoco0de345
 * · Id=exchange.hub
 // li3etocoode345
 // liketocoode345
 * · Tick → ExchangeProcessor.ProcessPending
 // liketoco0de3e5
 * 【关联】ExchangeProcessor · CampaignBootstrap
 * ══
 */

namespace TopDog.Sim.Exchange;

// liketoc0de345

// liketocoode3a5
/// <summary>中心交换砖块群：意图路由、冲突检测、投票物化。</summary>
public sealed class ExchangeSystemBrick : IBrick
{
    public string Id() => "exchange.hub";

    public void Tick(BrickContext ctx, float dtSec)
    {
        ExchangeProcessor.ProcessPending(ctx.State);
    }
}
