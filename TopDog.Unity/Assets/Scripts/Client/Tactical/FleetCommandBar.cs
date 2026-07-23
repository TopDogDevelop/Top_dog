using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.App;
using TopDog.AgentDiag;
using TopDog.Client;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §3 底栏舰队指令 · §3.4 舰载机指挥
 * 本文件: FleetCommandBar.cs — 实时战术底栏舰队指令（含刻度盘/进入建筑/停火）
 * 【机制要点】
 * · 接近/远离/环绕/跃迁：单击即下令（用「默认距离」）
 * · btn-cease-fire → OrderCeaseFire（舰载机 RECALL）
 * · btn-default-dist + 滑块：设定默认距离（TacticalRangeScale 非线性 1–1000 km）
 * · btn-enter-building → OrderEnterBuilding（跨星系跳桥）
 * 【关联】FleetOrderService · StrikeWingOrderService · TacticalSelectionState · CombatRealtimeController
 * ══
 */

namespace TopDog.Client.Tactical;

/// <summary>底栏舰队指令（TACTICAL_WARP_AND_ORDERS.md §3 · 框选子集）。</summary>
public sealed class FleetCommandBar
{
    private readonly Func<SimulationCore> _core;
    private readonly Action<string, bool> _status;
    private Button? _scopeBtn;

    // liketoc0de345

    public FleetCommandBar(
        VisualElement root,
        Func<SimulationCore> core,
        Action<string> status,
        Action<string, bool> statusWithSuccess = null)
    {
        _core = core;
        _status = statusWithSuccess ?? ((msg, _) => status(msg));

        _scopeBtn = root.Q<Button>("btn-command-scope");

        BindSimple(root, "btn-rally", () =>
        {
            var selected = Sel();
            // #region agent log
            AgentSessionDebugLog.WriteDebugSession(
                "R1",
                "FleetCommandBar.cs:btn-rally",
                "rally button invoked",
                new
                {
                    selectedCount = selected?.Count ?? 0,
                    scope = TacticalSelectionState.CommandScope.ToString(),
                });
            // #endregion
            WithBf((s, bf) =>
            {
                var result = FleetOrderService.RallyToBattlefield(s, bf, selected);
                // #region agent log
                AgentSessionDebugLog.WriteDebugSession(
                    "R1-R3",
                    "FleetCommandBar.cs:btn-rally:result",
                    "rally command returned",
                    new
                    {
                        result,
                        battlefieldId = bf.battlefieldId,
                        rallyActive = s.battlefields.Sum(item =>
                            item.units.Count(unit => unit.rallyActive)),
                        inTransit = s.tacticalWarpInTransit.Count,
                    });
                // #endregion
                return result;
            });
        });
        BindSimple(root, "btn-scatter", () => WithBf((s, bf) => FleetOrderService.OrderScatter(s, bf, new Random(), Sel())));
        BindSimple(root, "btn-stop", () => WithBf((s, bf) => FleetOrderService.OrderStop(s, bf, false, Sel())));
        BindSimple(root, "btn-stop-all", () => WithBf((s, bf) => FleetOrderService.OrderStop(s, bf, true, Sel())));
        BindSimple(root, "btn-retreat", () => WithBf((s, bf) => FleetOrderService.OrderRetreat(s, bf)));
        BindSimple(root, "btn-cease-fire", () =>
            WithBf((s, bf) => FleetOrderService.OrderCeaseFire(s, bf, Sel())));
        BindSimple(root, "btn-repair-target", () =>
            WithBf((s, bf) => FleetOrderService.OrderRepairTarget(
                s, bf, TacticalSelectionState.SelectedTargetUnitId, Sel(), _core()?.Modules)));
        BindSimple(root, "btn-command-scope", () =>
        {
            TacticalSelectionState.CommandScope = TacticalSelectionState.CommandScope == FleetCommandScope.AllInScene
                ? FleetCommandScope.SelectedOnly
                : FleetCommandScope.AllInScene;
            var c = _core();
            if (c != null)
            {
                c.State.fleetCommandScope = TacticalSelectionState.CommandScope;
            }

            Emit(TacticalSelectionState.CommandScopeLabel(), true);
        });
        BindSimple(root, "btn-enter-building", () =>
            WithBf((s, bf) => FleetOrderService.OrderEnterBuilding(s, bf, TacticalSelectionState.SelectedTargetUnitId, Sel())));
        BindSimple(root, "btn-auto-fire", () =>
        {
            var c = _core();
            if (c != null)
            {
                Emit(c.ToggleAutoFire(), true);
            }
        });
        BindSimple(root, "btn-possess-friendly", () =>
        {
            var c = _core();
            if (c == null)
            {
                return;
            }

            var bf = ActiveBf(c.State);
            if (bf == null)
            {
                Emit("无活跃战场", false);
                return;
            }

            var focus = VisionAnchorService.CycleTacticalFocus(c.State, bf);
            if (focus == null)
            {
                Emit("无可切换友方单位", false);
                return;
            }

            Emit("视野: " + (focus.displayName ?? focus.unitId), true);
        });
        BindSimple(root, "btn-continue", () =>
        {
            var c = _core();
            Emit(c != null ? c.CombatContinue() : "模拟未启动", c != null);
        });

        BindRangeCommand(root, "btn-follow", (s, bf, km) =>
            FleetOrderService.OrderApproach(s, bf, TacticalSelectionState.SelectedTargetUnitId, Sel(), km));
        BindRangeCommand(root, "btn-away", (s, bf, km) =>
            FleetOrderService.OrderAway(s, bf, TacticalSelectionState.SelectedTargetUnitId, Sel(), km));
        BindRangeCommand(root, "btn-orbit", (s, bf, km) =>
            FleetOrderService.OrderOrbit(s, bf, TacticalSelectionState.SelectedTargetUnitId, Sel(), km));
        BindRangeCommand(root, "btn-warp", (s, bf, km) => IssueWarp(s, bf, km));
        BindDefaultDistanceDial(root);
    }

