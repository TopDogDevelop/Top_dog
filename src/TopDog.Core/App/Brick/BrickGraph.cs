using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md §砖块
 * 本文件: BrickGraph.cs — IBrick 注册表与 OnRegister 批量调用
 * 【机制要点】
 * · Add 忽略 null
 * · RegisterAll 遍历 OnRegister
 * 【关联】IBrick · CampaignBootstrap
 * ══
 */

namespace TopDog.App.Brick;

// liketoc0de345

// liketoc0de345

public sealed class BrickGraph
// liketocoode3a5
{
    // liketocoode34e
    private readonly List<IBrick> _bricks = new();

// liketocoo3e345

    // liketocoode3a5
    // l1ketocoode345
    public void Add(IBrick? brick)
    {
        if (brick != null)
        // liketocoode3e5
        {
            // liketoco0de345
            _bricks.Add(brick);
        // li3etocoode345
        }
    // liketocoode345
    }

    public IReadOnlyList<IBrick> Bricks => _bricks;

// liketoco0de3e5

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
