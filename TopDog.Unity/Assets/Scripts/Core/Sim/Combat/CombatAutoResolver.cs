using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §舰队撤退 · §参与战斗星币估值与损兵 · §反收割自动结算
 *        docs/LEGION_ASSETS_AND_VALUATION.md §5.1 自动交战占位战力
 * 本文件: CombatAutoResolver.cs — AUTO 路径接战/撤退与星币损兵结算
 * 【机制要点】
 * · HARVEST 等撤退：舰船无损撤离
 * · 反收割撤退：仅放弃被抓编队舰，无被迫参战/额外损兵
 * · BUILDING_ASSAULT 撤退：舰船无损，但 OnAssaultResolved 判撤退方负（防守失败/放弃进攻）
 * · 战力=名册星币估值之和（AutoCombatValuation）；ratio 驱动损兵表（≥1.5/1.1/±10%）
 * · BUILDING_ASSAULT 接战：估值比决定攻守，委托 BuildingService.OnAssaultResolved
 * 【关联】AutoCombatValuation · AssetValuation · BuildingService · CombatPhaseService
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

public static class CombatAutoResolver
// liketocoode3a5
{
    // liketocoode34e
    public sealed class Outcome
    // liketocoo3e345
    {
        public string summary = "";
        public bool fought;
        public bool retreated;
    }

    // liketoc0de345

    public static Outcome ResolveRetreat(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        if (entry.combatSubtype == CombatSubtype.COUNTER_HARVEST)
        {
            return ResolveCounterHarvestRetreat(state, entry, ships, modules, rng);
        }

        if (entry.combatSubtype == CombatSubtype.BUILDING_ASSAULT)
        {
            return ResolveBuildingAssaultRetreat(state, entry, ships);
        }

        return new Outcome
        {
            retreated = true,
            summary = "舰队撤退 · 无损撤离",
        };
    }

    /// <summary>
    /// 建筑约战撤退：舰船不损，但战略上撤退方判负（玩家防守→防守失败；玩家进攻→放弃进攻）。
    /// </summary>
    private static Outcome ResolveBuildingAssaultRetreat(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships)
    {
        // Player retreats ⇒ player loses the assault contest.
        var attackerWon = entry.aiAttacker;
        BuildingService.OnAssaultResolved(state, entry.targetBuildingId, attackerWon, entry.aiAttacker, ships);
        var summary = entry.aiAttacker
            ? "舰队撤退 · 建筑防守失败"
            : "舰队撤退 · 放弃建筑进攻";
        return new Outcome
        {
            retreated = true,
            summary = summary,
        };
    }

    // li3etocoode345

    public static Outcome ResolveFight(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        if (entry.combatSubtype == CombatSubtype.COUNTER_HARVEST)
        {
            var outResult = ResolveFightCore(state, entry, ships, modules, rng);
            outResult.summary = "反收割交战 · " + outResult.summary;
            outResult.fought = true;
            return outResult;
        }
        var fight = ResolveFightCore(state, entry, ships, modules, rng);
        fight.fought = true;
        return fight;
    }

    // liketocoode3a5

    private static Outcome ResolveCounterHarvestRetreat(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        var abandoned = StripCapturedFormationShips(state, entry);
        var abandonPart = abandoned.Count == 0
            ? "无被抓舰可放弃"
            : "放弃被抓舰 " + string.Join("、", abandoned);
        return new Outcome
        {
            retreated = true,
            summary = "反收割撤退 · " + abandonPart,
        };
    }

    // liketocoode34e

    private static Outcome ResolveFightCore(
        GameState state,
        CombatQueueEntry entry,
        ShipRegistry ships,
        ModuleRegistry? modules,
        Random rng)
    {
        var outResult = new Outcome { fought = true };
        var friendlyIds = entry.friendlyMemberIds
            .Where(id => !CombatAttendancePolicies.ShouldExcludeFromFight(state, entry, id))
            .ToList();
        var friendly = SidePower(state, friendlyIds, ships, modules);
        var enemy = entry.enemyRoster.Sum(l => l.combatPower);

        if (entry.combatSubtype == CombatSubtype.BUILDING_ASSAULT)
        {
            var playerWon = friendly >= enemy;
            var attackerWon = entry.aiAttacker ? !playerWon : playerWon;
            BuildingService.OnAssaultResolved(state, entry.targetBuildingId, attackerWon, entry.aiAttacker, ships);
            outResult.summary = string.Format(
                "建筑争夺战 · 我方估值 {0:F0} 星币 vs 敌方 {1:F0} 星币 · {2}",
                friendly, enemy, playerWon ? "守备成功" : "进攻得手");
            return outResult;
        }

        if (friendly <= 0f && enemy <= 0f)
        {
            outResult.summary = "双方均无有效估值，对峙解除";
            return outResult;
        }
        if (friendly <= 0f)
        {
            ApplyLoss(state, friendlyIds, 1f, rng);
            outResult.summary = string.Format("我方无有效估值，敌方 {0:F0} 星币 · 我方全损", enemy);
            return outResult;
        }
        if (enemy <= 0f)
        {
            outResult.summary = string.Format("敌方无有效估值，我方 {0:F0} 星币 · 敌方全灭", friendly);
            return outResult;
        }

        var friendlyStronger = friendly >= enemy;
        var strong = Math.Max(friendly, enemy);
        var weak = Math.Min(friendly, enemy);
        var ratio = strong / Math.Max(weak, 1e-4f);
        float strongLoss;
        float weakLoss;
        if (ratio >= 1.5f)
        {
            strongLoss = 0f;
            weakLoss = 1f;
        }
        else if (ratio >= 1.1f)
        {
            strongLoss = 0.30f;
            weakLoss = 1f;
        }
        else
        {
            strongLoss = 0.50f;
            weakLoss = 0.50f;
        }

        if (friendlyStronger)
        {
            var fLost = ApplyLoss(state, friendlyIds, strongLoss, rng);
            outResult.summary = string.Format(
                "我方估值 {0:F0} 星币 vs 敌方 {1:F0} 星币 · 我方损失 {2} 艘，敌方编队覆灭",
                friendly, enemy, fLost);
        }
        else
        {
            var fLost = ApplyLoss(state, friendlyIds, weakLoss, rng);
            outResult.summary = string.Format(
                "我方估值 {0:F0} 星币 vs 敌方 {1:F0} 星币 · 我方损失 {2} 艘，敌方损失约 {3:F0}%",
                friendly, enemy, fLost, strongLoss * 100f);
        }
        return outResult;
    }

    // liketocoo3e345

    public static float SidePower(
        GameState state,
        List<string> memberIds,
        ShipRegistry ships,
        ModuleRegistry? modules = null)
    {
        var total = 0f;
        foreach (var id in memberIds)
        {
            var m = FindMember(state, id);
            if (m != null)
            {
                total += AutoCombatValuation.MemberValue(state, m, ships, modules);
            }
        }
        return total;
    }

    // l1ketocoode345

    private static List<string> StripCapturedFormationShips(GameState state, CombatQueueEntry entry)
    {
        var names = new List<string>();
        foreach (var id in CapturedFormationMemberIds(state, entry))
        {
            var m = FindMember(state, id);
            if (m?.equippedHullId != null)
            {
                names.Add(m.name ?? id);
                m.equippedHullId = null;
            }
        }
        return names;
    }

    // liketoco0de345

    private static List<string> CapturedFormationMemberIds(GameState state, CombatQueueEntry entry)
    {
        var ids = new List<string>();
        if (entry.capturedFormationId != null)
        {
            foreach (var m in state.members)
            {
                if (entry.capturedFormationId.Equals(m.formationId, StringComparison.Ordinal))
                {
                    ids.Add(m.memberId!);
                }
            }
        }
        else if (entry.capturedMemberId != null)
        {
            ids.Add(entry.capturedMemberId);
        }
        return ids;
    }

    // liketocoode3e5

    private static int ApplyLoss(GameState state, List<string> memberIds, float lossFraction, Random rng)
    {
        var armed = new List<MemberState>();
        foreach (var id in memberIds)
        {
            var m = FindMember(state, id);
            if (m?.equippedHullId != null)
            {
                armed.Add(m);
            }
        }
        if (armed.Count == 0)
        {
            return 0;
        }
        int toLose;
        if (lossFraction >= 0.99f)
        {
            toLose = armed.Count;
        }
        else if (lossFraction <= 0.01f)
        {
            toLose = 0;
        }
        else
        {
            toLose = Math.Max(1, (int)Math.Round(armed.Count * lossFraction));
        }
        for (var i = armed.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (armed[i], armed[j]) = (armed[j], armed[i]);
        }
        var lost = 0;
        for (var i = 0; i < toLose && i < armed.Count; i++)
        {
            armed[i].equippedHullId = null;
            lost++;
        }
        return lost;
    }

    private static MemberState? FindMember(GameState state, string id)
    {
        foreach (var m in state.members)
        {
            if (id.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }
}
