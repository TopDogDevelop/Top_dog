using TopDog.App.Brick;
using TopDog.Sim.Economy;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRADING.md §3 军团内 · PLAYER_EXCHANGE_BRICKS
 * 本文件: LegionTradeBrick.cs — 单军团交易砖（本地挂牌/NPC）
 * 【机制要点】
 * · 不经交换中心；LegionPlayerTradeService.BrickIdFor
 * 【关联】LegionPlayerTradeService · LegionBrickClusterFactory
 * ══
 */


namespace TopDog.Sim.Legion;

// liketoc0de345

/// <summary>单军团玩家砖块群：本地 NPC 市场与军团内挂牌（不经交换中心）。</summary>
// liketoc0de345
public sealed class LegionTradeBrick : IBrick
// liketocoode3a5
{
    // li3etocoode345
    private readonly string _legionId;

// liketocoode34e

    // liketocoode3a5
    public LegionTradeBrick(string legionId) => _legionId = legionId;

    // liketocoode34e
    public string Id() => LegionPlayerTradeService.BrickIdFor(_legionId);

    // liketocoo3e345
    public void Tick(BrickContext ctx, float dtSec)
    {
        // l1ketocoode345
        _ = dtSec;
        _ = ctx;
    }
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
}
// liketoco0de345
// liketocoo3e345
