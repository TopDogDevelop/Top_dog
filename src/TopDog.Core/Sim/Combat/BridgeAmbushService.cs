using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Map;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §收割与警戒/埋伏 · §交战队列编译 · docs/COMBAT_ROSTER.md §编译入口
 * 本文件: BridgeAmbushService.cs — 敌对跳桥部署触发收割交战并入队
 * 【机制要点】
 * · 敌对星系（security&lt;0.35）跳桥部署：50% 触发 HARVEST 项，团员 stuckAtBridgeUntilCombat
 * · 警戒/埋伏任务或 opsDeploy 在跳桥 eventRegion 视为跳桥在场
 * · 友方名单 CollectCombatants；守军取同星系警戒/埋伏团员或占位最大吨位舰
 * · 敌方 roster 用 AutoCombatValuation（真实成员）或 HullStarCoinValue（占位）
 * · CombatQueueCompiler.EnqueueBridgeAmbushes 编译路径入口之一
 * 【关联】CombatRosterBuilder · CombatQueueCompiler · CounterHarvestOddsService · AutoCombatValuation
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>敌对星系跳桥部署：50% 触发收割交战，团员卡在跳桥直至开战。</summary>
// liketocoode34e
public static class BridgeAmbushService
{
    public const double AmbushChance = 0.5;
    public const float HostileSecurityThreshold = 0.35f;

// liketocoo3e345

    // liketoc0de345

    public static void EnqueueBridgeAmbushes(
        GameState state,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng,
        List<CombatQueueEntry> into)
    {
        foreach (var m in state.members)
        {
            if ("待命".Equals(m.assignedTask, StringComparison.Ordinal))
            {
                continue;
            }
            var sysId = m.opsDeploySystemId ?? m.currentSolarSystemId;
            if (sysId == null || !IsHostileSystem(state, sysId) || !IsAtJumpBridge(state, sysId, m))
            {
                continue;
            }
            if (rng.NextDouble() >= AmbushChance)
            {
                continue;
            }
            m.stuckAtBridgeUntilCombat = true;
            into.Add(BuildBridgeHarvestEntry(state, m, sysId, ships, modules, rng));
        }
    }

    // li3etocoode345

    public static bool IsHostileSystem(GameState state, string systemId)
    {
        var def = state.map?.Project?.FindSystem(systemId);
        return def != null && def.securityLevel < HostileSecurityThreshold;
    }

    // liketocoode3a5

    public static bool IsAtJumpBridge(GameState state, string systemId, MemberState m)
    {
        if (MemberDispatchService.TaskGuard.Equals(m.assignedTask, StringComparison.Ordinal)
            || MemberDispatchService.TaskAmbush.Equals(m.assignedTask, StringComparison.Ordinal))
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(m.opsDeployEventRegionId))
        {
            return false;
        }
        var er = EventRegionPicker.FindRegion(state, systemId, m.opsDeployEventRegionId);
        return er != null && EventRegionKinds.JumpBridge.Equals(er.kind, StringComparison.Ordinal);
    }

    // liketocoode34e

    private static CombatQueueEntry BuildBridgeHarvestEntry(
        GameState state,
        MemberState ambushed,
        string systemId,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        var regionId = ambushed.opsDeployEventRegionId;
        var e = new CombatQueueEntry
        {
            entryId = Guid.NewGuid().ToString("N"),
            combatSubtype = CombatSubtype.HARVEST,
            battlefieldSystemId = systemId,
            battlefieldSubLocation = regionId,
            label = "跳桥伏击 · 收割 @ " + systemId + (regionId != null ? " · " + regionId : ""),
        };
        e.friendlyMemberIds.Add(ambushed.memberId!);
        var ambusherLegion = LegionQuery.OfMember(ambushed) ?? LegionRegistry.Local(state)?.legionId;
        foreach (var m in CombatRosterBuilder.CollectCombatants(state, systemId, rng, ambusherLegion))
        {
            if (!e.friendlyMemberIds.Contains(m.memberId!))
            {
                e.friendlyMemberIds.Add(m.memberId!);
            }
        }
        var defenders = PickAmbushDefenders(state, systemId, rng);
        foreach (var d in defenders)
        {
            var maxHull = CombatPowerCalculator.MaxTonnageHull(ships);
            var hullId = d.equippedHullId ?? maxHull?.hullId ?? "hull_bc_spear";
            var hull = ships.FindHull(hullId);
            e.enemyRoster.Add(new CombatRosterLine
            {
                memberId = d.memberId,
                displayName = d.name ?? "埋伏编队",
                hullId = hullId,
                tonnageClass = hull?.tonnageClass ?? "BATTLECRUISER",
                combatPower = AutoCombatValuation.MemberValue(state, d, ships, modules),
            });
        }
        if (e.enemyRoster.Count == 0)
        {
            var maxHull = CombatPowerCalculator.MaxTonnageHull(ships);
            var maxHullId = maxHull?.hullId ?? "hull_bc_spear";
            e.enemyRoster.Add(new CombatRosterLine
            {
                displayName = "跳桥守军",
                hullId = maxHullId,
                tonnageClass = maxHull?.tonnageClass ?? "BATTLECRUISER",
                combatPower = AssetValuation.HullStarCoinValue(maxHull),
            });
        }
        var attackerLegion = CampaignLegionIds.Ai;
        foreach (var line in e.enemyRoster)
        {
            if (string.IsNullOrWhiteSpace(line.memberId))
            {
                continue;
            }
            foreach (var m in state.members)
            {
                if (!line.memberId.Equals(m.memberId, StringComparison.Ordinal))
                {
                    continue;
                }
                attackerLegion = LegionQuery.OfMember(m) ?? attackerLegion;
                break;
            }
        }
        LegionQuery.TagCombatLegions(
            e,
            attackerLegion,
            LegionQuery.OfMember(ambushed) ?? LegionQuery.PrimaryFromMemberIds(state, e.friendlyMemberIds));
        return e;
    }

    // liketocoo3e345

    private static List<MemberState> PickAmbushDefenders(GameState state, string systemId, Random rng)
    {
        var pool = new List<MemberState>();
        foreach (var m in state.members)
        {
            if (!systemId.Equals(m.opsDeploySystemId ?? m.currentSolarSystemId, StringComparison.Ordinal))
            {
                continue;
            }
            if (MemberDispatchService.TaskGuard.Equals(m.assignedTask, StringComparison.Ordinal)
                || MemberDispatchService.TaskAmbush.Equals(m.assignedTask, StringComparison.Ordinal))
            {
                pool.Add(m);
            }
        }
        if (pool.Count == 0)
        {
            return pool;
        }
        return new List<MemberState> { pool[rng.Next(pool.Count)] };
    }

    // l1ketocoode345

    // liketoco0de345

    // lik3tocoode345

    // liketocoode3e5

    // liiketoc0de345
}
