using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

public static class PossessionInputService
{
    public static void ApplyPending(GameState state, BattlefieldState bf, float dtSec)
    {
        var u = BattlefieldSystem.FindPossessedUnit(state, bf);
        if (u == null)
        {
            state.possessionToggleThrottle = false;
            return;
        }

        u.aiOrder = UnitAiOrder.MANUAL;
        if (Math.Abs(state.possessionYawInput) > 0.01f || Math.Abs(state.possessionPitchInput) > 0.01f)
        {
            ShipMotionIntegrator.ApplyManualFacing(u, state.possessionYawInput, state.possessionPitchInput, dtSec);
        }

        if (state.possessionToggleThrottle)
        {
            u.throttleOn = !u.throttleOn;
            state.possessionToggleThrottle = false;
        }

        ShipMotionIntegrator.TickUnit(u, dtSec);
        state.possessionYawInput = 0f;
        state.possessionPitchInput = 0f;
    }

    public static void QueueInput(GameState state, PossessionInputSample sample)
    {
        state.possessionYawInput = Math.Clamp(sample.yawInput, -1f, 1f);
        state.possessionPitchInput = Math.Clamp(sample.pitchInput, -1f, 1f);
        if (sample.toggleThrottle)
        {
            state.possessionToggleThrottle = true;
        }
    }
}
