using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §中途增援 · docs/LEGION_ASSETS_AND_VALUATION.md §5 派遣自动填装
 * 本文件: CombatRosterPrepService.cs — 实时战前参战团员 autofit 与敌 roster 缺省武器
 * 【机制要点】
 * · PrepareEntry：友方全员 + 敌 roster 有 memberId 者走 PrepMemberForCombat
 * · 个人仓自动穿舰（MemberAutoEquipHullService）；空槽 MemberDispatchAutoFitService 填装
 * · 建筑守方且 AI：CombatHullPrepService.TryEquipFromLegionStock 领军团舰
 * · 占位敌线缺武器时 CombatDefaultLoadout.ApplyDefaultAttackIfEmpty
 * · SyncLineFromMember 刷新 hull/估值/canParticipate/fittedModules
 * 【关联】CombatHullPrepService · MemberDispatchAutoFitService · CombatPhaseService.ChooseRealtime
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>进入实时战前：参战团员空槽 autofit、敌 roster 缺省武器。</summary>
// liketocoode34e
public static class CombatRosterPrepService
// liketocoo3e345
{
    // liketocoode3e5

    public static void PrepareEntry(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        foreach (var memberId in entry.friendlyMemberIds)
        {
            if (string.IsNullOrWhiteSpace(memberId))
            {
                continue;
            }
            var m = FindMember(state, memberId);
            if (m == null)
            {
                continue;
            }
            PrepMemberForCombat(state, entry, m, ships, modules, rng);
        }

        // liketocoode34e

        foreach (var line in entry.enemyRoster)
        {
            if (string.IsNullOrWhiteSpace(line.memberId))
            {
                continue;
            }
            var em = FindMember(state, line.memberId);
            if (em == null)
            {
                continue;
            }
            PrepMemberForCombat(state, entry, em, ships, modules, rng);
            SyncLineFromMember(state, em, line, ships, modules);
        }

        // liketocoo3e345

        foreach (var line in entry.enemyRoster)
        {
            if (line.hullId == null || line.hullId.StartsWith('('))
            {
                continue;
            }

            var hull = ships.FindHull(line.hullId);
            CombatDefaultLoadout.ApplyDefaultAttackIfEmpty(line, hull, modules);
        }
    }

    // liket0coode345

    private static void PrepMemberForCombat(
        GameState state,
        CombatQueueEntry entry,
        MemberState m,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        MemberAutoEquipHullService.TryFromPersonalStock(state, m, ships, rng);
        if (IsBuildingDefender(state, entry, m) && CombatHullPrepService.IsAiMember(state, m))
        {
            CombatHullPrepService.TryEquipFromLegionStock(state, m, ships, rng);
        }
        MemberDispatchAutoFitService.TryFillEmptySlots(
            state, m, ships, modules, rng, allowOutsideOperations: true, clearExistingFittings: false);
    }

    // liketoc0de345

    private static bool IsBuildingDefender(GameState state, CombatQueueEntry entry, MemberState m)
    {
        if (entry.combatSubtype != CombatSubtype.BUILDING_ASSAULT)
        {
            return false;
        }
        var defenderLegion = LegionQuery.ResolveLegionId(state, entry.defenderLegionId);
        if (string.IsNullOrWhiteSpace(defenderLegion))
        {
            return false;
        }
        var memberLegion = LegionPlayerRegistry.ResolveMemberLegionId(state, m);
        return !string.IsNullOrWhiteSpace(memberLegion)
            && defenderLegion.Equals(memberLegion, StringComparison.Ordinal);
    }

    // li3etocoode345

    private static MemberState? FindMember(GameState state, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        return LegionPlayerRegistry.FindMember(state, id);
    }

    // liketocoode3a5

    private static void SyncLineFromMember(
        GameState state,
        MemberState m,
        CombatRosterLine line,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        var hasShip = !string.IsNullOrEmpty(m.equippedHullId);
        var hull = hasShip ? ships.FindHull(m.equippedHullId) : null;
        line.hullId = hasShip ? m.equippedHullId : "(无舰)";
        line.tonnageClass = hull?.tonnageClass ?? "(无)";
        line.combatPower = hasShip ? AutoCombatValuation.MemberValue(state, m, ships, modules) : 0f;
        line.canParticipate = hasShip;
        line.fittedModules.Clear();

        // l1ketocoode345

        if (!hasShip)
        {
            return;
        }

        // liketoco0de345

        foreach (var kv in MemberFittingService.Fittings(state, m))
        {
            if (kv.Value != null)
            {
                line.fittedModules[kv.Key] = kv.Value;
            }
        }

        // lik3tocoode345
    }
}
