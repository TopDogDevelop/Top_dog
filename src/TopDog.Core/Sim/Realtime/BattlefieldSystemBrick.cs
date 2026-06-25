using TopDog.App.Brick;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

public sealed class BattlefieldSystemBrick : IBrick
{
    public string Id() => "realtime.battlefield";

    public void Tick(BrickContext ctx, float dtSec)
    {
        if (!ctx.State.combatRealtimeActive)
        {
            return;
        }
        BattlefieldSystem.Tick(ctx.State, dtSec);
        if (ctx.State.phase != GamePhase.COMBAT || !ctx.State.combatRealtimeActive)
        {
            return;
        }
        foreach (var bf in ctx.State.battlefields)
        {
            if (!bf.finished || bf.battlefieldId == null
                || !bf.battlefieldId.Equals(ctx.State.activeBattlefieldId, StringComparison.Ordinal))
            {
                continue;
            }
            var summary = BattlefieldWriteback.Apply(ctx.State, bf, CombatPhaseService.CurrentEntry(ctx.State));
            ctx.State.lastCombatSummary = summary;
            ctx.State.combatAwaitingContinue = true;
            ctx.State.combatRealtimeActive = false;
            ctx.State.combatPrepStep = CombatPrepStep.SHOW_RESULT;
            PushAlert(ctx.State, summary);
            break;
        }
    }

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
