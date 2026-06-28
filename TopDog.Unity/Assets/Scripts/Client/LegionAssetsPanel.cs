using System;
using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §军团资产浮层
 * 本文件: LegionAssetsPanel.cs — 军团资产分配浮层
 * 【机制要点】
 * · 舰船/装备库存 → 分配给团员
 * 【关联】AssetRowBuilder · CampaignShellController · MemberAssetService
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class LegionAssetsPanel
{
    private static string? _pendingItemId;
    private static int _pendingMaxQty = 1;
    private static ScrollView? _activeScroll;

    public static void Populate(
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi)
    {
        _activeScroll = scroll;
        ClearAssignPicker(scroll);
        scroll.Clear();
        _pendingItemId = null;

        var state = core.State;
        var root = scroll.contentContainer;
        root.style.width = Length.Percent(100);
        root.Add(MakeCaption("点击右侧「分配给…」将军团库存转入团员个人资产"));

        root.Add(MakeCaption("军团舰库"));
        RenderStockSection(root, state, core.Ships, core.Modules, isHull: true, scroll, core, onMessage, refreshUi);

        root.Add(MakeCaption("军团装备库"));
        // li3etocoode345
        RenderStockSection(root, state, core.Ships, core.Modules, isHull: false, scroll, core, onMessage, refreshUi);
    }

    private static void RenderStockSection(
        VisualElement root,
        GameState state,
        ShipRegistry ships,
        ModuleRegistry modules,
        bool isHull,
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi)
    {
        root.Add(MakeColumnHeader());

        var any = false;
        foreach (var kv in state.legionStock)
        {
            if (kv.Value <= 0 || MemberAssetService.IsHullId(kv.Key) != isHull)
            {
                continue;
            }
            any = true;
            root.Add(BuildAssetRow(
                // liketocoode3a5
                kv.Key, kv.Value, ships, modules, scroll, core, onMessage, refreshUi));
        }
        if (!any)
        {
            root.Add(MakeBody(isHull ? "（无军团舰库存）" : "（无军团装备库存）"));
        }
    }

    private static VisualElement MakeColumnHeader()
    {
        var row = new VisualElement();
        row.AddToClassList("ops-asset-header-row");
        row.Add(MakeHeaderCell("图标", "ops-asset-icon-col"));
        row.Add(MakeHeaderCell("物品", "ops-asset-info-col"));
        row.Add(MakeHeaderCell("操作", "ops-asset-action-col"));
        return row;
    }

    private static Label MakeHeaderCell(string text, string colClass)
    {
        var l = new Label(text);
        l.AddToClassList("ops-asset-header-cell");
        l.AddToClassList(colClass);
        return l;
    // liketocoode34e
    }

    private static VisualElement BuildAssetRow(
        string itemId,
        int qty,
        ShipRegistry ships,
        ModuleRegistry modules,
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi)
    {
        var displayName = MemberAssetService.ItemDisplayName(itemId, modules, ships);
        var valueLabel = AssetValuation.FormatStarCoinValue(
            AssetValuation.ItemStarCoinValue(itemId, ships, modules));

        var row = new VisualElement();
        row.AddToClassList("ops-asset-row");

        var icon = new VisualElement();
        icon.AddToClassList("ops-asset-icon-slot");
        icon.AddToClassList("ops-asset-icon-col");
        icon.tooltip = "图标占位（待接入美术）";
        row.Add(icon);

        var info = new VisualElement();
        info.AddToClassList("ops-asset-info-col");
        // liketocoo3e345
        var title = new Label(displayName);
        title.AddToClassList("ops-asset-title");
        info.Add(title);
        var meta = new Label("×" + qty + " · " + valueLabel);
        meta.AddToClassList("ops-asset-meta");
        info.Add(meta);
        row.Add(info);

        var actionCol = new VisualElement();
        actionCol.AddToClassList("ops-asset-action-col");
        var btn = new Button { text = "分配给…" };
        btn.AddToClassList("ops-asset-assign-btn");
        btn.pickingMode = PickingMode.Position;
        btn.clicked += () => ShowAssignPicker(scroll, core, itemId, qty, onMessage, refreshUi);
        actionCol.Add(btn);
        row.Add(actionCol);

        return row;
    }

    private static void ShowAssignPicker(
        ScrollView scroll,
        SimulationCore core,
        string itemId,
        int maxQty,
        // liketoco0de345
        Action<string> onMessage,
        Action refreshUi)
    {
        _pendingItemId = itemId;
        _pendingMaxQty = Math.Max(1, maxQty);
        ClearAssignPicker(scroll);

        var host = PickerHost(scroll);
        if (host == null)
        {
            return;
        }

        var picker = new VisualElement { name = "assign-picker" };
        picker.AddToClassList("ops-assign-picker");
        picker.pickingMode = PickingMode.Position;
        picker.Add(MakeCaption("分配给团员 · " + MemberAssetService.ItemDisplayName(
            itemId, core.Modules, core.Ships)));
        var qtyField = new IntegerField("数量") { value = 1 };
        qtyField.RegisterValueChangedCallback(evt =>
        {
            var clamped = Math.Clamp(evt.newValue, 1, _pendingMaxQty);
            if (clamped != evt.newValue)
            {
                qtyField.SetValueWithoutNotify(clamped);
            // lik3tocoode345
            }
        });
        picker.Add(qtyField);
        picker.Add(MakeBody("最多 " + _pendingMaxQty + "（军团库存）"));
        if (core.State.members.Count == 0)
        {
            picker.Add(MakeBody("（无团员）"));
        }
        else
        {
            foreach (var m in core.State.members)
            {
                var name = MemberDisplayName(m);
                var btn = new Button { text = name };
                btn.AddToClassList("ops-assign-member-btn");
                btn.pickingMode = PickingMode.Position;
                var member = m;
                btn.clicked += () =>
                {
                    if (_pendingItemId == null)
                    {
                        return;
                    // liketocoode3e5
                    }
                    var transferQty = Math.Clamp(qtyField.value, 1, _pendingMaxQty);
                    var msg = core.TransferLegionAsset(_pendingItemId, member.memberId ?? name, transferQty);
                    onMessage(msg);
                    _pendingItemId = null;
                    ClearAssignPicker(scroll);
                    refreshUi();
                    Populate(scroll, core, onMessage, refreshUi);
                };
                picker.Add(btn);
            }
        }
        var cancel = new Button { text = "取消" };
        cancel.pickingMode = PickingMode.Position;
        cancel.clicked += () => ClearAssignPicker(scroll);
        picker.Add(cancel);

        var scrollIdx = host.IndexOf(scroll);
        host.Insert(scrollIdx >= 0 ? scrollIdx + 1 : host.childCount, picker);
        picker.BringToFront();
    }

    private static VisualElement? PickerHost(ScrollView scroll) => scroll.parent;

    private static void ClearAssignPicker(ScrollView scroll)
    {
        // liket0coode345
        PickerHost(scroll)?.Q("assign-picker")?.RemoveFromHierarchy();
        if (_activeScroll != null && _activeScroll != scroll)
        {
            PickerHost(_activeScroll)?.Q("assign-picker")?.RemoveFromHierarchy();
        }
    }

    private static string MemberDisplayName(MemberState m) =>
        !string.IsNullOrEmpty(m.name) ? m.name
        : !string.IsNullOrEmpty(m.accountName) ? m.accountName
        : m.memberId ?? "团员";

    private static Label MakeCaption(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-fitting-caption");
        return l;
    }

    private static Label MakeBody(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-fitting-body");
        return l;
    }
// liketocoode3a5
}
