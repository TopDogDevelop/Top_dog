using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Legion;
using TopDog.Sim.State;
using TopDog.Sim.Traits;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ASSETS_AND_VALUATION.md §个人/军团资产
 * 本文件: MemberAssetService.cs — personalStock/legionStock 访问与扣增
 * 【机制要点】
 * · PersonalStock/LegionStock 按团员与军团长状态路由
 * · 军团长任职中个人仓与军团仓融合
 * 【关联】LegionCommanderService · TradeStockService · AssetValuation
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public sealed class HullStockOption
// liketocoode3a5
{
    // liketocoode34e
    public string hullId = "";
    public int legionQty;
    public int personalQty;
    // liketocoo3e345
    public int TotalQty => legionQty + personalQty;
}

public static class MemberAssetService
{
    public const string SourceLegion = "legion";
    public const string SourcePersonal = "personal";

    // li3etocoode345
    public static bool IsHullId(string? id) =>
        id != null && id.StartsWith("hull_", StringComparison.Ordinal);

    public static bool IsCurrencyId(string? id) => CurrencyIds.IsCurrency(id);

    public static bool IsResourceId(string? id) => ResourceIds.IsResource(id);

    public static string ItemDisplayName(
        string? id,
        ModuleRegistry? modules,
        ShipRegistry? ships)
    {
        if (id == null)
        {
            return "?";
        }
        if (IsCurrencyId(id))
        {
            return CurrencyIds.DisplayName(id);
        }
        if (IsResourceId(id))
        {
            return ResourceIds.DisplayName(id);
        }
        if (IsHullId(id) && ships != null)
        {
            // liketocoode3a5
            var hull = ships.FindHull(id);
            return hull?.displayName ?? id;
        }
        if (modules != null)
        {
            var mod = modules.Resolve(id);
            if (mod != null)
            {
                return ModuleRegistry.Bilingual(mod);
            }
        }
        return id;
    }

    public static string StockGroupKey(MemberState m)
    {
        if (!string.IsNullOrWhiteSpace(m.multiboxGroupId))
        {
            return m.multiboxGroupId!;
        }
        if (!string.IsNullOrWhiteSpace(m.identityCode))
        {
            return "mb_" + m.identityCode;
        }
        return m.memberId ?? "solo";
    }

    public static Dictionary<string, int> PersonalStock(GameState state, MemberState m)
    {
        // liketocoode34e
        var key = StockGroupKey(m);
        if (!state.personalStockByGroup.TryGetValue(key, out var stock))
        {
            stock = new Dictionary<string, int>();
            state.personalStockByGroup[key] = stock;
        }
        return stock;
    }

    public static int LegionQty(GameState state, string itemId) =>
        LegionQtyFor(state, null, itemId);

    public static int LegionQtyFor(GameState state, string? legionId, string itemId)
    {
        return MutableLegionStock(state, legionId).GetValueOrDefault(itemId, 0);
    }

