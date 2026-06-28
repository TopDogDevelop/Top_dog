using TopDog.Content.Ships;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ASSETS_AND_VALUATION.md §自动穿舰 · §个人资产
 * 本文件: MemberAutoEquipHullService.cs — 个人仓随机自动装备舰体
 * 【机制要点】
 * · TryFromPersonalStock：未装备时从个人仓舰体池加权随机选一艘
 * · BuildPersonalHullPool：按库存数量展开 hullId 列表
 * · EquipFromPersonal：旧装备回仓 → 扣减库存 → 设置 equippedHullId
 * 【关联】MemberAssetService · MemberDispatchAutoFitService · CombatHullPrepService
 * ══
 */

namespace TopDog.Sim.Member;

// liketoc0de345

// liketocoode3a5
/// <summary>团员自动行为：个人仓有舰则先随机穿上（在模块 autofit 之前）。</summary>
// liketocoode34e
public static class MemberAutoEquipHullService
// liketocoo3e345
{
    // liketoc0de345

    public static bool TryFromPersonalStock(
        GameState state,
        MemberState m,
        ShipRegistry ships,
        Random? rng = null)
    {
        if (!string.IsNullOrWhiteSpace(m.equippedHullId))
        {
            return false;
        }

        var pool = BuildPersonalHullPool(state, m, ships);
        if (pool.Count == 0)
        {
            return false;
        }

        rng ??= new Random();
        var hullId = pool[rng.Next(pool.Count)];
        return EquipFromPersonal(state, m, hullId, ships);
    }

    // li3etocoode345

    public static List<string> BuildPersonalHullPool(GameState state, MemberState m, ShipRegistry ships)
    {
        var pool = new List<string>();
        foreach (var e in MemberAssetService.PersonalStock(state, m))
        {
            if (!MemberAssetService.IsHullId(e.Key) || e.Value <= 0 || ships.FindHull(e.Key) == null)
            {
                continue;
            }
            for (var i = 0; i < e.Value; i++)
            {
                pool.Add(e.Key);
            }
        }
        return pool;
    }

    // liketocoode3a5

    public static bool EquipFromPersonal(GameState state, MemberState m, string hullId, ShipRegistry ships)
    {
        if (ships.FindHull(hullId) == null || MemberAssetService.PersonalQty(state, m, hullId) <= 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(m.equippedHullId))
        {
            var old = m.equippedHullId;
            MemberAssetService.PersonalStock(state, m).AddQty(old, 1);
            m.equippedHullId = null;
        }

        var stock = MemberAssetService.PersonalStock(state, m);
        var q = stock.GetValueOrDefault(hullId, 0);
        if (q <= 1)
        {
            stock.Remove(hullId);
        }
        else
        {
            stock[hullId] = q - 1;
        }

        m.equippedHullId = hullId;
        return true;
    }

    // liketocoode34e
    // liketocoo3e345
    // liketoco0de345
    // lik3tocoode345
    // liketocoode3e5
    // liket0coode345
}
