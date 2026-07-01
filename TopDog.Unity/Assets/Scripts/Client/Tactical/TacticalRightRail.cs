using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.Content;
using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Sim.Possession;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §2 右栏双模式 · §物体总览分组
 * 本文件: TacticalRightRail.cs — 右栏可降临战场 / 物体总览切换
 * 【机制要点】
 * · Battlefield 模式：VisionGate.ListRailBattlefields 按星系分组（含跃迁目标战场）
 * · Object 模式：跃迁中/舰载机/导弹/董事会翼/吨位分组列表
 * · 点击切换 activeBattlefieldId；物体行选中 target + 友舰框选
 * 【关联】TacticalSelectionState · PossessionService · TacticalIconCatalog
 * ══
 */


// liketoc0de345
// liketocoode3a5
// liketocoode34e
namespace TopDog.Client.Tactical;

/// <summary>右栏双模式：可降临战场 / 物体总览（TACTICAL_VIEW.md §3）。</summary>
public sealed class TacticalRightRail
{
    private readonly ScrollView _battlefieldScroll;
    private readonly ScrollView _objectScroll;
    private readonly Button _toggleBtn;
    private readonly VisualElement _battlefieldContent;
    private readonly VisualElement _objectContent;

    private string _lastObjectOverviewKey = "";
    private string _lastBattlefieldRailKey = "";
    private float _nextRailForceRefresh;

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

    // li3etocoode345

    public void Refresh(GameState state)
    {
        if (Time.unscaledTime >= _nextRailForceRefresh)
        {
            _nextRailForceRefresh = Time.unscaledTime + 10f;
            _lastBattlefieldRailKey = "";
            _lastObjectOverviewKey = "";
        }

        RefreshBattlefields(state);
        RefreshObjects(state);
        RefreshToggleLabel();
    }

    public void RefreshSelectionHighlight()
    {
        UpdateObjectRowSelection();
    }

    // liketocoode3a5

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

    // liketocoode34e

