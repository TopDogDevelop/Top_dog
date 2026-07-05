using System;
using System.Collections.Generic;
using TopDog.App;
using TopDog.AgentDiag;
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

    // liketoc0de345

    public FleetCommandBar(
        VisualElement root,
        Func<SimulationCore> core,
        Action<string> status,
        Action<string, bool> statusWithSuccess = null)
    {
        _core = core;
        _status = statusWithSuccess ?? ((msg, _) => status(msg));

        BindSimple(root, "btn-rally", () => WithBf((s, bf) => FleetOrderService.RallyToBattlefield(s, bf, Sel())));
        BindSimple(root, "btn-scatter", () => WithBf((s, bf) => FleetOrderService.OrderScatter(s, bf, new Random(), Sel())));
        BindSimple(root, "btn-stop", () => WithBf((s, bf) => FleetOrderService.OrderStop(s, bf, false, Sel())));
        BindSimple(root, "btn-stop-all", () => WithBf((s, bf) => FleetOrderService.OrderStop(s, bf, true, Sel())));
        BindSimple(root, "btn-retreat", () => WithBf((s, bf) => FleetOrderService.OrderRetreat(s, bf)));
        BindSimple(root, "btn-focus", () =>
            WithBf((s, bf) => FleetOrderService.OrderFocus(s, bf, TacticalSelectionState.SelectedTargetUnitId, Sel())));
        BindSimple(root, "btn-cease-fire", () =>
            WithBf((s, bf) => FleetOrderService.OrderCeaseFire(s, bf, Sel())));
        BindSimple(root, "btn-follow-attack", () =>
            WithBf((s, bf) => FleetOrderService.OrderFollowAttack(s, bf, TacticalSelectionState.SelectedTargetUnitId, Sel())));
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
        // 实时战底栏始终可点；星图模式由 CombatRealtimeController 整栏 SetEnabled。
    }

    public void SetBarEnabled(bool enabled)
    {
        // 预留：CombatRealtimeController 星图模式调用
    }

    private string IssueWarp(GameState s, BattlefieldState bf, float? landingKm)
    {
        var sel = TacticalSelectionState.SelectedTargetUnitId;
        var resolved = FleetOrderService.TryResolveWarpTargetScene(s, bf, sel, out _, out _);
        AgentSessionDebugLog.Write(
            "H5",
            "FleetCommandBar.IssueWarp",
            "entry",
            new { sel, bfId = bf.battlefieldId, initialResolve = resolved });
        if (!resolved)
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

    private static float? CommandRangeKm() => TacticalSelectionState.DefaultCommandRangeKm;

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

        var label = new Label(FormatDefaultDistKm());
        label.name = "lbl-default-dist-km";
        label.style.minWidth = 52;

        var slider = new Slider(0f, 1f);
        slider.name = "slider-default-dist";
        slider.value = TacticalRangeScale.DialTFromKm(EffectiveDefaultDistKm());
        slider.style.flexGrow = 1;
        slider.style.minWidth = 80;
        slider.RegisterValueChangedCallback(evt =>
        {
            var km = TacticalRangeScale.KmFromDialT(evt.newValue);
            TacticalSelectionState.DefaultCommandRangeKm = km;
            label.text = FormatDefaultDistKm();
        });

        row.Add(label);
        row.Add(slider);

        if (btn != null)
        {
            btn.tooltip = "默认距离（滑块调节，用于接近/远离/环绕/跃迁）";
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

    private static float EffectiveDefaultDistKm() =>
        TacticalSelectionState.DefaultCommandRangeKm ?? TacticalRangeScale.MidKm;

    private static string FormatDefaultDistKm() =>
        TacticalSelectionState.DefaultCommandRangeKm.HasValue
            ? $"{TacticalSelectionState.DefaultCommandRangeKm.Value:0} km"
            : $"{EffectiveDefaultDistKm():0} km";
}
