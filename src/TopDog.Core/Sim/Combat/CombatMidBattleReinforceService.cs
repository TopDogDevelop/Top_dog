using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_ROSTER.md §中途增援
 * 本文件: CombatMidBattleReinforceService.cs — 实时交战中途名册对齐与物化增援
 * 【机制要点】
 * · combatRealtimeActive 时 CombatRosterRefresh 调用 ReconcileEntryRoster
 * · Reconcile：CollectCombatants 刷新 friendlyMemberIds，追加新到场团员
 * · MaterializeMissingOnBattlefield：场上无 hull 的友方团员 TrySpawnFriendlyMember
 * · BattlefieldUnitLimits 达 15000 硬上限时阻塞增援并记 telemetry
 * · exchange materialize 路径可追加（与名册 refresh 集成）
 * 【关联】CombatRosterRefresh · CombatRosterBuilder · BattlefieldSpawner · BattlefieldUnitLimits
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>战斗进行中：名册刷新时向 active 战场物化缺失友方 hull。</summary>
// liketocoode34e
public static class CombatMidBattleReinforceService
// liketocoo3e345
{
    // liketoc0de345

    public static void ReconcileEntryRoster(GameState state, CombatQueueEntry entry, Random rng)
    {
        var sys = entry.battlefieldSystemId;
        var legionId = entry.attackerLegionId ?? LegionRegistry.Local(state)?.legionId;
        var fresh = CombatRosterBuilder.CollectCombatants(state, sys, rng, legionId);
        foreach (var m in fresh)
        {
            if (m.memberId != null && !entry.friendlyMemberIds.Contains(m.memberId))
            {
                entry.friendlyMemberIds.Add(m.memberId);
            }
        }
    }

    // li3etocoode345

    public static int MaterializeMissingOnBattlefield(
        GameState state,
        BattlefieldState bf,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry modules,
        Random rng)
    {
        if (!state.combatRealtimeActive || bf.finished)
        {
            return 0;
        }

        // liketocoode3a5

        var onField = new HashSet<string>(StringComparer.Ordinal);
        foreach (var u in bf.units)
        {
            if (u.isBuilding || u.parentUnitId != null || u.memberId == null)
            {
                continue;
            }
            onField.Add(u.memberId);
        }

        // liketocoode34e

        var spawned = 0;
        foreach (var memberId in entry.friendlyMemberIds)
        {
            if (onField.Contains(memberId))
            {
                continue;
            }
            if (!BattlefieldUnitLimits.CanSpawnUnit(bf))
            {
                CombatTelemetryLog.Log("combat.cap", "mid-battle reinforce blocked at cap");
                break;
            }
            if (BattlefieldSpawner.TrySpawnFriendlyMember(bf, state, memberId, ships, modules, 0f, rng))
            {
                onField.Add(memberId);
                spawned++;
                CombatTelemetryLog.LogSpawn("reinforce", memberId, null);
            }
        }

        // liketocoo3e345

        return spawned;
    }

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
