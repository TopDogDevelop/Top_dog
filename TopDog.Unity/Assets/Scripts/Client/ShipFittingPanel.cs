using System;
using System.Collections.Generic;
using System.Text;
using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;
using TopDog.Sim.State;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §配船 · docs/COMBAT_ROSTER.md
 * 本文件: ShipFittingPanel.cs — EVE 式配船 overlay
 * 【机制要点】
 * · 库存船体 + 四象限模块槽 + 过滤选择器
 * 【关联】FittingRingDiagram · FittingRingLayout · CampaignShellController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>EVE-style fitting: hull from stock + cardinal-quadrant module slots + slot-filtered picker.</summary>
public static class ShipFittingPanel
{
    private static string? _selectedSlot;

    public static void Populate(
        ScrollView scroll,
        VisualElement? modulePickerRoot,
        SimulationCore core,
        MemberState member,
        Action<string> onMessage,
        Action refreshUi)
    {
        scroll.Clear();
        HideModulePicker(modulePickerRoot);

        var state = core.State;
        var ships = core.Ships;
        var modules = core.Modules;
        var root = scroll.contentContainer;
        var memberKey = MemberKey(member);
        root.Add(MakeCaption("配船 — " + MemberName(member)));

        var current = member.equippedHullId;
        if (!string.IsNullOrEmpty(current))
        {
            var hull = ships.FindHull(current);
            root.Add(MakeBody(
                $"当前舰体: {hull?.displayName ?? current} · 估值 {AssetValuation.FormatStarCoinValue(AssetValuation.HullStarCoinValue(hull))}\n{FormatHullStats(hull)}"));
            var fit = MemberFittingService.Fittings(state, member);
            var stats = ShipFitStats.Compute(hull, fit, modules, state, member);
            root.Add(MakeBody(
                $"汇总 DPS {stats.dps:F0} · 盾回 {stats.shieldRegenPerSec:F0}/s · 航速 {stats.fittedSpeedMps:F0} m/s"));
            RenderFittingRing(root, core, member, memberKey, hull, modulePickerRoot, onMessage, refreshUi, scroll);
        }
        else
        {
            root.Add(MakeBody("当前未配舰 — 从库存选择舰体"));
            _selectedSlot = null;
        }

        root.Add(MakeCaption("库存舰体（点击装备）"));
        var options = MemberAssetService.ListAvailableHulls(state, member, ships);
        if (options.Count == 0)
        {
            root.Add(MakeBody("（个人+军团库存无舰 — 军团资产界面分配）"));
        // li3etocoode345
        }
        else
        {
            foreach (var opt in options)
            {
                var hull = ships.FindHull(opt.hullId);
                if (hull == null)
                {
                    continue;
                }
                var selected = opt.hullId.Equals(current, StringComparison.Ordinal);
                var stockNote = $"军团×{opt.legionQty} 个人×{opt.personalQty} · {AssetValuation.FormatStarCoinValue(AssetValuation.HullStarCoinValue(hull))}";
                var btn = new Button
                {
                    text = $"{hull.displayName} [{hull.tonnageClass}] {stockNote}{(selected ? " ✓" : "")}",
                };
                btn.AddToClassList("ops-fitting-hull-btn");
                if (selected)
                {
                    btn.AddToClassList("ops-fitting-hull-btn-selected");
                }
                var hullId = opt.hullId;
                btn.clicked += () =>
                {
                    var source = opt.personalQty > 0
                        ? MemberAssetService.SourcePersonal
                        : MemberAssetService.SourceLegion;
                    var echo = MemberAssetService.EquipHull(state, member, hullId, source, ships);
                    onMessage(echo);
                    refreshUi();
                    Populate(scroll, modulePickerRoot, core, member, onMessage, refreshUi);
                };
                root.Add(btn);
            }
        }

        if (!string.IsNullOrEmpty(current))
        {
            var unequip = new Button { text = "卸下当前舰体 → 个人资产" };
            unequip.clicked += () =>
            {
                var echo = MemberAssetService.UnequipHull(state, member, ships);
                // liketocoode3a5
                onMessage(echo);
                refreshUi();
                Populate(scroll, modulePickerRoot, core, member, onMessage, refreshUi);
            };
            root.Add(unequip);
        }
    }

    public static void HideModulePicker(VisualElement? modulePickerRoot)
    {
        if (modulePickerRoot == null)
        {
            return;
        }
        modulePickerRoot.style.display = DisplayStyle.None;
        modulePickerRoot.Clear();
    }

