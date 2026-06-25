using System.Collections.Generic;
using System.Linq;
using TopDog.Content;
using TopDog.Sim.Possession;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/// <summary>右栏双模式：可降临战场 / 物体总览（TACTICAL_VIEW.md §3）。</summary>
public sealed class TacticalRightRail
{
    private readonly ScrollView _battlefieldScroll;
    private readonly ScrollView _objectScroll;
    private readonly Button _toggleBtn;
    private readonly VisualElement _battlefieldContent;
    private readonly VisualElement _objectContent;

    public TacticalRightRail(VisualElement root)
    {
        _battlefieldScroll = root.Q<ScrollView>("vision-rail-scroll");
        _objectScroll = root.Q<ScrollView>("object-overview-scroll");
        _toggleBtn = root.Q<Button>("btn-rail-mode-toggle");
        _battlefieldContent = _battlefieldScroll?.contentContainer;
        _objectContent = _objectScroll?.contentContainer;

        if (_toggleBtn != null)
        {
            _toggleBtn.clicked += () =>
            {
                TacticalSelectionState.ToggleRailMode();
                ApplyModeVisibility();
                RefreshToggleLabel();
            };
        }

        TacticalSelectionState.RailModeChanged += ApplyModeVisibility;
        ApplyModeVisibility();
        RefreshToggleLabel();
    }

    public void Refresh(GameState state)
    {
        RefreshBattlefields(state);
        RefreshObjects(state);
        RefreshToggleLabel();
    }

