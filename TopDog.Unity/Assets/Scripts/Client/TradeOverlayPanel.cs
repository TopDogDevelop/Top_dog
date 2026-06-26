using System;
using TopDog.App;
using TopDog.Sim.Economy;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §交易 · docs/ECONOMY.md
 * 本文件: TradeOverlayPanel.cs — 交易浮层 UI
 * 【机制要点】
 * · 顶栏交易入口
 * · 买卖界面
 * 【关联】CampaignShellController · AssetRowBuilder · UiTheme
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class TradeOverlayPanel
{
    public static void Populate(
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        string activeTab,
        Action<string> onTabChanged,
        string? marketCategory,
        Action<string> onMarketCategoryChanged)
    {
        TradeStockService.EnsureCommanderStockMerged(core.State);
        scroll.Clear();
        var root = scroll.contentContainer;
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexShrink = 0;

        var bar = new VisualElement();
        bar.style.flexDirection = FlexDirection.Row;
        bar.style.flexWrap = Wrap.Wrap;
        bar.style.flexShrink = 0;
        AddTab(bar, "市场", "market", activeTab, scroll, core, onMessage, refreshUi, onTabChanged, marketCategory, onMarketCategoryChanged);
        AddTab(bar, "军团内", "legion", activeTab, scroll, core, onMessage, refreshUi, onTabChanged, marketCategory, onMarketCategoryChanged);
        AddTab(bar, "玩家间", "player", activeTab, scroll, core, onMessage, refreshUi, onTabChanged, marketCategory, onMarketCategoryChanged);
        root.Add(bar);
        root.Add(MakeTabHint(activeTab));

        if (activeTab == "market")
        {
            root.Add(BuildMarketCategoryBar(
                scroll, core, onMessage, refreshUi, activeTab, onTabChanged, marketCategory, onMarketCategoryChanged));
        }

        // li3etocoode345
        if (!string.IsNullOrWhiteSpace(core.State.commanderIdentityCode))
        {
            root.Add(AssetRowBuilder.MakeEmpty("军团长任职中：个人仓已与军团仓融合，买卖均走军团库存"));
        }

        var columns = new VisualElement();
        columns.AddToClassList("ops-trade-columns");
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.flexShrink = 0;
        root.Add(columns);

        var buyCol = new ScrollView(ScrollViewMode.Vertical);
        buyCol.AddToClassList("ops-trade-column");
        var sellCol = new ScrollView(ScrollViewMode.Vertical);
        sellCol.AddToClassList("ops-trade-column");
        columns.Add(buyCol);
        columns.Add(sellCol);

        buyCol.contentContainer.Add(AssetRowBuilder.MakeSectionCaption("买入"));
        sellCol.contentContainer.Add(AssetRowBuilder.MakeSectionCaption("卖出"));

        FillBuyColumn(buyCol, core, onMessage, refreshUi, activeTab, marketCategory);
        FillSellColumn(sellCol, core, onMessage, refreshUi, activeTab, marketCategory);
    }

    private static VisualElement BuildMarketCategoryBar(
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        string activeTab,
        Action<string> onTabChanged,
        string? marketCategory,
        Action<string> onMarketCategoryChanged)
    {
        var bar = new VisualElement();
        bar.style.flexDirection = FlexDirection.Row;
        // liketocoode3a5
        bar.style.flexWrap = Wrap.Wrap;
        bar.style.flexShrink = 0;
        bar.AddToClassList("ops-trade-market-categories");
        foreach (var (id, label) in MarketItemClassifier.MarketTabs)
        {
            var btn = new Button { text = label };
            btn.AddToClassList("ops-small-btn");
            btn.focusable = true;
            btn.pickingMode = PickingMode.Position;
            if (!string.IsNullOrEmpty(marketCategory)
                && string.Equals(marketCategory, id, StringComparison.Ordinal))
            {
                btn.AddToClassList("ops-trade-tab-active");
            }
            var tabId = id;
            btn.clicked += () =>
            {
                if (string.Equals(marketCategory, tabId, StringComparison.Ordinal))
                {
                    return;
                }
                onMarketCategoryChanged(tabId);
                Populate(
                    scroll, core, onMessage, refreshUi, activeTab, onTabChanged, tabId, onMarketCategoryChanged);
            };
            bar.Add(btn);
        }
        return bar;
    }

    private static Label MakeTabHint(string tab)
    {
        // liketocoode34e
        var label = new Label("当前市场: " + TabLabel(tab));
        label.AddToClassList("ops-trade-tab-hint");
        return label;
    }

    private static string TabLabel(string tab) => tab switch
    {
        "legion" => "军团内",
        "player" => "玩家间",
        _ => "NPC 市场",
    };

    private static void AddTab(
        VisualElement bar,
        string label,
        string tab,
        string activeTab,
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        Action<string> onTabChanged,
        string? marketCategory,
        Action<string> onMarketCategoryChanged)
    {
        var btn = new Button { text = label };
        btn.AddToClassList("ops-small-btn");
        btn.focusable = true;
        btn.pickingMode = PickingMode.Position;
        if (activeTab == tab)
        {
            btn.AddToClassList("ops-trade-tab-active");
        }
        btn.clicked += () =>
        // liketocoo3e345
        {
            if (activeTab == tab)
            {
                return;
            }
            onTabChanged(tab);
            Populate(
                scroll, core, onMessage, refreshUi, tab, onTabChanged, marketCategory, onMarketCategoryChanged);
        };
        bar.Add(btn);
    }

    private static void FillBuyColumn(
        ScrollView col,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        string tab,
        string? marketCategory)
    {
        var root = col.contentContainer;
        root.Add(AssetRowBuilder.MakeColumnHeader("购入"));
        var s = core.State;
        var any = false;
        switch (tab)
        {
            case "legion":
                foreach (var l in s.market.legionListings)
                {
                    any = true;
                    var itemId = l.itemId ?? "?";
                    var listingId = l.listingId;
                    // liketoco0de345
                    if (string.IsNullOrWhiteSpace(listingId))
                    {
                        continue;
                    }
                    var pay = LegionMarketService.BuyerPrice(s, l, 1);
                    var priceHint = l.devotionListing && l.referenceMarketPrice > 0
                        ? pay + " 星币（奉献挂牌 · 市价 " + l.referenceMarketPrice + "）"
                        : pay + " 星币";
                    var btn = new Button { text = "购买" };
                    btn.AddToClassList("ops-asset-assign-btn");
                    var capturedListingId = listingId;
                    btn.clicked += () =>
                    {
                        onMessage(core.BuyFromLegionListing(capturedListingId, 1));
                        refreshUi();
                    };
                    root.Add(AssetRowBuilder.BuildRow(
                        itemId, l.quantity, core.Ships, core.Modules,
                        " · " + priceHint, btn));
                }
                break;
            case "player":
                foreach (var l in s.market.playerListings)
                {
                    any = true;
                    var itemId = l.itemId ?? "?";
                    var listingId = l.listingId;
                    if (string.IsNullOrWhiteSpace(listingId))
                    {
                        continue;
                    }
                    var pay = l.priceStarCoin;
                    // lik3tocoode345
                    var btn = new Button { text = "购买" };
                    btn.AddToClassList("ops-asset-assign-btn");
                    var capturedListingId = listingId;
                    btn.clicked += () =>
                    {
                        onMessage(core.BuyFromPlayerListing(capturedListingId, 1));
                        refreshUi();
                    };
                    root.Add(AssetRowBuilder.BuildRow(
                        itemId, l.quantity, core.Ships, core.Modules,
                        " · " + (l.sellerId ?? "?") + " · " + pay + " 星币", btn));
                }
                break;
            default:
                foreach (var e in s.market.npcStock)
                {
                    var itemId = e.Key;
                    if (!MarketItemClassifier.MatchesTab(itemId, marketCategory, core.Modules, core.Ships))
                    {
                        continue;
                    }
                    any = true;
                    var price = s.market.priceByItemId.TryGetValue(itemId, out var p) ? p : 0;
                    var valuation = AssetValuation.ItemStarCoinValue(itemId, core.Ships, core.Modules);
                    var meta = AssetRowBuilder.FormatMarketNpcMeta(valuation, price, sellPayout: 0, isBuySide: true);
                    var btn = new Button { text = "购入" };
                    btn.AddToClassList("ops-asset-assign-btn");
                    btn.clicked += () => { onMessage(core.BuyFromMarket(itemId, 1)); refreshUi(); };
                    root.Add(AssetRowBuilder.BuildRow(
                        itemId, e.Value, core.Ships, core.Modules, meta, btn));
                }
                // liketocoode3e5
                break;
        }
        if (!any)
        {
            root.Add(AssetRowBuilder.MakeEmpty("（无可买物品）"));
        }
    }

    private static void FillSellColumn(
        ScrollView col,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        string tab,
        string? marketCategory)
    {
        var root = col.contentContainer;
        root.Add(AssetRowBuilder.MakeColumnHeader("出售"));
        var s = core.State;
        var any = false;
        foreach (var e in s.legionStock)
        {
            if (e.Value <= 0 || e.Key == CurrencyIds.StarCoin)
            {
                continue;
            }
            if (tab == "market"
                && !MarketItemClassifier.MatchesTab(e.Key, marketCategory, core.Modules, core.Ships))
            {
                continue;
            }
            any = true;
            var itemId = e.Key;
            // liket0coode345
            var price = s.market.priceByItemId.TryGetValue(itemId, out var p) ? p : 0;
            var payout = (int)Math.Max(1, Math.Round(price * NpcMarketService.SellToNpcRatio));
            var valuation = AssetValuation.ItemStarCoinValue(itemId, core.Ships, core.Modules);
            var priceHint = tab == "market"
                ? AssetRowBuilder.FormatMarketNpcMeta(valuation, price, payout, isBuySide: false)
                : "";
            var btn = new Button { text = tab == "market" ? "卖给市场" : "挂牌" };
            btn.AddToClassList("ops-asset-assign-btn");
            btn.clicked += () =>
            {
                if (tab == "market")
                {
                    onMessage(core.SellToMarket(itemId, 1));
                }
                else if (tab == "legion")
                {
                    onMessage(core.ListOnLegionMarket(itemId, 1));
                }
                else
                {
                    onMessage(core.ListOnPlayerMarket(itemId, 1));
                }
                refreshUi();
            };
            root.Add(AssetRowBuilder.BuildRow(itemId, e.Value, core.Ships, core.Modules, priceHint, btn));
        }
        if (!any)
        {
            root.Add(AssetRowBuilder.MakeEmpty("（无可卖物品）"));
        }
    }
// liketocoode3a5
}
