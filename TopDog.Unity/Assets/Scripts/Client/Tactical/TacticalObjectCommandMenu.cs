using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.App;
using TopDog.Content;
using TopDog.Content.Traits;
using TopDog.Sim.Combat;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §7 点选 · docs/TACTICAL_WARP_AND_ORDERS.md §3
 * 本文件: TacticalObjectCommandMenu.cs — 点选单位快捷指令菜单
 * 【机制要点】
 * · AlignMenuAboveIcon：菜单在锚点上方，左下角对准图标中心
 * · ShowAt：视口 PickUnitAt 屏幕中心；右栏物体行共用同一菜单实例
 * 【关联】FleetOrderService · CombatRealtimeController · TacticalRightRail
 * ══
 */

/// <summary>点击单位弹出的快捷指令菜单（舰队指令 + 词条主动技）。</summary>
public sealed class TacticalObjectCommandMenu
{
    private readonly VisualElement _root;
    private readonly Func<SimulationCore?> _core;
    private readonly Action<string, bool> _status;
    private readonly Action? _onAfterCommand;
    private VisualElement? _panel;
    private string? _targetUnitId;
    private bool _allFriendly = true;

    public TacticalObjectCommandMenu(
        VisualElement root,
        Func<SimulationCore?> core,
        Action<string, bool> status,
        Action? onAfterCommand = null)
    {
        _root = root;
        _core = core;
        _status = status;
        _onAfterCommand = onAfterCommand;
    }

    public void ShowAt(Vector2 rootLocalPosition, string targetUnitId) =>
        ShowAtInternal(rootLocalPosition, targetUnitId);

    public void ShowAtWorld(Vector2 worldPosition, string targetUnitId) =>
        ShowAtInternal(_root.WorldToLocal(worldPosition), targetUnitId);

    private void ShowAtInternal(Vector2 anchorRootLocal, string targetUnitId)
    {
        Hide();
        _targetUnitId = targetUnitId;
        _allFriendly = true;

        _panel = new VisualElement();
        _panel.AddToClassList("rtcombat-object-command-menu");
        _panel.style.position = Position.Absolute;
        _panel.style.visibility = Visibility.Hidden;
        _panel.pickingMode = PickingMode.Position;

        var scopeRow = new VisualElement();
        scopeRow.AddToClassList("rtcombat-object-command-scope");
        var scopeLabel = new Label("指令范围");
        scopeLabel.AddToClassList("rtcombat-subtitle");
        scopeRow.Add(scopeLabel);
        var scopeAll = new Button { text = "全体" };
        var scopeSel = new Button { text = "选中" };
        scopeAll.AddToClassList("rtcombat-rail-item");
        scopeSel.AddToClassList("rtcombat-rail-item");
        scopeAll.AddToClassList("rtcombat-rail-item-active");
        scopeAll.clicked += () =>
        {
            _allFriendly = true;
            scopeAll.AddToClassList("rtcombat-rail-item-active");
            scopeSel.RemoveFromClassList("rtcombat-rail-item-active");
        };
        scopeSel.clicked += () =>
        {
            _allFriendly = false;
            scopeSel.AddToClassList("rtcombat-rail-item-active");
            scopeAll.RemoveFromClassList("rtcombat-rail-item-active");
        };
        scopeRow.Add(scopeAll);
        scopeRow.Add(scopeSel);
        _panel.Add(scopeRow);

        AddCommandButton("集火", () => Issue((s, bf, ships, sel) =>
            FleetOrderService.OrderFocus(s, bf, _targetUnitId, sel)));
        AddCommandButton("接近", () => Issue((s, bf, ships, sel) =>
            FleetOrderService.OrderApproach(s, bf, _targetUnitId, sel, TacticalSelectionState.DefaultCommandRangeKm)));
        AddCommandButton("远离", () => Issue((s, bf, ships, sel) =>
            FleetOrderService.OrderAway(s, bf, _targetUnitId, sel, TacticalSelectionState.DefaultCommandRangeKm)));
        AddCommandButton("环绕", () => Issue((s, bf, ships, sel) =>
            FleetOrderService.OrderOrbit(s, bf, _targetUnitId, sel, TacticalSelectionState.DefaultCommandRangeKm)));
        AddCommandButton("跃迁", () => Issue((s, bf, ships, sel) =>
            FleetOrderService.OrderWarpToSceneTarget(
                s, bf, _targetUnitId, ships, _allFriendly, sel, TacticalSelectionState.DefaultCommandRangeKm)));

        AppendActiveSkillButtons();

        _root.Add(_panel);
        _panel.BringToFront();
        AlignMenuAboveIcon(anchorRootLocal);
        _root.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
    }

