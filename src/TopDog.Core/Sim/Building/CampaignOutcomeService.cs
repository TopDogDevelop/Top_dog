using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Building;

public static class CampaignOutcomeService
{
    public const string Defeated = "DEFEATED";
    public const string Victory = "VICTORY";
    public const string Draw = "DRAW";

    public static int CountMembersForMatchEnd(GameState state)
    {
        var count = 0;
        foreach (var m in state.members)
        {
            if (!LegionCommanderService.IsCommanderMember(state, m))
            {
                count++;
            }
        }
        return count;
    }

    public static string LegionIdOf(BuildingState b) =>
        !string.IsNullOrWhiteSpace(b.legionId) ? b.legionId!
        : b.playerOwned ? CampaignLegionIds.Player : CampaignLegionIds.Ai;

    public static void ResetCombatRoundEliminations(GameState state) =>
        state.legionFortressEliminatedLegionIdsThisCombatRound.Clear();

    public static void RecordLegionFortressEliminated(GameState state, BuildingState b)
    {
        if (state.phase != GamePhase.COMBAT && state.phase != GamePhase.COMBAT_PREP)
        {
            return;
        }
        if (!BuildingService.LegionFortress.Equals(b.buildingType, StringComparison.Ordinal))
        {
            return;
        }
        var legionId = LegionIdOf(b);
        if (!state.legionFortressEliminatedLegionIdsThisCombatRound.Contains(legionId))
        {
            state.legionFortressEliminatedLegionIdsThisCombatRound.Add(legionId);
        }
    }

    public static HashSet<string> ActiveLegionIds(GameState state)
    {
        var legions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var b in state.buildings)
        {
            if (!BuildingService.IsDockableStatus(b.status))
            {
                continue;
            }
            if (!BuildingService.LegionFortress.Equals(b.buildingType, StringComparison.Ordinal)
                && !BuildingService.PersonalFortress.Equals(b.buildingType, StringComparison.Ordinal))
            {
                continue;
            }
            legions.Add(LegionIdOf(b));
        }
        return legions;
    }

    public static bool QualifiesForDraw(GameState state)
    {
        if (state.peakLegionCount < 2)
        {
            return false;
        }
        if (ActiveLegionIds(state).Count > 0)
        {
            return false;
        }
        if (BuildingService.HasAnyPersonalFortressOnMap(state))
        {
            return false;
        }
        return DistinctEliminatedLegionsThisCombatRound(state) >= 2;
    }

    public static int DistinctEliminatedLegionsThisCombatRound(GameState state) =>
        state.legionFortressEliminatedLegionIdsThisCombatRound
            .Distinct(StringComparer.Ordinal)
            .Count();

    public static void Evaluate(GameState state)
    {
        EvaluatePlayerElimination(state);
        EvaluateMatchEnd(state);
    }

    public static void EvaluatePlayerElimination(GameState state)
    {
        if (Victory.Equals(state.campaignOutcome, StringComparison.Ordinal)
            || Draw.Equals(state.campaignOutcome, StringComparison.Ordinal))
        {
            return;
        }
        if (Defeated.Equals(state.campaignOutcome, StringComparison.Ordinal))
        {
            return;
        }
        if (BuildingService.PlayerDockableBuildings(state).Count > 0)
        {
            return;
        }
        state.campaignOutcome = Defeated;
        PushAlert(state, "败北：星图内已无任何可停靠建筑");
    }

    public static void EvaluateMatchEnd(GameState state)
    {
        if (state.matchEnded)
        {
            return;
        }
        var active = ActiveLegionIds(state);
        if (active.Count > state.peakLegionCount)
        {
            state.peakLegionCount = active.Count;
        }
        if (active.Count > 1)
        {
            return;
        }
        if (active.Count == 1 && state.peakLegionCount < 2)
        {
            return;
        }
        state.matchEnded = true;
        if (active.Count == 1)
        {
            foreach (var id in active)
            {
                state.matchWinnerLegionId = id;
                break;
            }
            if (CampaignLegionIds.Player.Equals(state.matchWinnerLegionId, StringComparison.Ordinal))
            {
                state.campaignOutcome = Victory;
                PushAlert(state, "胜利：你是星图中最后的军团");
            }
            else
            {
                if (!Defeated.Equals(state.campaignOutcome, StringComparison.Ordinal))
                {
                    state.campaignOutcome = Defeated;
                }
                PushAlert(state, "对局结束：敌方军团获胜");
            }
        }
        else if (QualifiesForDraw(state))
        {
            state.matchWinnerLegionId = null;
            state.campaignOutcome = Draw;
            PushAlert(state, "平局：多军团军堡同回合同归于尽");
        }
        else
        {
            state.matchWinnerLegionId = null;
            if (!Victory.Equals(state.campaignOutcome, StringComparison.Ordinal))
            {
                state.campaignOutcome = Defeated;
            }
            PushAlert(state, "对局结束：所有军团均已覆灭");
        }
    }

    public static bool ShouldOfferDefeatChoice(GameState state) =>
        Defeated.Equals(state.campaignOutcome, StringComparison.Ordinal)
        && !Draw.Equals(state.campaignOutcome, StringComparison.Ordinal)
        && !state.matchEnded
        && !state.spectatorMode;

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
