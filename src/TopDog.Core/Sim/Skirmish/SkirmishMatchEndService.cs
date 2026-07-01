using TopDog.Content.Balance;
using TopDog.Sim.Building;
using TopDog.Sim.State;

namespace TopDog.Sim.Skirmish;

public static class SkirmishMatchEndService
{
    public static void Tick(GameState state, float dtSec)
    {
        if (!SkirmishBuildingRules.IsSkirmish(state) || state.skirmish == null || state.matchEnded)
        {
            return;
        }

        state.skirmish.elapsedSec += dtSec;
        var balance = SkirmishBalanceConfig.LoadDefault();
        if (state.skirmish.elapsedSec < balance.matchDurationSec)
        {
            return;
        }

        EndByScore(state, "时限到达");
    }

    public static void EndImmediate(GameState state, string loserLegionId, string reason)
    {
        if (state.matchEnded)
        {
            return;
        }

        string? winner = null;
        foreach (var legion in state.legions)
        {
            if (legion.legionId != null && !legion.legionId.Equals(loserLegionId, StringComparison.Ordinal))
            {
                winner = legion.legionId;
                break;
            }
        }

        Finalize(state, winner, reason);
    }

    public static void EndByScore(GameState state, string reason)
    {
        if (state.matchEnded || state.legions.Count < 2)
        {
            return;
        }

        var a = state.legions[0].legionId;
        var b = state.legions[1].legionId;
        if (a == null || b == null)
        {
            return;
        }

        var scoreA = state.skirmish?.scores.GetValueOrDefault(a) ?? 0;
        var scoreB = state.skirmish?.scores.GetValueOrDefault(b) ?? 0;
        if (scoreA != scoreB)
        {
            Finalize(state, scoreA > scoreB ? a : b, reason);
            return;
        }

        var hpA = SumBuildingStructure(state, a);
        var hpB = SumBuildingStructure(state, b);
        if (hpA != hpB)
        {
            Finalize(state, hpA > hpB ? a : b, reason + " · 建筑HP决胜");
            return;
        }

        state.matchEnded = true;
        state.campaignOutcome = CampaignOutcomeService.Draw;
        state.skirmish!.endReason = reason + " · 平局";
    }

    private static float SumBuildingStructure(GameState state, string legionId)
    {
        var sum = 0f;
        foreach (var bf in state.battlefields)
        {
            foreach (var u in bf.units)
            {
                if (!u.isBuilding || u.IsDestroyed())
                {
                    continue;
                }

                var bld = BuildingService.Find(state, u.buildingId);
                if (bld?.legionId != null && bld.legionId.Equals(legionId, StringComparison.Ordinal))
                {
                    sum += u.structureHp;
                }
            }
        }

        return sum;
    }

    private static void Finalize(GameState state, string? winnerLegionId, string reason)
    {
        state.matchEnded = true;
        state.matchWinnerLegionId = winnerLegionId;
        state.skirmish!.endReason = reason;
        var local = state.legions.Find(l => l.isLocal);
        if (local?.legionId != null && winnerLegionId != null)
        {
            state.campaignOutcome = local.legionId.Equals(winnerLegionId, StringComparison.Ordinal)
                ? CampaignOutcomeService.Victory
                : CampaignOutcomeService.Defeated;
        }
        state.combatRealtimeActive = false;
    }
}
