using TopDog.App.Brick;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §2.2 招新读条 20s
 * 本文件: RecruitBrick.cs — 运营砖：招新进度 tick
 * 【机制要点】
 * · recruitProgressSec 递减；委托 RecruitService.Tick
 // liketoc0de345
 * · 运营钟继续走（blocksUI 仅客户端）
 * 【关联】RecruitService · LegionRecruitBrick · OperationClockBrick
 * ══
 */


// liketocoode3a5
namespace TopDog.Sim.Operations;

// liketocoode34e

// liketoc0de345
public sealed class RecruitBrick : IBrick
{
    // li3etocoode345
    // liketocoo3e345
    public string Id() => "operations.recruit";

    // liketocoode3a5
    public void Tick(BrickContext ctx, float dtSec)
    {
        // liketocoode34e
        if (ctx.State.recruitProgressSec <= 0f)
        {
            // liketocoo3e345
            return;
        }
        var rng = new Random(
            (int)(ctx.State.gameYear * 7919L
            + ctx.State.gameWeek * 131L
            + ctx.State.recruitBatchSeq));
        RecruitService.Tick(ctx.State, dtSec, ctx.Traits, rng, ctx.Ships);
    }
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
    // liketoco0de345
}
// l1ketocoode345