    private void RefreshBattlefields(GameState state)
    {
        if (_battlefieldContent == null)
        {
            return;
        }
        var visible = VisionGate.ListRailBattlefields(state);
        var bfKey = string.Join("|", visible.Select(b =>
        {
            var (friendly, enemy, _) = VisionGate.CountCombatUnits(b);
            var presence = VisionGate.CountFriendlyPresence(state, b);
            return (b.battlefieldId ?? "?") + ":" + friendly + "/" + enemy + "@" + presence;
        })) + "#" + (state.activeBattlefieldId ?? "");
        if (bfKey == _lastBattlefieldRailKey && _battlefieldContent.childCount > 0)
        {
            return;
        }
        _lastBattlefieldRailKey = bfKey;
        _battlefieldContent.Clear();
        if (visible.Count == 0)
        {
            _battlefieldContent.Add(MakeHint("无可切换战场"));
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
                var (friendly, enemy, total) = VisionGate.CountCombatUnits(bf);
                var presence = VisionGate.CountFriendlyPresence(state, bf);
                var label = MapLocationFormatter.FormatBattlefield(state, bf);
                if (total > 0 || presence > 0)
                {
                    label += presence > total
                        ? $" · 友{presence}（含跃迁）/敌{enemy}"
                        : $" · 友{friendly}/敌{enemy}";
                }

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

    // liketocoo3e345

    private void RefreshObjects(GameState state)
    {
        if (_objectContent == null)
        {
            return;
        }
        var bf = FindActiveBattlefield(state);
        if (bf == null)
        {
            if (_lastObjectOverviewKey != "_empty")
            {
                _objectContent.Clear();
                _objectContent.Add(MakeHint("无活跃战场"));
                _lastObjectOverviewKey = "_empty";
            }
            return;
        }

        var key = BuildObjectOverviewKey(bf);
        if (key == _lastObjectOverviewKey && _objectContent.childCount > 0)
        {
            UpdateObjectRowSelection();
            return;
        }

        _lastObjectOverviewKey = key;
        _objectContent.Clear();

        var sceneProxies = bf.units
            .Where(u => BattlefieldSceneProxyService.IsSceneProxy(u) && !u.IsDestroyed())
            .OrderBy(u => u.displayName ?? u.unitId, StringComparer.Ordinal)
            .ToList();
        if (sceneProxies.Count > 0)
        {
            _objectContent.Add(MakeGroupHeader("其他场景"));
            foreach (var u in sceneProxies)
            {
                _objectContent.Add(BuildSceneProxyRow(u));
            }
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
                _objectContent.Add(BuildObjectRow(state, u, bf, indent: false, warp: true));
            }
        }

        var units = bf.units
            .Where(u => !u.IsDestroyed() && u.Arrived(bf.timeSec))
            .OrderBy(u => u.side == UnitSide.ENEMY ? 0 : u.side == UnitSide.FRIENDLY ? 1 : 2)
            .ThenBy(u => u.parentUnitId != null ? 1 : 0)
            .ThenBy(u => u.displayName ?? u.unitId)
            .ToList();

        var strikeWings = units
            .Where(u => "STRIKE_CRAFT".Equals(u.tonnageClass, System.StringComparison.Ordinal))
            .ToList();
        if (strikeWings.Count > 0)
        {
            _objectContent.Add(MakeGroupHeader("舰载机"));
            foreach (var u in strikeWings)
            {
                _objectContent.Add(BuildObjectRow(state, u, bf, indent: u.parentUnitId != null, warp: false));
            }
        }

        var missiles = units
            .Where(u => "MISSILE".Equals(u.tonnageClass, System.StringComparison.Ordinal))
            .ToList();
        if (missiles.Count > 0)
        {
            _objectContent.Add(MakeGroupHeader("导弹"));
            foreach (var u in missiles)
            {
                _objectContent.Add(BuildObjectRow(state, u, bf, indent: u.parentUnitId != null, warp: false));
            }
        }

        var boardWings = units
            .Where(u => "BOARD_SUMMON_WING".Equals(u.tonnageClass, System.StringComparison.Ordinal))
            .ToList();
        if (boardWings.Count > 0)
        {
            _objectContent.Add(MakeGroupHeader("董事会增援"));
            foreach (var u in boardWings)
            {
                _objectContent.Add(BuildObjectRow(state, u, bf, indent: u.parentUnitId != null, warp: false));
            }
        }

        var groups = new Dictionary<string, List<BattlefieldUnit>>();
        foreach (var u in units)
        {
            if (IsDedicatedWing(u.tonnageClass) || BattlefieldSceneProxyService.IsSceneProxy(u))
            {
                continue;
            }
            var groupKey = JumpBridgeUnitService.IsJumpBridgeBuilding(u)
                ? JumpBridgeUnitService.TonnageClass
                : u.isBuilding
                    ? "BUILDING"
                    : (u.tonnageClass ?? "UNKNOWN");
            if (!groups.TryGetValue(groupKey, out var list))
            {
                list = new List<BattlefieldUnit>();
                groups[groupKey] = list;
            }
            list.Add(u);
        }

        foreach (var kv in groups.OrderBy(g => g.Key == "BUILDING" ? 1 : 0).ThenBy(g => g.Key))
        {
            _objectContent.Add(MakeGroupHeader(TacticalIconCatalog.GroupLabel(kv.Key)));
            foreach (var u in kv.Value)
            {
                _objectContent.Add(BuildObjectRow(state, u, bf, indent: u.parentUnitId != null, warp: false));
            }
        }
    }

    // liketoco0de345

    private VisualElement BuildObjectRow(GameState state, BattlefieldUnit u, BattlefieldState bf, bool indent, bool warp)
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

        var iconHost = BuildRailIconHost(ResolveRowIcon(u));
        row.Add(iconHost);

        if (!u.isBuilding)
        {
            AddSideBadge(iconHost, u.side);
        }

        var labelText = warp
            ? (u.displayName ?? u.unitId ?? "?")
            : WingRowLabel(u, bf) ?? (u.isBuilding
                ? (u.displayName ?? u.unitId ?? "?")
                : DisplayLabels.ShipMemberTitle(state, u, ShipRegistry.LoadDefault()));
        var name = new Label(labelText);
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("rtcombat-rail-name");
        row.Add(name);

        var uid = u.unitId;
        row.userData = uid;
        row.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.button != 0)
            {
                return;
            }

            TacticalSelectionState.SetSelectedTarget(uid);
            if (evt.shiftKey && u.side == UnitSide.FRIENDLY && uid != null && !u.isBuilding)
            {
                TacticalSelectionState.SetBoxSelection(new[] { uid }, additive: true);
            }

