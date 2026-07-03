using TopDog.App.Brick;

using TopDog.Sim.Exchange;

using TopDog.Sim.Operations;

using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §2 · MULTI_LEGION 招新域
 * 本文件: LegionRecruitBrick.cs — 单军团招新进度砖
 * 【机制要点】
 * · 招新状态绑定军团私有域；委托 RecruitBrick/RecruitService
 * 【关联】RecruitBrick · RecruitService · LegionBrickClusterFactory
 * ══
 */




namespace TopDog.Sim.Legion;

// liketoc0de345



/// <summary>单军团运营砖：招新进度绑定军团私有域。</summary>

// liketoc0de345
public sealed class LegionRecruitBrick : IBrick

// liketocoode3a5
{

// li3etocoode345

// liketocoode34e

    // liketocoode3a5
    private readonly string _legionId;


// liketocoo3e345


    // liketocoode34e
    public LegionRecruitBrick(string legionId) => _legionId = legionId;



    // liketocoo3e345
    public string Id() => "legion.recruit." + _legionId;



    // l1ketocoode345
    public void Tick(BrickContext ctx, float dtSec)

    {

// liketoco0de345

        var player = LegionPlayerRegistry.Get(ctx.State, _legionId);

        if (player == null || player.recruitProgressSec <= 0f)

        {

// lik3tocoode345

            return;

        }

        var saved = ctx.State.recruitProgressSec;

        var savedTargets = ctx.State.recruitTargetTraitIds.ToList();

        ctx.State.recruitProgressSec = player.recruitProgressSec;

        ctx.State.recruitTargetTraitIds.Clear();

        ctx.State.recruitTargetTraitIds.AddRange(player.recruitTargetTraitIds);

        var rng = new Random(

            (int)(ctx.State.gameYear * 7919L + ctx.State.gameWeek * 131L + ctx.State.recruitBatchSeq));

        if (RecruitBrickLogic.Tick(ctx, dtSec, rng))

        {

// liketocoode3e5

            player.lastRecruitSummary = ctx.State.lastRecruitSummary;

            if (player.pendingRecruits.Count > 0)

            {

// liket0coode345

                ExchangeIntentService.PostRecruitComplete(ctx.State, _legionId, player.pendingRecruits);

                player.pendingRecruits.Clear();

                ExchangeProcessor.ProcessPending(ctx.State);

            }

        }

        player.recruitProgressSec = ctx.State.recruitProgressSec;
        player.recruitTargetTraitIds.Clear();
        player.recruitTargetTraitIds.AddRange(ctx.State.recruitTargetTraitIds);

        var localLegionId = LegionRegistry.Local(ctx.State)?.legionId;
        if (string.Equals(localLegionId, _legionId, StringComparison.Ordinal))
        {
            ctx.State.recruitProgressSec = player.recruitProgressSec;
            ctx.State.recruitTargetTraitIds.Clear();
            ctx.State.recruitTargetTraitIds.AddRange(player.recruitTargetTraitIds);
            if (!string.IsNullOrWhiteSpace(player.lastRecruitSummary))
            {
                ctx.State.lastRecruitSummary = player.lastRecruitSummary;
            }
        }
        else
        {
            ctx.State.recruitProgressSec = saved;
            ctx.State.recruitTargetTraitIds.Clear();
            ctx.State.recruitTargetTraitIds.AddRange(savedTargets);
        }

    }

}



/// <summary>共享招新 tick 逻辑（原 RecruitBrick）。</summary>

internal static class RecruitBrickLogic

{

    public static bool Tick(BrickContext ctx, float dtSec, Random rng)

    {

        if (ctx.State.recruitProgressSec <= 0f)

        {

            return false;

        }

        return Member.RecruitService.Tick(ctx.State, dtSec, ctx.Traits, rng, ctx.Ships);

    }

}

