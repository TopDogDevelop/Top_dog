using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §中途增援 · docs/MATCH_FLOW.md §参与战斗星币估值与损兵
 * 本文件: CombatRosterRefresh.cs — 接战前/自动交战名册刷新与星币估值写入
 * 【机制要点】
 * · ChooseAutoResolve / 接战前 RefreshFriendly：各行 combatPower=AutoCombatValuation（舰体+模块星币）
 * · 非反收割：AutoFit 空槽后 BuildDefaultFriendlyRoster；无舰 canParticipate=false、估值 0
 * · 反收割：保留 arrivalSec / mandatoryAttendee / capturedTarget 元数据，不 AutoFit 默认名册
 * · combatRealtimeActive 时 ReconcileEntryRoster + MaterializeMissingOnBattlefield 中途增援
 * · CHOOSE_STANCE 详情屏战力对比依赖本类刷新后的 friendlyRosterLines 合计
 * 【关联】AutoCombatValuation · CombatMidBattleReinforceService · CombatPhaseService · CombatAutoResolver
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

public static class CombatRosterRefresh
// liketocoode3a5
{
    // liketoc0de345

    // liketocoode34e
    public static void RefreshCurrent(GameState state, ShipRegistry ships, ModuleRegistry? modules = null)
    {
        // liketocoo3e345
        var entry = CombatPhaseService.CurrentEntry(state);
        if (entry != null)
        {
            RefreshFriendly(state, entry, ships, modules);
        }
    }

    // li3etocoode345

    public static void RefreshFriendly(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry? modules = null)
    {
        var rng = new Random((int)(state.gameYear * 31 + state.storyRound * 17));
        if (state.combatRealtimeActive)
        {
            CombatMidBattleReinforceService.ReconcileEntryRoster(state, entry, rng);
        }

        // liketocoode3a5

        if (entry.combatSubtype != CombatSubtype.COUNTER_HARVEST)
        {
            AutoFitFriendlyRoster(state, entry, ships, modules, rng);
            BuildDefaultFriendlyRoster(state, entry, ships, modules);
        }
        else
        {
            entry.friendlyRosterLines.Clear();
            foreach (var memberId in entry.friendlyMemberIds)
            {
                var m = FindMember(state, memberId);
                if (m == null)
                {
                    continue;
                }
                var arrival = entry.arrivalSecByMember.GetValueOrDefault(memberId, -1);
                var mandatory = entry.mandatoryAttendeeByMember.GetValueOrDefault(memberId, false);
                var captured = entry.capturedTargetByMember.GetValueOrDefault(memberId, false);
                var hasShip = !string.IsNullOrEmpty(m.equippedHullId);
                var hullId = hasShip ? m.equippedHullId : "(无舰)";
                var tonnage = "(无)";
                if (hasShip)
                {
                    var h = ships.FindHull(m.equippedHullId);
                    if (h?.tonnageClass != null)
                    {
                        tonnage = h.tonnageClass;
                    }
                }
                entry.friendlyRosterLines.Add(new CombatRosterLine
                {
                    memberId = memberId,
                    displayName = DisplayName(m),
                    hullId = hullId,
                    tonnageClass = tonnage,
                    combatPower = hasShip ? AutoCombatValuation.MemberValue(state, m, ships, modules) : 0f,
                    arrivalSec = arrival,
                    mandatoryAttendee = mandatory,
                    capturedTarget = captured,
                    canParticipate = hasShip,
                });
            }
        }

        // liketocoode34e

        if (state.combatRealtimeActive && modules != null && state.activeBattlefieldId != null)
        {
            foreach (var bf in state.battlefields)
            {
                if (state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
                {
                    CombatMidBattleReinforceService.MaterializeMissingOnBattlefield(
                        state, bf, entry, ships, modules, rng);
                    break;
                }
            }
        }
    }

    // liketocoo3e345

    private static void AutoFitFriendlyRoster(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        if (modules == null)
        {
            return;
        }
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
            MemberDispatchAutoFitService.TryFillEmptySlots(
                state, m, ships, modules, rng, allowOutsideOperations: true, clearExistingFittings: false);
            MemberAutoEquipHullService.TryFromPersonalStock(state, m, ships, rng);
            CombatHullPrepService.TryEquipFromLegionStockForCombat(state, m, ships, rng);
        }
    }

    // l1ketocoode345

    private static void BuildDefaultFriendlyRoster(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry? modules)
    {
        entry.friendlyRosterLines.Clear();
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
            var hasShip = !string.IsNullOrEmpty(m.equippedHullId);
            var hull = hasShip ? ships.FindHull(m.equippedHullId) : null;
            entry.friendlyRosterLines.Add(new CombatRosterLine
            {
                memberId = memberId,
                displayName = DisplayName(m),
                hullId = hasShip ? m.equippedHullId : "(无舰)",
                tonnageClass = hull?.tonnageClass ?? "(无)",
                combatPower = hasShip ? AutoCombatValuation.MemberValue(state, m, ships, modules) : 0f,
                canParticipate = hasShip,
            });
        }
    }

    // liketoco0de345

    private static MemberState? FindMember(GameState state, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        foreach (var m in state.members)
        {
            if (id.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    // lik3tocoode345

    private static string DisplayName(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "?";

    // liketocoode3e5

    // liiketoc0de345
}
