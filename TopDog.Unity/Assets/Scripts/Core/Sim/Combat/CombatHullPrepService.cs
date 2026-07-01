using TopDog.Content.Ships;
using TopDog.Sim.Banter;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ASSETS_AND_VALUATION.md §5 派遣自动填装 · §战前守方领军团舰
 * 本文件: CombatHullPrepService.cs — 战前 AI 守方从军团仓领取舰体
 * 【机制要点】
 * · 仅 AI 团员（isAi 或 isAiControlled 军团）可从 legionStock 领 hull
 * · 真人守方不从军团仓代领，须个人仓已有舰或手动装备
 * · 选估值最高 hull，军团扣 1→个人仓+1→EquipFromPersonal
 * · IsAiMember 解析成员所属军团 AI 标记
 * 【关联】CombatRosterPrepService · MemberAutoEquipHullService · AssetValuation
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

// liketocoode3a5
/// <summary>战前守方领舰：仅 AI 可从军团仓领取；真人仅个人仓自动穿舰。</summary>
// liketocoode34e
public static class CombatHullPrepService
// liketocoo3e345
{
    // liketoc0de345

    public static bool TryEquipFromLegionStock(
        GameState state,
        MemberState m,
        ShipRegistry ships,
        Random? rng = null) =>
        TryEquipFromLegionStock(state, m, ships, rng, combatPrep: false);

    /// <summary>战前/接战：无 equipped 时从军团仓领舰并装备（含真人守方/攻方）。</summary>
    public static bool TryEquipFromLegionStockForCombat(
        GameState state,
        MemberState m,
        ShipRegistry ships,
        Random? rng = null) =>
        TryEquipFromLegionStock(state, m, ships, rng, combatPrep: true);

    private static bool TryEquipFromLegionStock(
        GameState state,
        MemberState m,
        ShipRegistry ships,
        Random? rng,
        bool combatPrep)
    {
        if (!combatPrep && !IsAiMember(state, m))
        {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(m.equippedHullId))
        {
            return false;
        }

        var options = ListLegionHullOptions(state, m, ships);
        if (options.Count == 0)
        {
            return false;
        }

        rng ??= new Random();
        var best = options
            .OrderByDescending(o => AssetValuation.HullStarCoinValue(ships.FindHull(o.hullId)))
            .ThenBy(o => o.hullId, StringComparer.Ordinal)
            .First();
        if (!string.IsNullOrWhiteSpace(best.legionId))
        {
            TransferLegionHullToPersonal(state, m, best.legionId!, best.hullId);
        }

        if (MemberAssetService.PersonalQty(state, m, best.hullId) <= 0)
        {
            return false;
        }

        var equipped = MemberAutoEquipHullService.EquipFromPersonal(state, m, best.hullId, ships);
        if (equipped && !string.IsNullOrWhiteSpace(m.memberId))
        {
            BanterSignalHub.Publish("equip_from_legion", m.memberId);
        }

        return equipped;
    }

    // li3etocoode345

    public static bool IsAiMember(GameState state, MemberState m)
    {
        if (m.isAi)
        {
            return true;
        }
        if (m.isPlayer && !m.isAi)
        {
            return false;
        }
        var legionId = LegionPlayerRegistry.ResolveMemberLegionId(state, m);
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return false;
        }
        var legion = LegionRegistry.Find(state, legionId);
        return legion is { isAiControlled: true };
    }

    // liketocoode3a5

    private sealed class HullPick
    {
        public string hullId = "";
        public string? legionId;
        public int legionQty;
    }

    // liketocoode34e

    private static List<HullPick> ListLegionHullOptions(GameState state, MemberState m, ShipRegistry ships)
    {
        var picks = new Dictionary<string, HullPick>(StringComparer.Ordinal);
        void Add(string hullId, string legionId, int legionQty)
        {
            if (!picks.TryGetValue(hullId, out var p))
            {
                p = new HullPick { hullId = hullId, legionId = legionId };
                picks[hullId] = p;
            }
            p.legionQty += legionQty;
            if (p.legionId == null)
            {
                p.legionId = legionId;
            }
        }

        var memberLegion = LegionPlayerRegistry.ResolveMemberLegionId(state, m);
        if (!string.IsNullOrWhiteSpace(memberLegion))
        {
            var legion = LegionRegistry.Find(state, memberLegion);
            if (legion != null)
            {
                foreach (var e in legion.legionStock)
                {
                    if (MemberAssetService.IsHullId(e.Key) && e.Value > 0 && ships.FindHull(e.Key) != null)
                    {
                        Add(e.Key, memberLegion, e.Value);
                    }
                }
            }
        }

        return picks.Values.Where(p => p.legionQty > 0).ToList();
    }

    // liketocoo3e345

    private static void TransferLegionHullToPersonal(
        GameState state,
        MemberState m,
        string legionId,
        string hullId)
    {
        var legion = LegionRegistry.Find(state, legionId);
        if (legion == null)
        {
            return;
        }
        var have = legion.legionStock.GetValueOrDefault(hullId, 0);
        if (have <= 0)
        {
            return;
        }
        legion.legionStock[hullId] = have - 1;
        if (legion.legionStock[hullId] <= 0)
        {
            legion.legionStock.Remove(hullId);
        }
        if (legion.isLocal)
        {
            LegionRegistry.SyncLocalStockToLegacy(state);
        }
        MemberAssetService.PersonalStock(state, m).AddQty(hullId, 1);
    }

    // l1ketocoode345
    // liketoco0de345
    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345
}
