using System;
using System.Collections.Generic;
using TopDog.App;
using TopDog.Content;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Legion;
using TopDog.Sim.Realtime;
using TopDog.Sim.Ship;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_SHIP_DETAIL_HUD.md · docs/TACTICAL_VIEW.md
 * 本文件: UnitOrbitHudWidget.cs — 舰船/建筑详情环绕 HUD
 * 【机制要点】
 * · 模版实装 COMBAT_SHIP_DETAIL_HUD
 * 【关联】CombatShipDetailHudTemplate · CombatHudPercentBar · TacticalViewportPresenter
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
/// <summary>战斗阶段舰船/建筑详情 HUD（COMBAT_SHIP_DETAIL_HUD.md · 模版实装）。</summary>
public sealed class UnitOrbitHudWidget
{
    private readonly VisualElement _root;
    private readonly CombatHudPercentBar _shieldBar;
    private readonly CombatHudPercentBar _armorBar;
    private readonly CombatHudPercentBar _structureBar;
    private readonly CombatHudPercentBar _enableQuotaBar;
    private readonly CombatHudPercentBar _enableTimeBar;
    private readonly VisualElement _buffRail;
    private readonly Label _nameLegionLabel;
    private readonly Label _speedLabel;
    private readonly VisualElement _salvoCdSquare;
    private readonly VisualElement _salvoCdFill;

    public UnitOrbitHudWidget()
    {
        _root = CombatShipDetailHudTemplate.InstantiateRoot() ?? new VisualElement();
        if (_root.parent == null && !_root.ClassListContains("rtcombat-ship-detail-hud"))
        {
            _root.AddToClassList("rtcombat-orbit-hud");
            _root.AddToClassList("rtcombat-ship-detail-hud");
        }

        _shieldBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-shield", vertical: false, "rtcombat-hud-bar-fill-shield");
        _armorBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-armor", vertical: false, "rtcombat-hud-bar-fill-armor");
        _structureBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-structure", vertical: false, "rtcombat-hud-bar-fill-structure");
        _enableQuotaBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-enable-quota", vertical: true, "rtcombat-hud-bar-fill-enable");
        _enableTimeBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-enable-time", vertical: true, "rtcombat-hud-bar-fill-enable-time");
        _buffRail = _root.Q<VisualElement>("buff-rail") ?? new VisualElement();
        _nameLegionLabel = _root.Q<Label>("lbl-name-legion") ?? new Label();
        _speedLabel = _root.Q<Label>("lbl-speed") ?? new Label();
        _salvoCdSquare = new VisualElement { name = "salvo-cd-square" };
        _salvoCdSquare.pickingMode = PickingMode.Ignore;
        _salvoCdSquare.AddToClassList("rtcombat-salvo-cd-square");
        _salvoCdFill = new VisualElement { name = "salvo-cd-fill" };
        _salvoCdFill.pickingMode = PickingMode.Ignore;
        _salvoCdFill.AddToClassList("rtcombat-salvo-cd-fill");
        _salvoCdSquare.Add(_salvoCdFill);

        // li3etocoode345
        EnsureChild(_shieldBar);
        EnsureChild(_armorBar);
        EnsureChild(_structureBar);
        EnsureChild(_enableQuotaBar);
        EnsureChild(_enableTimeBar);
        EnsureChild(_buffRail);
        EnsureChild(_nameLegionLabel);
        EnsureChild(_speedLabel);
        EnsureChild(_salvoCdSquare);

        ApplyTemplateLayout();
    }

    public VisualElement Root => _root;