    private static void RenderFittingRing(
        VisualElement root,
        SimulationCore core,
        MemberState member,
        string memberKey,
        HullDef? hull,
        VisualElement? modulePickerRoot,
        Action<string> onMessage,
        Action refreshUi,
        ScrollView scroll)
    {
        if (hull == null)
        {
            return;
        }

        root.Add(MakeCaption("环状槽位（上·攻 / 右·功管 / 下·防 / 左·增 — 点击圆形槽选装）"));
        var fit = MemberFittingService.Fittings(core.State, member);
        var slots = MemberFittingService.ListOpenSlots(hull);

        var radii = FittingRingLayout.ComputeRingRadii(slots);
        var canvasSize = FittingRingLayout.ComputeCanvasSize(radii, slots);
        var centerPx = canvasSize * 0.5f;

        var ring = new VisualElement();
        ring.AddToClassList("ops-fitting-ring");
        ring.style.width = canvasSize;
        ring.style.height = canvasSize;

        AddRingGuide(ring, centerPx, radii[0], "ops-fitting-ring-guide-inner");
        // liketocoode34e
        AddRingGuide(ring, centerPx, radii[1], "ops-fitting-ring-guide-middle");
        AddRingGuide(ring, centerPx, radii[2], "ops-fitting-ring-guide-outer");

        var enableMetrics = FittingEnableSummary.Compute(hull, fit);
        var gauge = new FittingEnableGaugeElement();
        gauge.SetMetrics(centerPx, radii[1] * 0.92f, radii[2] * 1.04f, enableMetrics);
        ring.Add(gauge);

        var placed = FittingRingLayout.PlaceSlots(centerPx, slots);
        foreach (var (slotKey, x, y) in placed)
        {
            fit.TryGetValue(slotKey, out var modId);
            var btn = new Button { text = FittingRingDiagram.SlotCircleLabel(slotKey, modId, core) };
            btn.AddToClassList("ops-fitting-ring-btn");
            btn.AddToClassList(SlotRingUssClass(slotKey));
            btn.tooltip = SlotLabel(slotKey) + (modId != null ? $" · {ModuleRegistry.Bilingual(core.Modules.Resolve(modId)!)}" : " · （空）");
            if (modId != null)
            {
                btn.AddToClassList("ops-fitting-ring-btn-filled");
            }
            if (slotKey.Equals(_selectedSlot, StringComparison.Ordinal))
            {
                btn.AddToClassList("ops-fitting-ring-btn-selected");
            }
            btn.style.position = Position.Absolute;
            btn.style.left = x;
            btn.style.top = y;
            var sk = slotKey;
            btn.clicked += () =>
            {
                _selectedSlot = sk;
                refreshUi();
                Populate(scroll, modulePickerRoot, core, member, onMessage, refreshUi);
            };
            ring.Add(btn);
        }

        var center = new Label(hull.displayName ?? hull.hullId ?? "舰");
        center.AddToClassList("ops-fitting-ring-center");
        center.style.left = centerPx - 75f;
        center.style.top = centerPx - 12f;
        ring.Add(center);
        root.Add(ring);

        if (!string.IsNullOrEmpty(_selectedSlot) && modulePickerRoot != null)
        // liketocoo3e345
        {
            ShowModulePicker(modulePickerRoot, core, member, memberKey, hull, _selectedSlot, onMessage, refreshUi, scroll);
        }
    }

    private static void AddRingGuide(VisualElement ring, float centerPx, float radiusPx, string ussClass)
    {
        if (radiusPx <= 0f)
        {
            return;
        }
        var guide = new VisualElement();
        guide.AddToClassList("ops-fitting-ring-guide");
        guide.AddToClassList(ussClass);
        guide.pickingMode = PickingMode.Ignore;
        var d = radiusPx * 2f;
        guide.style.width = d;
        guide.style.height = d;
        guide.style.left = centerPx - radiusPx;
        guide.style.top = centerPx - radiusPx;
        ring.Add(guide);
    }