    private void ApplyModeVisibility()
    {
        var bf = TacticalSelectionState.RightRailMode == TacticalRightRailMode.Battlefield;
        if (_battlefieldScroll != null)
        {
            _battlefieldScroll.style.display = bf ? DisplayStyle.Flex : DisplayStyle.None;
        }
        if (_objectScroll != null)
        {
            _objectScroll.style.display = bf ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    private void RefreshToggleLabel()
    {
        if (_toggleBtn == null)
        {
            return;
        }
        _toggleBtn.text = TacticalSelectionState.RightRailMode == TacticalRightRailMode.Battlefield
            ? "切换：物体总览"
            : "切换：可降临战场";
    }

    private void RefreshBattlefields(GameState state)
    {
        if (_battlefieldContent == null)
        {
            return;
        }
        _battlefieldContent.Clear();
        var visible = VisionGate.ListVisibleBattlefields(state);
        if (visible.Count == 0)
        {
            _battlefieldContent.Add(MakeHint("无有视野战场"));
            return;
        }

        var bySystem = new Dictionary<string, List<BattlefieldState>>();
        foreach (var bf in visible)
        {
            var key = bf.systemId ?? "?";
            if (!bySystem.TryGetValue(key, out var list))
            {
                list = new List<BattlefieldState>();
                bySystem[key] = list;
            }
            list.Add(bf);
        }

        foreach (var kv in bySystem)
        {
            _battlefieldContent.Add(MakeGroupHeader(MapLocationFormatter.FormatSystemPath(state, kv.Key)));
            foreach (var bf in kv.Value)
            {
                var label = MapLocationFormatter.FormatBattlefield(state, bf);
                var btn = new Button { text = label };
                btn.AddToClassList("rtcombat-rail-item");
                if (bf.battlefieldId != null && bf.battlefieldId.Equals(state.activeBattlefieldId, System.StringComparison.Ordinal))
                {
                    btn.AddToClassList("rtcombat-rail-item-active");
                }
                var bfId = bf.battlefieldId;
                btn.clicked += () =>
                {
                    if (bfId != null)
                    {
                        PossessionService.SwitchBattlefield(state, bfId);
                        TacticalSelectionState.ClearOnBattlefieldSwitch();
                    }
                };
                _battlefieldContent.Add(btn);
            }
        }
    }

    private void RefreshObjects(GameState state)
    {
        if (_objectContent == null)
        {
            return;
        }
        _objectContent.Clear();
        var bf = FindActiveBattlefield(state);
        if (bf == null)
        {
            _objectContent.Add(MakeHint("无活跃战场"));
            return;
        }

        var inbound = bf.units
            .Where(u => !u.IsDestroyed() && !u.Arrived(bf.timeSec))
            .OrderBy(u => u.displayName ?? u.unitId)
            .ToList();
        if (inbound.Count > 0)
        {
            _objectContent.Add(MakeGroupHeader("跃迁中"));
            foreach (var u in inbound)
            {
                _objectContent.Add(BuildObjectRow(u, bf, indent: false, warp: true));
            }
        }

        var units = bf.units
            .Where(u => !u.IsDestroyed() && u.Arrived(bf.timeSec))
            .OrderBy(u => u.side == UnitSide.ENEMY ? 0 : u.side == UnitSide.FRIENDLY ? 1 : 2)
            .ThenBy(u => u.parentUnitId != null ? 1 : 0)
            .ThenBy(u => u.displayName ?? u.unitId)
            .ToList();

        var groups = new Dictionary<string, List<BattlefieldUnit>>();
        foreach (var u in units)
        {
            var key = u.isBuilding ? "BUILDING" : (u.tonnageClass ?? "UNKNOWN");
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<BattlefieldUnit>();
                groups[key] = list;
            }
            list.Add(u);
        }

        foreach (var kv in groups.OrderBy(g => g.Key == "BUILDING" ? 1 : 0).ThenBy(g => g.Key))
        {
            _objectContent.Add(MakeGroupHeader(TacticalIconCatalog.GroupLabel(kv.Key)));
            foreach (var u in kv.Value)
            {
                _objectContent.Add(BuildObjectRow(u, bf, indent: u.parentUnitId != null, warp: false));
            }
        }
    }

    private VisualElement BuildObjectRow(BattlefieldUnit u, BattlefieldState bf, bool indent, bool warp)
    {
        var row = new VisualElement();
        row.AddToClassList("rtcombat-rail-object-row");
        if (indent)
        {
            row.AddToClassList("rtcombat-rail-object-indent");
        }
        row.pickingMode = PickingMode.Position;
        row.focusable = true;
        if (u.unitId != null && u.unitId.Equals(TacticalSelectionState.SelectedTargetUnitId, System.StringComparison.Ordinal))
        {
            row.AddToClassList("rtcombat-rail-item-active");
        }

        var iconHost = new VisualElement();
        iconHost.AddToClassList("rtcombat-rail-icon");
        iconHost.pickingMode = PickingMode.Ignore;
        var tex = TacticalIconCatalog.ResolveShipIcon(u.tonnageClass);
        if (tex != null)
        {
            iconHost.style.backgroundImage = new StyleBackground(tex);
        }
        row.Add(iconHost);

        if (!u.isBuilding)
        {
            var badge = new Label(u.side == UnitSide.ENEMY ? "−" : "+");
            badge.pickingMode = PickingMode.Ignore;
            badge.AddToClassList(u.side == UnitSide.ENEMY ? "rtcombat-rail-badge-hostile" : "rtcombat-rail-badge-friendly");
            iconHost.Add(badge);
        }

        var labelText = warp
            ? (u.displayName ?? u.unitId ?? "?")
            : DisplayLabels.TonnageBilingual(u.tonnageClass) + " · " + (u.displayName ?? u.unitId ?? "?");
        var name = new Label(labelText);
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("rtcombat-rail-name");
        row.Add(name);

        var uid = u.unitId;
        row.RegisterCallback<ClickEvent>(_ =>
        {
            TacticalSelectionState.SetSelectedTarget(uid);
        });
        return row;
    }

    private static BattlefieldState? FindActiveBattlefield(GameState state)
    {
        if (state.activeBattlefieldId == null)
        {
            return null;
        }
        foreach (var bf in state.battlefields)
        {
            if (state.activeBattlefieldId.Equals(bf.battlefieldId, System.StringComparison.Ordinal))
            {
                return bf;
            }
        }
        return null;
    }

    private static Label MakeGroupHeader(string text)
    {
        var l = new Label(text);
        l.AddToClassList("rtcombat-rail-group");
        return l;
    }

    private static Label MakeHint(string text)
    {
        var l = new Label(text);
        l.AddToClassList("rtcombat-subtitle");
        return l;
    }
}