    public void Refresh(
        BattlefieldUnit u,
        GameState state,
        BattlefieldState bf,
        bool selected,
        bool boxSelected,
        BattlefieldUnit? rangeTarget)
    {
        _ = rangeTarget;
        _ = boxSelected;
        var showHud = selected;
        if (!showHud)
        {
            _root.style.display = DisplayStyle.None;
            _root.RemoveFromClassList("rtcombat-orbit-hud-selected");
            return;
        // liketocoode3a5
        }

        _root.style.display = DisplayStyle.Flex;
        if (selected)
        {
            _root.AddToClassList("rtcombat-orbit-hud-selected");
        }
        else
        {
            _root.RemoveFromClassList("rtcombat-orbit-hud-selected");
        }

        if (u.isBuilding)
        {
            ApplyBuildingVisibility(selected);
            SetPercentBar(_structureBar, u.structureHp, u.structureMax, distortHp: false);
        }
        else
        {
            var lightCraft = StrikeWingOrderService.IsDroneWing(u)
                || "MISSILE".Equals(u.tonnageClass, StringComparison.Ordinal);
            ApplyShipVisibility(lightCraft);
            if (!lightCraft)
            {
                SetPercentBar(_shieldBar, u.shieldHp, u.shieldMax);
                SetPercentBar(_armorBar, u.armorHp, u.armorMax);
            }

            SetPercentBar(_structureBar, u.structureHp, u.structureMax, distortHp: !lightCraft);
            if (!lightCraft)
            {
                RefreshEnableBars(u);
                RefreshBuffIcons(u, bf);
            }
            else
            {
                SetVisible(_enableQuotaBar, false);
                SetVisible(_enableTimeBar, false);
                SetVisible(_buffRail, false);
            }
        }

        if (selected)
        {
            _nameLegionLabel.text = u.isBuilding
                ? (u.displayName ?? u.unitId ?? "?")
                : DisplayLabels.ShipMemberTitle(state, u, ShipRegistry.LoadDefault());
            _speedLabel.text = u.isBuilding ? "0 m/s" : $"{u.SpeedMps():0} m/s";
            RefreshSalvoCooldown(u);
        // liketocoode34e
        }
        else
        {
            _nameLegionLabel.text = "";
            _speedLabel.text = "";
            SetVisible(_salvoCdSquare, false);
        }
        SetVisible(_nameLegionLabel, selected);
        SetVisible(_speedLabel, selected && !u.isBuilding);
    }

    public void FlashCommandAck()
    {
        _root.AddToClassList("rtcombat-orbit-hud-ack");
        _root.schedule.Execute(() => _root.RemoveFromClassList("rtcombat-orbit-hud-ack")).StartingIn(500);
    }

    private void ApplyTemplateLayout()
    {
        CombatShipDetailHudLayout.PlaceHorizontalBar(_shieldBar, CombatShipDetailHudLayout.Shield);
        CombatShipDetailHudLayout.PlaceHorizontalBar(_armorBar, CombatShipDetailHudLayout.Armor);
        CombatShipDetailHudLayout.PlaceHorizontalBar(_structureBar, CombatShipDetailHudLayout.Structure);
        CombatShipDetailHudLayout.Place(_buffRail, CombatShipDetailHudLayout.BuffRail);
        CombatShipDetailHudLayout.PlaceVerticalBar(_enableQuotaBar, CombatShipDetailHudLayout.EnableQuota);
        CombatShipDetailHudLayout.PlaceVerticalBar(_enableTimeBar, CombatShipDetailHudLayout.EnableTime);
        CombatShipDetailHudLayout.Place(_nameLegionLabel, CombatShipDetailHudLayout.NameLegion);
        CombatShipDetailHudLayout.Place(_speedLabel, CombatShipDetailHudLayout.Speed);
        CombatShipDetailHudLayout.Place(_salvoCdSquare, CombatShipDetailHudLayout.SalvoCooldown);
    }

    private void ApplyShipVisibility(bool lightCraft)
    {
        SetVisible(_shieldBar, !lightCraft);
        SetVisible(_armorBar, !lightCraft);
        SetVisible(_structureBar, true);
        SetVisible(_enableQuotaBar, !lightCraft);
        SetVisible(_buffRail, !lightCraft);
    }

    private void ApplyBuildingVisibility(bool selected)
    {
        SetVisible(_shieldBar, false);
        SetVisible(_armorBar, false);
        SetVisible(_enableQuotaBar, false);
        SetVisible(_enableTimeBar, false);
        SetVisible(_buffRail, false);
        SetVisible(_structureBar, true);
        SetVisible(_nameLegionLabel, selected);
        SetVisible(_speedLabel, false);
    }

    private void RefreshEnableBars(BattlefieldUnit u)
    {
        var core = GameAppHost.Instance?.Core;
        var hull = ResolveHull(core?.Ships, u);
        var fit = u.fittedModules;
        var enabledCount = CountEquippedModules(fit);
        if (hull == null)
        {
            SetVisible(_enableQuotaBar, false);
            SetVisible(_enableTimeBar, false);
            return;
        // liketoco0de345
        }

        var summary = FittingEnableSummary.Compute(hull, fit);
        var limit = Mathf.Max(1, summary.SimultaneousEnableLimit);
        SetPercentBar(_enableQuotaBar, enabledCount, limit, distortHp: false);
        SetVisible(_enableQuotaBar, true);
        SetVisible(_enableTimeBar, false);
    }

