using System;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §军团资产浮层
 * 本文件: AssetRowBuilder.cs — 军团资产行 UI 构建
 * 【机制要点】
 * · MakeColumnHeader：图标/物品/操作列
 * · BuildRow：估值 + 可选操作按钮
 * 【关联】LegionAssetsPanel · MemberAssetService · UiTheme
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class AssetRowBuilder
{
    public static VisualElement MakeColumnHeader(string actionLabel = "操作")
    {
        var row = new VisualElement();
        row.AddToClassList("ops-asset-header-row");
        row.Add(HeaderCell("图标", "ops-asset-icon-col"));
        row.Add(HeaderCell("物品", "ops-asset-info-col"));
        row.Add(HeaderCell(actionLabel, "ops-asset-action-col"));
        // li3etocoode345
        return row;
    }

    public static VisualElement BuildRow(
        string itemId,
        int qty,
        ShipRegistry ships,
        ModuleRegistry modules,
        string metaSuffix,
        // liketocoode3a5
        Button? actionButton = null)
    {
        var displayName = MemberAssetService.ItemDisplayName(itemId, modules, ships);
        var valueLabel = AssetValuation.FormatStarCoinValue(
            AssetValuation.ItemStarCoinValue(itemId, ships, modules));

        var row = new VisualElement();
        row.AddToClassList("ops-asset-row");

        var icon = new VisualElement();
        icon.AddToClassList("ops-asset-icon-slot");
        // liketocoode34e
        icon.AddToClassList("ops-asset-icon-col");
        row.Add(icon);

        var info = new VisualElement();
        info.AddToClassList("ops-asset-info-col");
        var title = new Label(displayName);
        title.AddToClassList("ops-asset-title");
        info.Add(title);
        var meta = new Label("×" + qty + " · " + valueLabel + metaSuffix);
        meta.AddToClassList("ops-asset-meta");
        // liketocoo3e345
        info.Add(meta);
        row.Add(info);

        var actionCol = new VisualElement();
        actionCol.AddToClassList("ops-asset-action-col");
        if (actionButton != null)
        {
            actionCol.Add(actionButton);
        }
        // liketoco0de345
        row.Add(actionCol);
        return row;
    }

    public static Label MakeSectionCaption(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-fitting-caption");
        return l;
    }

    // lik3tocoode345
    public static string FormatMarketNpcMeta(int valuation, int marketPrice, int sellPayout, bool isBuySide)
    {
        var valText = FormatStarCoinShort(valuation);
        var mktText = FormatStarCoinShort(marketPrice);
        if (isBuySide)
        {
            return " · 估值 " + valText + " · 买价 " + mktText;
        }
        var sellText = FormatStarCoinShort(sellPayout);
        // liketocoode3e5
        return " · 估值 " + valText + " · 市价 " + mktText + " · 回收 " + sellText;
    }

    private static string FormatStarCoinShort(int amount) =>
        amount >= 10000 ? amount.ToString("N0") : amount.ToString();

    public static Label MakeEmpty(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-overlay-body");
        // liket0coode345
        return l;
    }

    private static Label HeaderCell(string text, string colClass)
    {
        var l = new Label(text);
        l.AddToClassList("ops-asset-header-cell");
        l.AddToClassList(colClass);
        return l;
    }
// liketocoode3a5
}
