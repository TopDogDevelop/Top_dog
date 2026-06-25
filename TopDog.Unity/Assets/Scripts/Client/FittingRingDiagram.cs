using System;
using System.Collections.Generic;
using TopDog.App;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Read-only or interactive fitting ring diagram (shared by配船 overlay and团员详情).</summary>
public static class FittingRingDiagram
{
    public static VisualElement BuildMini(SimulationCore core, MemberState member, HullDef hull, float maxWidthPx = 248f)
    {
        var fit = MemberFittingService.Fittings(core.State, member);
        var slots = MemberFittingService.ListOpenSlots(hull);
        var radii = FittingRingLayout.ComputeRingRadii(slots);
        var canvasSize = FittingRingLayout.ComputeCanvasSize(radii, slots);
        var scale = Mathf.Min(0.38f, maxWidthPx / canvasSize);
        var scaledSize = canvasSize * scale;

        var wrap = new VisualElement();
        wrap.AddToClassList("ops-fitting-ring-mini-wrap");
        wrap.style.width = scaledSize;
        wrap.style.height = scaledSize;

        var ring = BuildRing(core, member, hull, fit, slots, radii, canvasSize, interactive: false);
        ring.style.scale = new Scale(new Vector3(scale, scale, 1f));
        ring.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(0));
        wrap.Add(ring);
        return wrap;
    }

    public static VisualElement BuildInteractive(
        SimulationCore core,
        MemberState member,
        HullDef hull,
        string? selectedSlot,
        Action<string> onSlotSelected)
    {
        var fit = MemberFittingService.Fittings(core.State, member);
        var slots = MemberFittingService.ListOpenSlots(hull);
        var radii = FittingRingLayout.ComputeRingRadii(slots);
        var canvasSize = FittingRingLayout.ComputeCanvasSize(radii, slots);
        return BuildRing(core, member, hull, fit, slots, radii, canvasSize, interactive: true, selectedSlot, onSlotSelected);
    }

    private static VisualElement BuildRing(
        SimulationCore core,
        MemberState member,
        HullDef hull,
        Dictionary<string, string> fit,
        List<string> slots,
        float[] radii,
        float canvasSize,
        bool interactive,
        string? selectedSlot = null,
        Action<string>? onSlotSelected = null)
    {
        var centerPx = canvasSize * 0.5f;
        var ring = new VisualElement();
        ring.AddToClassList("ops-fitting-ring");
        ring.style.width = canvasSize;
        ring.style.height = canvasSize;

        AddRingGuide(ring, centerPx, radii[0], "ops-fitting-ring-guide-inner");
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
            var label = SlotCircleLabel(slotKey, modId, core);
            VisualElement slotEl;
            if (interactive)
            {
                var btn = new Button { text = label };
                btn.clicked += () => onSlotSelected?.Invoke(slotKey);
                slotEl = btn;
            }
            else
            {
                slotEl = new Label(label);
                slotEl.pickingMode = PickingMode.Ignore;
            }
            slotEl.AddToClassList("ops-fitting-ring-btn");
            if (!interactive)
            {
                slotEl.AddToClassList("ops-fitting-ring-btn-mini");
            }
            slotEl.AddToClassList(SlotRingUssClass(slotKey));
            if (modId != null)
            {
                slotEl.AddToClassList("ops-fitting-ring-btn-filled");
            }
            if (slotKey.Equals(selectedSlot, StringComparison.Ordinal))
            {
                slotEl.AddToClassList("ops-fitting-ring-btn-selected");
            }
            slotEl.tooltip = SlotLabel(slotKey) + (modId != null
                ? " · " + ModuleRegistry.Bilingual(core.Modules.Resolve(modId))
                : " · （空）");
            slotEl.style.position = Position.Absolute;
            slotEl.style.left = x;
            slotEl.style.top = y;
            ring.Add(slotEl);
        }

        var center = new Label(hull.displayName ?? hull.hullId ?? "舰");
        center.AddToClassList("ops-fitting-ring-center");
        center.pickingMode = PickingMode.Ignore;
        center.style.left = centerPx - 75f;
        center.style.top = centerPx - 12f;
        ring.Add(center);
        return ring;
    }

    public static string SlotCircleLabel(string slotKey, string? modId, SimulationCore core)
    {
        var tag = SlotCategoryAbbr(slotKey);
        if (modId == null)
        {
            return tag;
        }

        var mod = core.Modules.Resolve(modId);
        var shortName = ModuleShortLabel(mod, modId);
        return string.IsNullOrEmpty(shortName) ? tag + "●" : tag + "\n" + shortName;
    }

    private static string ModuleShortLabel(TopDog.Content.Modules.ModuleDef? mod, string modId)
    {
        if (mod != null)
        {
            var zh = ModuleCatalog.DisplayNameZh(mod);
            if (!string.IsNullOrEmpty(zh))
            {
                return zh.Length <= 4 ? zh : zh[..4];
            }
            if (!string.IsNullOrEmpty(mod.displayName))
            {
                return mod.displayName.Length <= 4 ? mod.displayName : mod.displayName[..4];
            }
        }

        return modId.Length <= 4 ? modId : modId[..4];
    }

    private static string SlotCategoryAbbr(string slotKey)
    {
        if (slotKey.StartsWith("atk_", StringComparison.Ordinal))
        {
            return "攻" + slotKey[4..];
        }
        if (slotKey.StartsWith("fn_", StringComparison.Ordinal))
        {
            return "功" + slotKey[3..];
        }
        if (slotKey.StartsWith("def_", StringComparison.Ordinal))
        {
            return "防" + slotKey[4..];
        }
        if (slotKey.StartsWith("tube_", StringComparison.Ordinal))
        {
            return "管" + slotKey[5..];
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal))
        {
            return "增" + slotKey[4..];
        }
        return "?";
    }

    private static string SlotLabel(string slotKey)
    {
        if (slotKey.StartsWith("atk_", StringComparison.Ordinal))
        {
            return "攻击 " + slotKey[4..];
        }
        if (slotKey.StartsWith("fn_", StringComparison.Ordinal))
        {
            return "功能 " + slotKey[3..];
        }
        if (slotKey.StartsWith("tube_", StringComparison.Ordinal))
        {
            return "发射管 " + slotKey[5..];
        }
        if (slotKey.StartsWith("def_", StringComparison.Ordinal))
        {
            return "防御 " + slotKey[4..];
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal))
        {
            return "增益 " + slotKey[4..];
        }
        return slotKey;
    }

    private static string SlotRingUssClass(string slotKey)
    {
        if (slotKey.StartsWith("atk_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-atk";
        }
        if (slotKey.StartsWith("fn_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-fn";
        }
        if (slotKey.StartsWith("tube_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-tube";
        }
        if (slotKey.StartsWith("def_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-def";
        }
        if (slotKey.StartsWith("pas_", StringComparison.Ordinal))
        {
            return "ops-fitting-ring-btn-pas";
        }
        return "ops-fitting-ring-btn-fn";
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
}