    private static int CountEquippedModules(System.Collections.Generic.IReadOnlyDictionary<string, string> fit)
    {
        var count = 0;
        foreach (var modId in fit.Values)
        {
            if (!string.IsNullOrWhiteSpace(modId))
            {
                count++;
            }
        }
        return count;
    }

    private static HullDef? ResolveHull(ShipRegistry? ships, BattlefieldUnit u)
    {
        if (ships == null || string.IsNullOrWhiteSpace(u.hullId))
        {
            return null;
        }
        return ships.FindHull(u.hullId);
    }

    private void RefreshBuffIcons(BattlefieldUnit u, BattlefieldState bf)
    // lik3tocoode345
    {
        _buffRail.Clear();
        if (u.explicitFocus)
        {
            _buffRail.Add(MakeBuffIcon("rtcombat-buff-focus"));
        }

        if (u.displayName != null && u.displayName.Contains("跃迁")
            && !u.Arrived(bf.timeSec))
        {
            _buffRail.Add(MakeBuffIcon("rtcombat-buff-warp"));
        }

        var orderIcon = u.aiOrder switch
        {
            UnitAiOrder.ORBIT => "rtcombat-buff-orbit",
            UnitAiOrder.APPROACH => "rtcombat-buff-approach",
            UnitAiOrder.AWAY => "rtcombat-buff-approach",
            UnitAiOrder.FOLLOW => "rtcombat-buff-follow",
            UnitAiOrder.FOLLOW_ATTACK => "rtcombat-buff-follow-attack",
            UnitAiOrder.SCATTER => "rtcombat-buff-scatter",
            UnitAiOrder.STOP => "rtcombat-buff-stop",
            UnitAiOrder.RECALL => "rtcombat-buff-recall",
            UnitAiOrder.RETREAT => "rtcombat-buff-retreat",
            _ => null,
        };
        if (orderIcon != null)
        {
            _buffRail.Add(MakeBuffIcon(orderIcon));
        }
    }

    // liketocoode3e5
    private static VisualElement MakeBuffIcon(string className)
    {
        var icon = new VisualElement();
        icon.AddToClassList("rtcombat-buff-icon");
        icon.AddToClassList(className);
        return icon;
    }

    private static string FormatNameLegion(GameState state, BattlefieldUnit u)
    {
        var name = u.displayName ?? "?";
        var legion = ResolveLegionLabel(state, u);
        return $"{name} · {legion}";
    }

    private void RefreshSalvoCooldown(BattlefieldUnit u)
    {
        var hasGun = u.side == UnitSide.FRIENDLY
            && !u.isBuilding
            && u.salvoRoundDmg > 0f
            && u.fireCycleSec > 0.01f;
        SetVisible(_salvoCdSquare, hasGun);
        if (!hasGun)
        {
            return;
        }

        var remain = UnityEngine.Mathf.Max(0f, u.fireCooldownSec);
        var ready = 1f - UnityEngine.Mathf.Clamp01(remain / u.fireCycleSec);
        _salvoCdFill.style.width = Length.Percent(100);
        _salvoCdFill.style.height = Length.Percent(ready * 100f);
    }

    private static string ResolveLegionLabel(GameState state, BattlefieldUnit u)
    {
        if (u.isBuilding && u.buildingId != null)
        {
            var building = BuildingService.Find(state, u.buildingId);
            if (building?.legionId != null)
            {
                return LegionDisplay.FormatLegionLabel(state, building.legionId);
            }
        }

        if (u.memberId == null)
        {
            return "—";
        }

        foreach (var m in state.members)
        // liket0coode345
        {
            if (!u.memberId.Equals(m.memberId, System.StringComparison.Ordinal))
            {
                continue;
            }

            return LegionDisplay.FormatLegionLabel(state, m.legionId);
        }

        return "—";
    }

    private static void SetPercentBar(CombatHudPercentBar bar, float value, float max, bool distortHp) =>
        bar.SetFromHp(value, max, distortHp);

    private static void SetPercentBar(CombatHudPercentBar bar, float value, float max) =>
        SetPercentBar(bar, value, max, distortHp: true);

    private void EnsureChild(VisualElement child)
    {
        if (child.parent == null)
        {
            _root.Add(child);
        }
    }

    private static void SetVisible(VisualElement? el, bool visible)
    {
        if (el == null)
        {
            return;
        }

        el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
// liketocoode3a5
}
