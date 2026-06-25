using System;
using TopDog.App;
using TopDog.Sim.Economy;
using TopDog.Sim.State;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>军团制造面板（CRAFTING.md · 仅舰船，无机物 1:1 星币）。</summary>
public static class CraftOverlayPanel
{
    public static void Populate(
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi)
    {
        scroll.Clear();
        var root = scroll.contentContainer;
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexGrow = 1;

        var stock = core.State.legionStock;
        var inorganic = stock.TryGetValue("res_inorganic", out var n) ? n : 0;
        root.Add(AssetRowBuilder.MakeSectionCaption("军团无机物: " + inorganic));
        root.Add(AssetRowBuilder.MakeEmpty("定价：1 无机物 = 1 星币等价；仅可制造舰船（舰体）"));

        var list = new ScrollView(ScrollViewMode.Vertical);
        list.style.flexGrow = 1;
        list.style.minHeight = 320;
        root.Add(list);

        foreach (var hullId in CraftRecipeCatalog.ListCraftableHulls(core.Ships))
        {
            var hull = core.Ships.FindHull(hullId);
            if (hull == null)
            {
                continue;
            }

            var cost = CraftRecipeCatalog.InorganicCost(hullId, core.Ships, core.Modules);
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;

            var label = new Label((hull.displayName ?? hullId) + " · " + cost + " 无机物");
            label.style.flexGrow = 1;
            row.Add(label);

            var capturedId = hullId;
            var btn = new Button { text = "制造" };
            btn.clicked += () =>
            {
                onMessage(core.CraftHull(capturedId));
                refreshUi();
            };
            row.Add(btn);
            list.contentContainer.Add(row);
        }
    }
}
