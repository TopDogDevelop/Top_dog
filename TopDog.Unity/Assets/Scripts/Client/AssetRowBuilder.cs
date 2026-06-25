using System;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using UnityEngine.UIElements;

namespace TopDog.Client;

public static class AssetRowBuilder
{
    public static VisualElement MakeColumnHeader(string actionLabel = "操作")
    {
        var row = new VisualElement();
        row.AddToClassList("ops-asset-header-row");
        row.Add(HeaderCell("图标", "ops-asset-icon-col"));
        row.Add(HeaderCell("物品", "ops-asset-info-col"));
        row.Add(HeaderCell(actionLabel, "ops-asset-action-col"));
        return row;
    }

    public static VisualElement BuildRow(
        string itemId,
        int qty,
        ShipRegistry ships,
        ModuleRegistry modules,
        string metaSuffix,
        Button? actionButton = null)
    {
        var displayName = MemberAssetService.ItemDisplayName(itemId, modules, ships);
        var valueLabel = AssetValuation.FormatStarCoinValue(
            AssetValuation.ItemStarCoinValue(itemId, ships, modules));

        var row = new VisualElement();
        row.AddToClassList("ops-asset-row");

        var icon = new VisualElement();
        icon.AddToClassList("ops-asset-icon-slot");
        icon.AddToClassList("ops-asset-icon-col");
        row.Add(icon);

        var info = new VisualElement();
        info.AddToClassList("ops-asset-info-col");
        var title = new Label(displayName);
        title.AddToClassList("ops-asset-title");
        info.Add(title);
        var meta = new Label("×" + qty + " · " + valueLabel + metaSuffix);
        meta.AddToClassList("ops-asset-meta");
        info.Add(meta);
        row.Add(info);

        var actionCol = new VisualElement();
        actionCol.AddToClassList("ops-asset-action-col");
        if (actionButton != null)
        {
            actionCol.Add(actionButton);
        }
        row.Add(actionCol);
        return row;
    }

    public static Label MakeSectionCaption(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-fitting-caption");
        return l;
    }

    public static Label MakeEmpty(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-overlay-body");
        return l;
    }

    private static Label HeaderCell(string text, string colClass)
    {
        var l = new Label(text);
        l.AddToClassList("ops-asset-header-cell");
        l.AddToClassList(colClass);
        return l;
    }
}
