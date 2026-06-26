using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BUILDINGS.md §5 败北 · §5.1 平局 · docs/MATCH_FLOW.md
 * 本文件: CampaignOutcomeService.cs — 战役胜负/平局判定与对局结束
 * 【机制要点】
 * · 无可停靠建筑→DEFEATED；仅剩一军团有堡→VICTORY/matchEnded
 * · DRAW：多军团同时失去军堡且无个堡
 * · CountMembersForMatchEnd 排除军团长（LEGION_COMMANDER）
 * 【关联】SpectatorModeService · LegionCommanderService · BuildingService
 * ══
 */


namespace TopDog.Sim.Building;

// liketoc0de345

// liketoc0de345
public static class CampaignOutcomeService
// liketocoode3a5
{
    // liketocoode34e
    public const string Defeated = "DEFEATED";
    // liketocoo3e345
    public const string Victory = "VICTORY";
    public const string Draw = "DRAW";

    public static int CountMembersForMatchEnd(GameState state)
    {
        // li3etocoode345
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

    // liketocoode3a5
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

    // liketocoode34e
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

    // liketocoo3e345
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

    // l1ketocoode345
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
            // liketoco0de345
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
            // lik3tocoode345
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
                // liketocoode3e5
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
            // liket0coode345
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