    // li3etocoode345

    public void RefreshGate(GameState state)
    {
        var hasSelection = TacticalSelectionState.GetSelectedFriendlyUnitIds().Count > 0
            || !string.IsNullOrEmpty(TacticalSelectionState.SelectedTargetUnitId);
        if (_scopeBtn != null)
        {
            _scopeBtn.SetEnabled(!hasSelection);
            _scopeBtn.text = TacticalSelectionState.CommandScopeLabel();
            _scopeBtn.EnableInClassList("rtcombat-scope-forced-selected", hasSelection);
        }
    }

    public void SetBarEnabled(bool enabled)
    {
        // 预留：CombatRealtimeController 星图模式调用
    }

    private string IssueWarp(GameState s, BattlefieldState bf, float? landingKm)
    {
        var sel = TacticalSelectionState.SelectedTargetUnitId;
        // Same-scene unit / landmark targets must keep selection — do NOT overwrite with scene proxies
        // (old auto-pick stole 同场景跃迁 targets → silent / wrong cross-scene routing).
        var sameSceneUnitTarget = IsSameSceneWarpUnitTarget(bf, sel);
        var resolved = !sameSceneUnitTarget
            && FleetOrderService.TryResolveWarpTargetScene(s, bf, sel, out _, out _);
        AgentSessionDebugLog.Write(
            "H5",
            "FleetCommandBar.IssueWarp",
            "entry",
            new { sel, bfId = bf.battlefieldId, sameSceneUnitTarget, initialResolve = resolved });
        if (!sameSceneUnitTarget && !resolved)
        {
            foreach (var u in bf.units)
            {
                if (!BattlefieldSceneProxyService.IsSceneProxy(u) || u.unitId == null)
                {
                    continue;
                }

                if (FleetOrderService.TryResolveWarpTargetScene(s, bf, u.unitId, out _, out _))
                {
                    sel = u.unitId;
                    TacticalSelectionState.SetSelectedTarget(sel);
                    break;
                }
            }

            if (!FleetOrderService.TryResolveWarpTargetScene(s, bf, sel, out _, out _))
            {
                foreach (var link in BattlefieldSceneProxyService.ListOffSceneLinks(s, bf))
                {
                    if (FleetOrderService.TryResolveWarpTargetScene(s, bf, link.UnitId, out _, out _))
                    {
                        sel = link.UnitId;
                        TacticalSelectionState.SetSelectedTarget(sel);
                        break;
                    }
                }
            }
        }

        var c = _core();
        if (c == null)
        {
            return "模拟未启动";
        }

        return FleetOrderService.OrderWarpToSceneTarget(
            s,
            bf,
            sel,
            c.Ships,
            allFriendly: Sel().Count == 0,
            Sel(),
            landingKm);
    }

    private IReadOnlyCollection<string> Sel() =>
        TacticalSelectionState.GetSelectedFriendlyUnitIds();

    private static float? CommandRangeKm()
    {
        TacticalSelectionState.EnsureDefaultCommandRangeLoaded();
        return TacticalSelectionState.EffectiveDefaultCommandRangeKm;
    }