    /// <summary>菜单左下角对准锚点（图标中心），整体在锚点上方。</summary>
    private void AlignMenuAboveIcon(Vector2 anchorRootLocal)
    {
        if (_panel == null)
        {
            return;
        }

        void Reposition(GeometryChangedEvent _)
        {
            if (_panel == null)
            {
                return;
            }

            var h = _panel.resolvedStyle.height;
            if (float.IsNaN(h) || h <= 0f)
            {
                return;
            }

            _panel.style.left = anchorRootLocal.x;
            _panel.style.top = anchorRootLocal.y - h;
            _panel.style.visibility = Visibility.Visible;
        }

        _panel.RegisterCallback<GeometryChangedEvent>(Reposition);
        _panel.schedule.Execute(() => Reposition(default)).StartingIn(0);
    }

    public void Hide()
    {
        if (_panel == null)
        {
            return;
        }

        _root.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
        _panel.RemoveFromHierarchy();
        _panel = null;
        _targetUnitId = null;
    }

    private void AppendActiveSkillButtons()
    {
        var core = _core();
        if (core == null || _panel == null || string.IsNullOrEmpty(_targetUnitId))
        {
            return;
        }

        var bf = ActiveBf(core.State);
        if (bf == null)
        {
            return;
        }

        BattlefieldUnit? unit = null;
        foreach (var u in bf.units)
        {
            if (_targetUnitId.Equals(u.unitId, StringComparison.Ordinal))
            {
                unit = u;
                break;
            }
        }

        if (unit == null || unit.side != UnitSide.FRIENDLY || unit.memberId == null
            || BattlefieldSceneProxyService.IsSceneProxy(unit))
        {
            return;
        }

        var skills = CombatActiveSkillGate.ListMemberActiveSkills(core.State, unit.memberId).ToList();
        if (skills.Count == 0)
        {
            return;
        }

        var header = new Label("词条主动技");
        header.AddToClassList("rtcombat-rail-group");
        _panel.Add(header);

        var catalog = TraitCatalog.LoadDefault();
        var memberId = unit.memberId;
        foreach (var skill in skills)
        {
            var trait = catalog.Find(skill.TraitId);
            var label = DisplayLabels.TraitBilingual(trait);
            if (skill.CooldownRounds > 0)
            {
                label += $" ({skill.CooldownRounds})";
            }

            var traitId = skill.TraitId;
            var btn = new Button { text = label };
            btn.AddToClassList("rtcombat-rail-item");
            btn.SetEnabled(skill.CanUse);
            btn.clicked += () =>
            {
                var msg = core.UseSuppressionSkill(traitId, memberId);
                var ok = msg.StartsWith("已", StringComparison.Ordinal)
                    || msg.Contains("已召唤", StringComparison.Ordinal)
                    || msg.Contains("已召来", StringComparison.Ordinal);
                _status(msg, ok);
                _onAfterCommand?.Invoke();
                Hide();
            };
            _panel.Add(btn);
        }
    }

    private void OnRootPointerDown(PointerDownEvent evt)
    {
        if (_panel == null)
        {
            return;
        }

        if (evt.target is VisualElement ve && _panel.Contains(ve))
        {
            return;
        }

        Hide();
    }

    private void AddCommandButton(string label, Action action)
    {
        if (_panel == null)
        {
            return;
        }

        var btn = new Button { text = label };
        btn.AddToClassList("rtcombat-rail-item");
        btn.clicked += () =>
        {
            action();
            Hide();
        };
        _panel.Add(btn);
    }

    private void Issue(Func<GameState, BattlefieldState, TopDog.Content.Ships.ShipRegistry, System.Collections.Generic.IReadOnlyCollection<string>?, string> action)
    {
        var core = _core();
        if (core == null)
        {
            _status("模拟未启动", false);
            return;
        }

        var s = core.State;
        var bf = ActiveBf(s);
        if (bf == null)
        {
            _status("无活跃战场", false);
            return;
        }

        if (string.IsNullOrEmpty(_targetUnitId))
        {
            _status("未选中目标", false);
            return;
        }

        TacticalSelectionState.SetSelectedTarget(_targetUnitId);
        var sel = _allFriendly ? null : TacticalSelectionState.GetSelectedFriendlyUnitIds();
        var msg = action(s, bf, core.Ships, sel);
        _status(msg, msg.StartsWith("已下令", StringComparison.Ordinal));
        _onAfterCommand?.Invoke();
    }

    private static BattlefieldState? ActiveBf(GameState s)
    {
        if (s.activeBattlefieldId == null)
        {
            return null;
        }

        foreach (var bf in s.battlefields)
        {
            if (s.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
    }
}
