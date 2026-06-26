using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §舰队撤退 · §参与战斗星币估值与损兵 · §反收割自动结算
 *        docs/LEGION_ASSETS_AND_VALUATION.md §5.1 自动交战占位战力
 * 本文件: CombatAutoResolver.cs — AUTO 路径接战/撤退概率与星币损兵结算
 * 【机制要点】
 * · 撤退：10% 被迫参战 / 40% 全身而退 / 50% 随机损 1～2 艘（卸 equippedHullId）
 * · 战力=名册星币估值之和（AutoCombatValuation）；ratio 驱动损兵表（≥1.5/1.1/±10%）
 * · 反收割撤退：先放弃被抓编队舰，再叠加上表概率与额外损兵
 * · BUILDING_ASSAULT：估值比决定攻守，委托 BuildingService.OnAssaultResolved
 * · 损失落实为随机挑选已装备舰清空 hull
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
        var outResult = new Outcome { retreated = true };
        var roll = (float)rng.NextDouble();
        if (roll < 0.10f)
        {
            outResult.summary = "撤退失败：被迫参战";
            outResult.retreated = false;
            var fight = ResolveFight(state, entry, ships, modules, rng);
            outResult.summary += " · " + fight.summary;
            outResult.fought = true;
            return outResult;
        }
        if (roll < 0.50f)
        {
            outResult.summary = "舰队全身而退，无损失";
            return outResult;
        }
        var losses = 1 + rng.Next(2);
        var lost = StripRandomFriendlyShips(state, entry, losses, rng);
        outResult.summary = "撤退遭拦截：损失 " + lost.Count + " 艘舰 (" + string.Join("、", lost) + ")";
        return outResult;
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
        var outResult = new Outcome { retreated = true };
        var abandoned = StripCapturedFormationShips(state, entry);
        var abandonPart = abandoned.Count == 0
            ? "无舰可放弃"
            : "放弃 " + string.Join("、", abandoned);
        var roll = (float)rng.NextDouble();
        if (roll < 0.10f)
        {
            outResult.retreated = false;
            var fight = ResolveFight(state, entry, ships, modules, rng);
            outResult.fought = true;
            outResult.summary = "反收割撤退失败被迫参战 · " + abandonPart + " · " + fight.summary;
            return outResult;
        }
        if (roll < 0.50f)
        {
            outResult.summary = "反收割撤退 · " + abandonPart + " · 编外编队全身而退";
            return outResult;
        }
        var penaltyEntry = CopyForRetreatPenalty(state, entry);
        var losses = 1 + rng.Next(2);
        var lost = StripRandomFriendlyShips(state, penaltyEntry, losses, rng);
        outResult.summary = "反收割撤退 · " + abandonPart + " · 额外损失 "
            + lost.Count + " 艘(" + string.Join("、", lost) + ")";
        return outResult;
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
        var friendly = SidePower(state, entry.friendlyMemberIds, ships, modules);
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
            ApplyLoss(state, entry.friendlyMemberIds, 1f, rng);
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
            var fLost = ApplyLoss(state, entry.friendlyMemberIds, strongLoss, rng);
            outResult.summary = string.Format(
                "我方估值 {0:F0} 星币 vs 敌方 {1:F0} 星币 · 我方损失 {2} 艘，敌方编队覆灭",
                friendly, enemy, fLost);
        }
        else
        {
            var fLost = ApplyLoss(state, entry.friendlyMemberIds, weakLoss, rng);
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

    // lik3tocoode345

    private static CombatQueueEntry CopyForRetreatPenalty(GameState state, CombatQueueEntry entry)
    {
        var copy = new CombatQueueEntry { combatSubtype = entry.combatSubtype };
        var skip = new HashSet<string>(CapturedFormationMemberIds(state, entry));
        foreach (var id in entry.friendlyMemberIds)
        {
            if (!skip.Contains(id))
            {
                copy.friendlyMemberIds.Add(id);
            }
        }
        return copy;
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

    // liket0coode345

    private static List<string> StripRandomFriendlyShips(
        GameState state,
        CombatQueueEntry entry,
        int maxLoss,
        Random rng)
    {
        var names = new List<string>();
        var armed = new List<MemberState>();
        foreach (var id in entry.friendlyMemberIds)
        {
            var m = FindMember(state, id);
            if (m?.equippedHullId != null)
            {
                armed.Add(m);
            }
        }
        for (var i = armed.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (armed[i], armed[j]) = (armed[j], armed[i]);
        }
        var n = Math.Min(maxLoss, armed.Count);
        for (var i = 0; i < n; i++)
        {
            armed[i].equippedHullId = null;
            names.Add(armed[i].name ?? armed[i].memberId ?? "?");
        }
        return names;
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
