using TopDog.App.Brick;
using TopDog.Sim.Economy;
using TopDog.Sim.State;

namespace TopDog.Sim.Legion;

/// <summary>单军团玩家砖块群：本地 NPC 市场与军团内挂牌（不经交换中心）。</summary>
public sealed class LegionTradeBrick : IBrick
{
    private readonly string _legionId;

    public LegionTradeBrick(string legionId) => _legionId = legionId;

    public string Id() => LegionPlayerTradeService.BrickIdFor(_legionId);

    public void Tick(BrickContext ctx, float dtSec)
    {
        _ = dtSec;
        _ = ctx;
    }
}
