using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §无舰剔除 · docs/LEGION_ASSETS_AND_VALUATION.md §5 派遣自动填装
 * 本文件: CombatRosterPrepService.cs — 实时战前参战团员穿舰/autofit 与敌缺省武器
 * 【机制要点】
 * · 未驾驶且个人/军团仓无舰 → 不进名册（PruneMembersWithoutHull）
 * · 未驾驶且仓内有舰 → 随机穿舰（个人仓优先，否则军团仓）+ clear 后随机装备
 * · 已驾驶 → 仅填空槽；不强制卸装
 * · 占位敌线缺武器时 CombatDefaultLoadout.ApplyDefaultAttackIfEmpty
 * · SyncLineFromMember 刷新 hull/估值/canParticipate/fittedModules
 * 【关联】CombatHullPrepService · MemberDispatchAutoFitService · CombatRosterRefresh
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>进入实时战前：未驾驶则仓内随机穿舰+随机装；仍无舰则剔出名册。</summary>
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
        foreach (var memberId in entry.friendlyMemberIds.ToList())
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
            PrepMemberForCombat(state, m, ships, modules, rng);
        }

        PruneMembersWithoutHull(state, entry);

        // liketocoode34e

        foreach (var line in entry.enemyRoster.ToList())
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
            PrepMemberForCombat(state, em, ships, modules, rng);
            SyncLineFromMember(state, em, line, ships, modules);
        }

        entry.enemyRoster.RemoveAll(l =>
            !l.canParticipate
            || string.IsNullOrWhiteSpace(l.hullId)
            || l.hullId.StartsWith('('));

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

    /// <summary>
    /// 战前单员：未驾驶则仓内随机上舰；上舰成功后随机填装；已驾驶仅补空槽。
    /// </summary>
    public static void PrepMemberForCombat(
        GameState state,
        MemberState m,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        var wasUnpiloted = string.IsNullOrWhiteSpace(m.equippedHullId);
        if (wasUnpiloted)
        {
            MemberAutoEquipHullService.TryFromPersonalStock(state, m, ships, rng);
            if (string.IsNullOrWhiteSpace(m.equippedHullId))
            {
                CombatHullPrepService.TryEquipFromLegionStockForCombat(state, m, ships, rng);
            }
        }

        if (string.IsNullOrWhiteSpace(m.equippedHullId))
        {
            return;
        }

        // 刚从仓库穿上：卸空后随机装备；已在驾驶：只填空槽
        MemberDispatchAutoFitService.TryFillEmptySlots(
            state,
            m,
            ships,
            modules,
            rng,
            allowOutsideOperations: true,
            clearExistingFittings: wasUnpiloted);
    }

    /// <summary>已解析到的团员仍无舰 → 移出友好参战 id；找不到的 id 保留（由物化路径自行跳过）。</summary>
    public static void PruneMembersWithoutHull(GameState state, CombatQueueEntry entry)
    {
        entry.friendlyMemberIds.RemoveAll(id =>
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return true;
            }

            var m = FindMember(state, id);
            if (m == null)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(m.equippedHullId);
        });
    }

    // liketoc0de345

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