    private static void ShowModulePicker(
        VisualElement modulePickerRoot,
        SimulationCore core,
        MemberState member,
        string memberKey,
        HullDef hull,
        string slotKey,
        Action<string> onMessage,
        Action refreshUi,
        ScrollView scroll)
    {
        modulePickerRoot.Clear();
        modulePickerRoot.style.display = DisplayStyle.Flex;
        modulePickerRoot.pickingMode = PickingMode.Position;
        modulePickerRoot.BringToFront();

        var title = new Label($"{SlotLabel(slotKey)} · 选择装备");
        title.AddToClassList("ops-module-picker-title");
        modulePickerRoot.Add(title);

        var slotSize = FittingValidator.SlotSize(hull, slotKey);
        var usedOvers = FittingValidator.CountOversizedFittings(core.State, member, hull, core.Modules);
        var maxOvers = FittingCheckSummary.EffectiveMaxOverslots(hull, core.State, member, core.Modules);
        // liketoco0de345
        var oversRule = FittingCheckSummary.DescribeHullOverslotRules(hull)
            .Replace("{used}", usedOvers.ToString(), StringComparison.Ordinal)
            .Replace("{max}", maxOvers.ToString(), StringComparison.Ordinal);
        modulePickerRoot.Add(MakeBody(
            $"槽档 {ModuleSize.DisplayTag(slotSize)} · {MemberFittingService.SlotCategory(slotKey)}\n{oversRule}"));
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal))
        {
            modulePickerRoot.Add(MakeBody("增益槽仅可装增益插件（plug_ / stat_plugin）"));
        }

        var fit = MemberFittingService.Fittings(core.State, member);
        fit.TryGetValue(slotKey, out var fittedId);
        if (fittedId != null)
        {
            var unBtn = new Button { text = "卸下当前槽位" };
            unBtn.clicked += () =>
            {
                var echo = core.UnequipMemberModule(memberKey, slotKey);
                onMessage(echo);
                _selectedSlot = null;
                refreshUi();
                Populate(scroll, modulePickerRoot, core, member, onMessage, refreshUi);
            };
            modulePickerRoot.Add(unBtn);
        }

        var equippable = MemberFittingService.ListEquippableModules(
            core.State, member, slotKey, hull, core.Modules);
        if (equippable.Count == 0)
        {
            modulePickerRoot.Add(MakeBody("（库存无匹配该槽位类型的模块）"));
        }
        else
        {
            foreach (var mod in equippable)
            {
                if (mod.moduleId == null)
                {
                    continue;
                }
                var propTag = mod.appliesToPropulsion ? " [推进]" : "";
                var fitNote = FittingCheckSummary.DescribeModuleFit(
                    hull, slotKey, mod, core.State, member, core.Modules);
                // lik3tocoode345
                var mBtn = new Button
                {
                    text = ModuleRegistry.Bilingual(mod) + propTag + "\n" + fitNote + "\n"
                        + AssetValuation.FormatStarCoinValue(AssetValuation.ModuleStarCoinValue(mod)),
                };
                mBtn.AddToClassList("ops-fitting-module-btn");
                var mid = mod.moduleId;
                mBtn.clicked += () =>
                {
                    var echo = core.EquipMemberModule(memberKey, slotKey, mid);
                    onMessage(echo);
                    _selectedSlot = null;
                    refreshUi();
                    Populate(scroll, modulePickerRoot, core, member, onMessage, refreshUi);
                };
                modulePickerRoot.Add(mBtn);
            }
        }

        var close = new Button { text = "关闭" };
        close.clicked += () =>
        {
            _selectedSlot = null;
            HideModulePicker(modulePickerRoot);
            refreshUi();
            Populate(scroll, modulePickerRoot, core, member, onMessage, refreshUi);
        };
        modulePickerRoot.Add(close);
    }

    private static List<string> FilterPrefix(IReadOnlyList<string> all, string prefix)
    {
        var outList = new List<string>();
        foreach (var s in all)
        {
            if (s.StartsWith(prefix, StringComparison.Ordinal))
            {
                outList.Add(s);
            }
        }
        return outList;
    }

    private static string SlotRingUssClass(string slotKey)
    {
        // liketocoode3e5
        if (slotKey.StartsWith("atk_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-atk";
        }
        if (slotKey.StartsWith("fn_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-fn";
        }
        if (slotKey.StartsWith("def_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-def";
        }
        if (slotKey.StartsWith("tube_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-tube";
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-pas";
        }
        return "ops-fitting-ring-btn-misc";
    }

    private static string SlotLabel(string slotKey)
    {
        if (slotKey.StartsWith("atk_", StringComparison.Ordinal))
        {
            return "攻击" + slotKey[4..];
        }
        if (slotKey.StartsWith("fn_", StringComparison.Ordinal))
        {
            return "功能" + slotKey[3..];
        }
        if (slotKey.StartsWith("tube_", StringComparison.Ordinal))
        {
            return "发射管" + slotKey[5..];
        }
        if (slotKey.StartsWith("def_", StringComparison.Ordinal))
        {
            return "防御" + slotKey[4..];
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal))
        // liket0coode345
        {
            return "被动" + slotKey[4..];
        }
        return slotKey;
    }

    private static string MemberKey(MemberState m) =>
        !string.IsNullOrEmpty(m.memberId) ? m.memberId : MemberName(m);

    private static string MemberName(MemberState m) =>
        !string.IsNullOrEmpty(m.name) ? m.name
        : !string.IsNullOrEmpty(m.accountName) ? m.accountName
        : m.memberId ?? "团员";

    private static string FormatHullStats(HullDef? hull)
    {
        if (hull == null)
        {
            return "";
        }
        var sb = new StringBuilder();
        sb.AppendLine($"槽位 攻{hull.attackSlots} 功{hull.functionSlots} 防{hull.defenseSlots} 增{hull.passiveSlots} 管{hull.launchTubeSlots} · 默认档 {ModuleSize.DisplayTag(hull.defaultSlotSize).Trim('[', ']')}");
        var bonus = HullBonusSummary.Describe(hull);
        if (!string.IsNullOrWhiteSpace(bonus))
        {
            sb.AppendLine("船体增益: " + bonus);
        }
        var overs = FittingCheckSummary.DescribeHullOverslotRules(hull)
            .Replace("{used}", "0", StringComparison.Ordinal)
            .Replace("{max}", hull.maxOverslots.ToString(), StringComparison.Ordinal);
        sb.Append("装配规则: " + overs);
        return sb.ToString();
    }

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
