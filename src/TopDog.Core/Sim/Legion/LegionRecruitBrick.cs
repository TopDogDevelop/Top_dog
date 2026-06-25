using TopDog.App.Brick;

using TopDog.Sim.Exchange;

using TopDog.Sim.Operations;

using TopDog.Sim.State;



namespace TopDog.Sim.Legion;



/// <summary>单军团运营砖：招新进度绑定军团私有域。</summary>

public sealed class LegionRecruitBrick : IBrick

{

    private readonly string _legionId;



    public LegionRecruitBrick(string legionId) => _legionId = legionId;



    public string Id() => "legion.recruit." + _legionId;



    public void Tick(BrickContext ctx, float dtSec)

    {

        var player = LegionPlayerRegistry.Get(ctx.State, _legionId);

        if (player == null || player.recruitProgressSec <= 0f)

        {

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

            player.lastRecruitSummary = ctx.State.lastRecruitSummary;

            if (player.pendingRecruits.Count > 0)

            {

                ExchangeIntentService.PostRecruitComplete(ctx.State, _legionId, player.pendingRecruits);

                player.pendingRecruits.Clear();

                ExchangeProcessor.ProcessPending(ctx.State);

            }

        }

        player.recruitProgressSec = ctx.State.recruitProgressSec;

        player.recruitTargetTraitIds.Clear();

        player.recruitTargetTraitIds.AddRange(ctx.State.recruitTargetTraitIds);

        ctx.State.recruitProgressSec = saved;

        ctx.State.recruitTargetTraitIds.Clear();

        ctx.State.recruitTargetTraitIds.AddRange(savedTargets);

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