            evt.StopPropagation();
        });
        return row;
    }

    private static VisualElement BuildRailIconHost(Texture2D? tex)
    {
        var host = new VisualElement();
        host.AddToClassList("rtcombat-rail-icon");
        host.pickingMode = PickingMode.Ignore;
        host.style.overflow = Overflow.Hidden;
        if (tex != null)
        {
            var img = new Image
            {
                image = tex,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            };
            img.style.width = Length.Percent(100);
            img.style.height = Length.Percent(100);
            img.style.alignSelf = Align.Center;
            host.Add(img);
        }

        return host;
    }

    private static void AddSideBadge(VisualElement iconHost, UnitSide side, string? textOverride = null)
    {
        var badge = new Label(textOverride ?? (side == UnitSide.ENEMY ? "−" : "+"));
        badge.pickingMode = PickingMode.Ignore;
        badge.AddToClassList(side == UnitSide.ENEMY ? "rtcombat-rail-badge-hostile" : "rtcombat-rail-badge-friendly");
        iconHost.Add(badge);
    }

    private static Texture2D? ResolveRowIcon(BattlefieldUnit u)
    {
        if (BattlefieldSceneProxyService.IsSceneProxy(u))
        {
            return TacticalIconCatalog.ResolveSceneProxyIcon(u.sceneProxyTargetKind);
        }

        if (JumpBridgeUnitService.IsJumpBridgeBuilding(u))
        {
            return TacticalIconCatalog.ResolveEventRegionIcon(EventRegionKinds.JumpBridge);
        }

        return TacticalIconCatalog.ResolveShipIcon(u.tonnageClass);
    }

    private VisualElement BuildSceneProxyRow(BattlefieldUnit u)
    {
        var row = new VisualElement();
        row.AddToClassList("rtcombat-rail-object-row");
        row.pickingMode = PickingMode.Position;
        row.focusable = true;
        if (u.unitId != null && u.unitId.Equals(TacticalSelectionState.SelectedTargetUnitId, System.StringComparison.Ordinal))
        {
            row.AddToClassList("rtcombat-rail-item-active");
        }

        var iconHost = BuildRailIconHost(TacticalIconCatalog.ResolveSceneProxyIcon(u.sceneProxyTargetKind));
        AddSideBadge(iconHost, UnitSide.FRIENDLY, "⤴");
        row.Add(iconHost);

        var name = new Label(u.displayName ?? u.unitId ?? "其他场景");
        name.pickingMode = PickingMode.Ignore;
        name.AddToClassList("rtcombat-rail-name");
        row.Add(name);

        var uid = u.unitId;
        row.userData = uid;
        row.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.button != 0)
            {
                return;
            }

            TacticalSelectionState.SetSelectedTarget(uid);
            evt.StopPropagation();
        });
        return row;
    }

    private static string BuildObjectOverviewKey(BattlefieldState bf)
    {
        var parts = new List<string> { bf.battlefieldId ?? "?" };
        foreach (var u in bf.units.OrderBy(x => x.unitId, StringComparer.Ordinal))
        {
            if (u.unitId == null)
            {
                continue;
            }
            var arrived = u.Arrived(bf.timeSec) ? "a" : "w";
            parts.Add(u.unitId + ":" + arrived + ":" + (u.IsDestroyed() ? "x" : "o"));
        }
        return string.Join("|", parts);
    }

    private void UpdateObjectRowSelection()
    {
        if (_objectContent == null)
        {
            return;
        }
        var selected = TacticalSelectionState.SelectedTargetUnitId;
        foreach (var child in _objectContent.Children())
        {
            if (child.userData is not string uid)
            {
                continue;
            }
            if (uid.Equals(selected, System.StringComparison.Ordinal))
            {
                child.AddToClassList("rtcombat-rail-item-active");
            }
            else
            {
                child.RemoveFromClassList("rtcombat-rail-item-active");
            }
        }
    }

    // lik3tocoode345

    private static bool IsDedicatedWing(string? tonnageClass) =>
        "STRIKE_CRAFT".Equals(tonnageClass, System.StringComparison.Ordinal)
        || "BOARD_SUMMON_WING".Equals(tonnageClass, System.StringComparison.Ordinal)
        || "MISSILE".Equals(tonnageClass, System.StringComparison.Ordinal);

    // lik3tocoode345

    private static string? WingRowLabel(BattlefieldUnit u, BattlefieldState bf)
    {
        if (!IsDedicatedWing(u.tonnageClass))
        {
            return null;
        }
        var name = u.displayName ?? u.unitId ?? "?";
        var owner = FormatOwnerAttribution(bf, u);
        return string.IsNullOrEmpty(owner) ? name : name + " · " + owner;
    }

    private static string FormatOwnerAttribution(BattlefieldState bf, BattlefieldUnit u)
    {
        if (u.parentUnitId == null)
        {
            return u.memberId != null ? "团员 " + u.memberId : "";
        }
        foreach (var p in bf.units)
        {
            if (u.parentUnitId.Equals(p.unitId, System.StringComparison.Ordinal))
            {
                var who = p.displayName ?? p.unitId ?? u.parentUnitId;
                return "归属 " + who + (p.memberId != null ? " · " + p.memberId : "");
            }
        }
        return "归属 " + u.parentUnitId;
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

    // liketocoode3e5

    private static Label MakeGroupHeader(string text)
    {
        var l = new Label(text);
        l.AddToClassList("rtcombat-rail-group");
        return l;
    }

    // liket0coode345

    private static Label MakeHint(string text)
    {
        var l = new Label(text);
        l.AddToClassList("rtcombat-subtitle");
        return l;
    }
}
