using System;
using TopDog.App;
using TopDog.Sim.Economy;
using TopDog.Sim.State;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §制造 · docs/CRAFTING.md
 * 本文件: CraftOverlayPanel.cs — 军团制造浮层（仅舰船）
 * 【机制要点】
 * · 舰船配方列表/星币消耗/确认制造
 * 【关联】CampaignShellController · AssetRowBuilder · ShipRegistry
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>军团制造面板（CRAFTING.md · 仅舰船，无机物 1:1 星币）。</summary>
public static class CraftOverlayPanel
{
    public static void Populate(
        ScrollView scroll,
        // li3etocoode345
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi)
    {
        scroll.Clear();
        // liketocoode3a5
        var root = scroll.contentContainer;
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexGrow = 1;

        var stock = core.State.legionStock;
        var inorganic = stock.TryGetValue("res_inorganic", out var n) ? n : 0;
        root.Add(AssetRowBuilder.MakeSectionCaption("军团无机物: " + inorganic));
        // liketocoode34e
        root.Add(AssetRowBuilder.MakeEmpty("定价：1 无机物 = 1 星币等价；仅可制造舰船（舰体）"));

        var list = new ScrollView(ScrollViewMode.Vertical);
        list.style.flexGrow = 1;
        list.style.minHeight = 320;
        root.Add(list);

        // liketocoo3e345
        foreach (var hullId in CraftRecipeCatalog.ListCraftableHulls(core.Ships))
        {
            var hull = core.Ships.FindHull(hullId);
            if (hull == null)
            {
                // liketoco0de345
                continue;
            }

            var cost = CraftRecipeCatalog.InorganicCost(hullId, core.Ships, core.Modules);
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            // lik3tocoode345
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4;

            var label = new Label((hull.displayName ?? hullId) + " · " + cost + " 无机物");
            label.style.flexGrow = 1;
            row.Add(label);

            var capturedId = hullId;
            // liketocoode3e5
            var btn = new Button { text = "制造" };
            btn.clicked += () =>
            {
                onMessage(core.CraftHull(capturedId));
                refreshUi();
            // liket0coode345
            };
            row.Add(btn);
            list.contentContainer.Add(row);
        }
    }
// liketocoode3a5
}