    /// <summary>同场景非 proxy 单位（含友舰）作为跃迁目标，走 OrderIntraSceneWarp。</summary>
    private static bool IsSameSceneWarpUnitTarget(BattlefieldState bf, string? unitId)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            return false;
        }

        foreach (var u in bf.units)
        {
            if (u.unitId == null || !u.unitId.Equals(unitId, StringComparison.Ordinal))
            {
                continue;
            }

            return !BattlefieldSceneProxyService.IsSceneProxy(u) && !u.isBuilding && !u.IsDestroyed();
        }

        return false;
    }

    private void WithBf(Func<GameState, BattlefieldState, string> action)
    {
        var c = _core();
        if (c == null)
        {
            return;
        }

        var bf = ActiveBf(c.State);
        if (bf == null)
        {
            Emit("无活跃战场", false);
            return;
        }

        c.State.fleetCommandScope = TacticalSelectionState.CommandScope;
        var msg = action(c.State, bf);
        Emit(msg, msg.StartsWith("已下令", StringComparison.Ordinal));
    }

    private void Emit(string msg, bool success) => _status(msg, success);

    // liketoco0de345

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

    private void BindSimple(VisualElement root, string name, Action action)
    {
        var btn = root.Q<Button>(name);
        if (btn != null)
        {
            btn.clicked += action;
        }
    }

    private void BindRangeCommand(
        VisualElement root,
        string name,
        Func<GameState, BattlefieldState, float?, string> issue)
    {
        var btn = root.Q<Button>(name);
        if (btn == null)
        {
            return;
        }

        btn.clicked += () =>
        {
            if (name == "btn-warp")
            {
                AgentSessionDebugLog.Write(
                    "H4",
                    "FleetCommandBar.btn-warp",
                    "clicked",
                    new { sel = TacticalSelectionState.SelectedTargetUnitId });
            }

            var c = _core();
            if (c == null)
            {
                if (name == "btn-warp")
                {
                    AgentSessionDebugLog.Write("H4", "FleetCommandBar.btn-warp", "core_null", null);
                }

                return;
            }

            var bf = ActiveBf(c.State);
            if (bf == null)
            {
                if (name == "btn-warp")
                {
                    AgentSessionDebugLog.Write("H4", "FleetCommandBar.btn-warp", "no_active_bf", null);
                }

                Emit("无活跃战场", false);
                return;
            }

            var km = CommandRangeKm();
            var msg = issue(c.State, bf, km);
            if (name == "btn-warp")
            {
                AgentSessionDebugLog.Write(
                    "H4-H5",
                    "FleetCommandBar.btn-warp",
                    "result",
                    new { msg });
            }

            Emit(msg, msg.StartsWith("已下令", StringComparison.Ordinal));
        };
    }

    private void BindDefaultDistanceDial(VisualElement root)
    {
        var btn = root.Q<Button>("btn-default-dist");
        var bar = root.Q<VisualElement>("fleet-command-bar");
        if (bar == null)
        {
            return;
        }

        var row = new VisualElement();
        row.AddToClassList("rtcombat-default-dist-slider");
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.flexGrow = 0;
        row.style.flexShrink = 1;
        row.style.minWidth = 140;

        TacticalSelectionState.EnsureDefaultCommandRangeLoaded();
        var label = new Label(FormatDefaultDistKm(TacticalSelectionState.EffectiveDefaultCommandRangeKm));
        label.name = "lbl-default-dist-km";
        label.style.minWidth = 52;

        var slider = new Slider(0f, 1f);
        slider.name = "slider-default-dist";
        slider.value = TacticalRangeScale.DialTFromKm(TacticalSelectionState.EffectiveDefaultCommandRangeKm);
        slider.style.flexGrow = 1;
        slider.style.minWidth = 80;

        // 拖动中只预览标签；松手（PointerUp / CaptureOut）才提交距离并写入 PlayerPrefs
        slider.RegisterValueChangedCallback(evt =>
        {
            label.text = FormatDefaultDistKm(TacticalRangeScale.KmFromDialT(evt.newValue));
        });

        void CommitFromSlider()
        {
            var km = TacticalRangeScale.KmFromDialT(slider.value);
            ClientGameSettings.SetDefaultCommandRangeKm(km, persist: true);
            label.text = FormatDefaultDistKm(km);
        }

        slider.RegisterCallback<PointerUpEvent>(_ => CommitFromSlider());
        slider.RegisterCallback<PointerCaptureOutEvent>(_ => CommitFromSlider());
        slider.RegisterCallback<FocusOutEvent>(_ => CommitFromSlider());

        row.Add(label);
        row.Add(slider);

        if (btn != null)
        {
            btn.tooltip = "默认距离（松手生效，0km=不限距；跨对局记忆）";
            var idx = bar.IndexOf(btn);
            if (idx >= 0)
            {
                bar.Insert(idx + 1, row);
            }
            else
            {
                bar.Add(row);
            }
        }
        else
        {
            bar.Add(row);
        }
    }

    private static string FormatDefaultDistKm(float km) =>
        km <= 0.01f ? "0 km" : $"{km:0} km";
}
