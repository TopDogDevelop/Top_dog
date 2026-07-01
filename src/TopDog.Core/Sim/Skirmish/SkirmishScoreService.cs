using TopDog.Content.Balance;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Skirmish;

public static class SkirmishScoreService
{
    public static void OnUnitDestroyed(
        GameState state,
        BattlefieldState bf,
        BattlefieldUnit target,
        BattlefieldUnit? attacker,
        ShipRegistry ships)
    {
        if (!SkirmishBuildingRules.IsSkirmish(state) || state.skirmish == null || target.isBuilding)
        {
            return;
        }

        if (target.IsBallisticMissile() || target.parentUnitId != null && target.tonnageClass is "DRONE" or "STRIKE_CRAFT")
        {
            // still score drones/strike craft
        }

        var attackerLegion = attacker?.legionId ?? ResolveLegionFromMember(state, attacker?.memberId);
        if (attackerLegion == null)
        {
            return;
        }

        var defenderLegion = target.legionId ?? ResolveLegionFromMember(state, target.memberId);
        if (defenderLegion != null && defenderLegion.Equals(attackerLegion, StringComparison.Ordinal))
        {
            return;
        }

        var balance = SkirmishBalanceConfig.LoadDefault();
        var points = balance.ScoreForTonnage(target.tonnageClass);
        if (points <= 0)
        {
            return;
        }

        if (!state.skirmish.scores.ContainsKey(attackerLegion))
        {
            state.skirmish.scores[attackerLegion] = 0;
        }

        state.skirmish.scores[attackerLegion] += points;
        state.skirmish.scoreLedger.Add(new SkirmishScoreEntry
        {
            legionId = attackerLegion,
            points = points,
            sourceUnitId = attacker?.unitId,
            targetUnitId = target.unitId,
            targetHullId = target.hullId,
            tonnageClass = target.tonnageClass,
            battlefieldId = bf.battlefieldId,
            timeSec = state.skirmish.elapsedSec,
        });
    }

    private static string? ResolveLegionFromMember(GameState state, string? memberId)
    {
        if (memberId == null)
        {
            return null;
        }

        var member = LegionPlayerRegistry.FindMember(state, memberId);
        return member?.legionId;
    }
}