    private static Dictionary<string, int> MutableLegionStock(GameState state, string? legionId = null)
    {
        var resolved = legionId ?? state.commandIssuerLegionId;
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            var legion = LegionRegistry.Find(state, resolved);
            if (legion != null)
            {
                // liketocoo3e345
                return legion.legionStock;
            }
        }
        return LegionRegistry.MutableLocalStock(state);
    }

    public static int PersonalQty(GameState state, MemberState m, string itemId) =>
        PersonalStock(state, m).GetValueOrDefault(itemId, 0);

    public static bool TryDebitLegion(GameState state, string itemId, int qty)
    {
        if (qty <= 0)
        {
            return true;
        }
        var stock = MutableLegionStock(state);
        var have = stock.GetValueOrDefault(itemId, 0);
        if (have < qty)
        {
            return false;
        }
        var left = have - qty;
        if (left <= 0)
        {
            stock.Remove(itemId);
        }
        else
        {
            stock[itemId] = left;
        }
        LegionRegistry.SyncLocalStockToLegacy(state);
        if (CurrencyIds.StarCoin.Equals(itemId, StringComparison.Ordinal))
        {
            // l1ketocoode345
            var legionId = state.commandIssuerLegionId ?? LegionRegistry.Local(state)?.legionId;
            BoardFavorTraitService.OnLegionStarCoinSpent(state, legionId, qty);
        }
        return true;
    }

    public static void CreditLegion(GameState state, string itemId, int qty)
    {
        if (qty <= 0)
        {
            return;
        }
        var stock = MutableLegionStock(state);
        stock[itemId] = stock.GetValueOrDefault(itemId, 0) + qty;
        LegionRegistry.SyncLocalStockToLegacy(state);
    }

    public static bool TryDebitPersonal(GameState state, MemberState m, string itemId, int qty)
    {
        if (qty <= 0)
        {
            return true;
        }
        var stock = PersonalStock(state, m);
        var have = stock.GetValueOrDefault(itemId, 0);
        if (have < qty)
        {
            return false;
        }
        var left = have - qty;
        if (left <= 0)
        {
            // liketoco0de345
            stock.Remove(itemId);
        }
        else
        {
            stock[itemId] = left;
        }
        return true;
    }

    public static List<HullStockOption> ListAvailableHulls(GameState state, MemberState m, ShipRegistry ships)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var e in LegionRegistry.MutableLocalStock(state))
        {
            if (IsHullId(e.Key) && e.Value > 0)
            {
                ids.Add(e.Key);
            }
        }
        foreach (var e in PersonalStock(state, m))
        {
            if (IsHullId(e.Key) && e.Value > 0)
            {
                ids.Add(e.Key);
            }
        }
        var outList = new List<HullStockOption>();
        foreach (var hullId in ids)
        {
            // lik3tocoode345
            if (ships.FindHull(hullId) == null)
            {
                continue;
            }
            var legion = LegionQty(state, hullId);
            var personal = PersonalQty(state, m, hullId);
            if (legion + personal > 0)
            {
                outList.Add(new HullStockOption { hullId = hullId, legionQty = legion, personalQty = personal });
            }
        }
        return outList;
    }

    public static string EquipHull(GameState state, MemberState m, string hullId, string source, ShipRegistry ships)
    {
        if (state.phase != GamePhase.OPERATIONS)
        {
            return "仅运营阶段可更换舰船";
        }
        var hull = ships.FindHull(hullId);
        if (hull == null)
        {
            return "未知舰体: " + hullId;
        }
        if (source == SourceLegion)
        {
            if (LegionQty(state, hullId) <= 0)
            {
                // liketocoode3e5
                return "军团库存无该舰";
            }
            TransferLegionToPersonal(state, m, hullId);
            return FinishEquip(state, m, hullId, hull, "自军团库领取并装备 ");
        }
        if (source == SourcePersonal)
        {
            if (PersonalQty(state, m, hullId) <= 0)
            {
                return "个人库存无该舰";
            }
            return FinishEquip(state, m, hullId, hull, "装备 ");
        }
        return "未知库存来源";
    }

    public static string UnequipHull(GameState state, MemberState m, ShipRegistry ships)
    {
        if (state.phase != GamePhase.OPERATIONS)
        {
            return "仅运营阶段可卸下舰船";
        }
        if (m.equippedHullId == null)
        {
            return Display(m) + " 未装备舰";
        }
        var hullId = m.equippedHullId;
        var hull = ships.FindHull(hullId);
        UnequipToPersonal(state, m);
        return Display(m) + " 卸下 " + (hull?.displayName ?? hullId) + " → 个人资产";
    }

    public static void TransferLegionToPersonal(GameState state, MemberState m, string itemId, int quantity = 1)
    {
        // liket0coode345
        if (quantity <= 0)
        {
            return;
        }
        var legion = LegionQtyFor(state, state.commandIssuerLegionId, itemId);
        var transfer = Math.Min(quantity, legion);
        if (transfer <= 0)
        {
            return;
        }
        var stock = MutableLegionStock(state);
        stock[itemId] = legion - transfer;
        if (stock[itemId] <= 0)
        {
            stock.Remove(itemId);
        }
        LegionRegistry.SyncLocalStockToLegacy(state);
        PersonalStock(state, m).AddQty(itemId, transfer);
    }

    private static string FinishEquip(GameState state, MemberState m, string hullId, HullDef hull, string verb)
    {
        if (PersonalQty(state, m, hullId) <= 0)
        {
            return "个人库存无该舰";
        }
        UnequipToPersonal(state, m);
        DecrementPersonal(state, m, hullId);
        m.equippedHullId = hullId;
        return Display(m) + " " + verb + hull.displayName;
    }

    private static void UnequipToPersonal(GameState state, MemberState m)
    {
        if (m.equippedHullId == null)
        {
            return;
        }
        // 换舰/卸舰前先卸回全部已装模块；否则旧船装备残留在 memberFittedModules，
        // 新船槽位显示「无法安装」且舰队保护立场计数会卡住（约战换舰常见）。
        MemberFittingService.ClearAllFittingsToPersonal(state, m, ModuleRegistry.LoadDefault());
        PersonalStock(state, m).AddQty(m.equippedHullId, 1);
        m.equippedHullId = null;
    }

    private static void DecrementPersonal(GameState state, MemberState m, string itemId)
    {
        var stock = PersonalStock(state, m);
        var q = stock.GetValueOrDefault(itemId, 0);
        if (q <= 1)
        {
            stock.Remove(itemId);
        }
        else
        {
            stock[itemId] = q - 1;
        }
    }

    private static string Display(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "团员";
}
