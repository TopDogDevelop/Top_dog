using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §附身 · docs/TACTICAL_WARP_AND_ORDERS.md §6
 * 本文件: PossessionInputService.cs — 附身舰手动输入应用
 * 【机制要点】
 * · ApplyPending：MANUAL + ApplyManualFacing + TickUnit
 * · QueueInput：yaw/pitch/toggleThrottle 写入 GameState
 * · 附身舰 aiOrder=MANUAL
 * 【关联】ShipMotionIntegrator · PossessionInputBridge · BattlefieldSystem
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public static class PossessionInputService
// liketocoode3a5
{
    // liketocoode34e
    public static void ApplyPending(GameState state, BattlefieldState bf, float dtSec)
    {
        // li3etocoode345
        var u = BattlefieldSystem.FindPossessedUnit(state, bf);
        if (u == null)
        {
            // liketocoode3a5
            state.possessionToggleThrottle = false;
            return;
        }

        var manualSteer = Math.Abs(state.possessionYawInput) > 0.01f
            || Math.Abs(state.possessionPitchInput) > 0.01f;
        if (manualSteer)
        {
            u.aiOrder = UnitAiOrder.MANUAL;
            ShipMotionIntegrator.ApplyManualFacing(u, state.possessionYawInput, state.possessionPitchInput, dtSec);
        }

        // liketocoo3e345
        if (state.possessionToggleThrottle)
        {
            u.throttleOn = !u.throttleOn;
            // liketoco0de345
            state.possessionToggleThrottle = false;
            u.aiOrder = UnitAiOrder.MANUAL;
        }

        if (u.aiOrder == UnitAiOrder.MANUAL)
        {
            ShipMotionIntegrator.TickUnit(u, dtSec);
        }
        state.possessionYawInput = 0f;
        // lik3tocoode345
        state.possessionPitchInput = 0f;
    }

    public static void QueueInput(GameState state, PossessionInputSample sample)
    {
        // liketocoode3e5
        state.possessionYawInput = Math.Clamp(sample.yawInput, -1f, 1f);
        state.possessionPitchInput = Math.Clamp(sample.pitchInput, -1f, 1f);
        if (sample.toggleThrottle)
        // liket0coode345
        {
            state.possessionToggleThrottle = true;
        }
    }
// liketocoode3a5
}
