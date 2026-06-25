using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/// <summary>战斗阶段舰船/建筑详情 HUD（COMBAT_SHIP_DETAIL_HUD.md · 模版实装）。</summary>
public sealed class UnitOrbitHudWidget
{
    private readonly VisualElement _root;
    private readonly CombatHudPercentBar _shieldBar;
    private readonly CombatHudPercentBar _armorBar;
    private readonly CombatHudPercentBar _structureBar;
    private readonly CombatHudPercentBar _capacitorBar;
    private readonly CombatHudPercentBar _implantBar;
    private readonly VisualElement _buffRail;
    private readonly Label _nameLegionLabel;
    private readonly Label _speedLabel;

    public UnitOrbitHudWidget()
    {
        _root = CombatShipDetailHudTemplate.InstantiateRoot() ?? new VisualElement();
        if (_root.parent == null && !_root.ClassListContains("rtcombat-ship-detail-hud"))
        {
            _root.AddToClassList("rtcombat-orbit-hud");
            _root.AddToClassList("rtcombat-ship-detail-hud");
        }

        _shieldBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-shield", vertical: false, "rtcombat-hud-bar-fill-hp");
        _armorBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-armor", vertical: false, "rtcombat-hud-bar-fill-hp");
        _structureBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-structure", vertical: false, "rtcombat-hud-bar-fill-hp");
        _capacitorBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-capacitor", vertical: true, "rtcombat-hud-bar-fill-capacitor");
        _implantBar = CombatHudPercentBar.ReplaceTemplateBar(_root, "bar-implant", vertical: true, "rtcombat-hud-bar-fill-implant");
        _buffRail = _root.Q<VisualElement>("buff-rail") ?? new VisualElement();
        _nameLegionLabel = _root.Q<Label>("lbl-name-legion") ?? new Label();
        _speedLabel = _root.Q<Label>("lbl-speed") ?? new Label();

        EnsureChild(_shieldBar);
        EnsureChild(_armorBar);
        EnsureChild(_structureBar);
        EnsureChild(_capacitorBar);
        EnsureChild(_implantBar);
        EnsureChild(_buffRail);
        EnsureChild(_nameLegionLabel);
        EnsureChild(_speedLabel);

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
        if (!selected)
        {
            _root.style.display = DisplayStyle.None;
            _root.RemoveFromClassList("rtcombat-orbit-hud-selected");
            return;
        }

        _root.style.display = DisplayStyle.Flex;
        _root.AddToClassList("rtcombat-orbit-hud-selected");

        if (u.isBuilding)
        {
            ApplyBuildingVisibility();
            SetPercentBar(_structureBar, u.structureHp, u.structureMax);
        }
        else
        {
            ApplyShipVisibility();
            SetPercentBar(_shieldBar, u.shieldHp, u.shieldMax);
            SetPercentBar(_armorBar, u.armorHp, u.armorMax);
            SetPercentBar(_structureBar, u.structureHp, u.structureMax);
            SetPercentBar(_capacitorBar, 0f, 1f, distortHp: false);
            SetPercentBar(_implantBar, 0f, 1f, distortHp: false);
            RefreshBuffIcons(u, bf);
        }

        _nameLegionLabel.text = FormatNameLegion(state, u);
        _speedLabel.text = u.isBuilding ? "0 m/s" : $"{u.SpeedMps():0} m/s";
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
        CombatShipDetailHudLayout.PlaceVerticalBar(_capacitorBar, CombatShipDetailHudLayout.Capacitor);
        CombatShipDetailHudLayout.PlaceVerticalBar(_implantBar, CombatShipDetailHudLayout.Implant);
        CombatShipDetailHudLayout.Place(_nameLegionLabel, CombatShipDetailHudLayout.NameLegion);
        CombatShipDetailHudLayout.Place(_speedLabel, CombatShipDetailHudLayout.Speed);
    }

    private void ApplyShipVisibility()
    {
        SetVisible(_shieldBar, true);
        SetVisible(_armorBar, true);
        SetVisible(_structureBar, true);
        SetVisible(_capacitorBar, true);
        SetVisible(_implantBar, true);
        SetVisible(_buffRail, true);
    }

    private void ApplyBuildingVisibility()
    {
        SetVisible(_shieldBar, false);
        SetVisible(_armorBar, false);
        SetVisible(_capacitorBar, false);
        SetVisible(_implantBar, false);
        SetVisible(_buffRail, false);
        SetVisible(_structureBar, true);
    }

    private void RefreshBuffIcons(BattlefieldUnit u, BattlefieldState bf)
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
            UnitAiOrder.FOLLOW => "rtcombat-buff-follow",
            UnitAiOrder.FOLLOW_ATTACK => "rtcombat-buff-follow-attack",
            UnitAiOrder.SCATTER => "rtcombat-buff-scatter",
            UnitAiOrder.STOP => "rtcombat-buff-stop",
            UnitAiOrder.RETREAT => "rtcombat-buff-retreat",
            _ => null,
        };
        if (orderIcon != null)
        {
            _buffRail.Add(MakeBuffIcon(orderIcon));
        }
    }

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
        return $"{name} · {ResolveLegionName(state, u)}";
    }

    private static string ResolveLegionName(GameState state, BattlefieldUnit u)
    {
        if (u.isBuilding && u.buildingId != null)
        {
            var building = BuildingService.Find(state, u.buildingId);
            if (building?.legionId != null)
            {
                return LegionDisplayName(state, building.legionId);
            }
        }

        if (u.memberId == null)
        {
            return "—";
        }

        foreach (var m in state.members)
        {
            if (!u.memberId.Equals(m.memberId, System.StringComparison.Ordinal))
            {
                continue;
            }

            return string.IsNullOrWhiteSpace(m.legionId) ? "—" : LegionDisplayName(state, m.legionId);
        }

        return "—";
    }

    private static string LegionDisplayName(GameState state, string legionId)
    {
        foreach (var legion in state.legions)
        {
            if (legionId.Equals(legion.legionId, System.StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(legion.displayName) ? legionId : legion.displayName;
            }
        }

        return legionId;
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
}
