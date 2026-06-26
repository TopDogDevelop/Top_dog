using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.App;
using TopDog.Sim.Possession;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §3 底栏舰队指令 · §2 战场间跃迁
 * 本文件: FleetCommandBar.cs — 实时战术底栏舰队指令（含接近/远离/跃迁）
 * 【机制要点】
 * · Sel()：框选友舰子集；无框选时接近/远离默认附身舰
 * · OrderApproach/OrderAway/OrderOrbit/OrderWarp 等经 FleetOrderService
 * · btn-warp：VisionGate 可见的其他 battlefield 集体跃迁
 * 【关联】FleetOrderService · TacticalSelectionState · CombatRealtimeController
 * ══
 */


// liketoc0de345
// liketocoode3a5
// liketocoode34e
namespace TopDog.Client.Tactical;

/// <summary>底栏舰队指令（TACTICAL_WARP_AND_ORDERS.md §3 · 框选子集）。</summary>
public sealed class FleetCommandBar
{
    private readonly Button _focusBtn;
    private readonly Button _followBtn;
    private readonly Button _followAttackBtn;
    private readonly Button _scatterBtn;
    private readonly Button _orbitBtn;
    private readonly Button _awayBtn;
    private readonly Button _warpBtn;
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
        _focusBtn = root.Q<Button>("btn-focus");
        _followBtn = root.Q<Button>("btn-follow");
        _followAttackBtn = root.Q<Button>("btn-follow-attack");
        _scatterBtn = root.Q<Button>("btn-scatter");
        _orbitBtn = root.Q<Button>("btn-orbit");
        _awayBtn = root.Q<Button>("btn-away");
        _warpBtn = root.Q<Button>("btn-warp");

        Bind(root, "btn-rally", () => WithBf((s, bf) => FleetOrderService.RallyToBattlefield(s, bf, Sel())));
        Bind(root, "btn-follow", () => WithBf((s, bf) =>
        {
            var id = TacticalSelectionState.SelectedTargetUnitId;
            return FleetOrderService.OrderApproach(s, bf, id, Sel());
        }));
        Bind(root, "btn-away", () => WithBf((s, bf) =>
        {
            var id = TacticalSelectionState.SelectedTargetUnitId;
            return FleetOrderService.OrderAway(s, bf, id, Sel());
        }));
        Bind(root, "btn-focus", () => WithBf((s, bf) => FleetOrderService.OrderFocus(s, bf, TacticalSelectionState.SelectedTargetUnitId, Sel())));
        Bind(root, "btn-follow-attack", () => WithBf((s, bf) => FleetOrderService.OrderFollowAttack(s, bf, TacticalSelectionState.SelectedTargetUnitId, Sel())));
        Bind(root, "btn-scatter", () => WithBf((s, bf) => FleetOrderService.OrderScatter(s, bf, new Random(), Sel())));
        Bind(root, "btn-orbit", () => WithBf((s, bf) =>
        {
            var id = TacticalSelectionState.SelectedTargetUnitId;
            return id != null ? FleetOrderService.OrderOrbit(s, bf, id, Sel()) : "请先选择目标";
        }));
        Bind(root, "btn-stop", () => WithBf((s, bf) => FleetOrderService.OrderStop(s, bf, false, Sel())));
        Bind(root, "btn-stop-all", () => WithBf((s, bf) => FleetOrderService.OrderStop(s, bf, true, Sel())));
        Bind(root, "btn-retreat", () => WithBf((s, bf) => FleetOrderService.OrderRetreat(s, bf)));
        Bind(root, "btn-warp", WarpToAlternateBattlefield);
        Bind(root, "btn-auto-fire", () =>
        {
            var c = _core();
            if (c != null)
            {
                Emit(c.ToggleAutoFire(), true);
            }
        });
        Bind(root, "btn-possess-friendly", () =>
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

            var next = VisionAnchorService.CyclePossession(c.State, bf);
            if (next == null)
            {
                Emit("无可附身友舰", false);
                return;
            }

            Emit(c.PossessMember(next), true);
        });
        Bind(root, "btn-continue", () =>
        {
            var c = _core();
            Emit(c != null ? c.CombatContinue() : "模拟未启动", c != null);
        });
    }

    // li3etocoode345

    public void RefreshGate(GameState state)
    {
        var hasTarget = TacticalSelectionState.SelectedTargetUnitId != null;
        if (_focusBtn != null) _focusBtn.SetEnabled(hasTarget);
        if (_followBtn != null) _followBtn.SetEnabled(hasTarget);
        if (_awayBtn != null) _awayBtn.SetEnabled(hasTarget);
        if (_followAttackBtn != null) _followAttackBtn.SetEnabled(hasTarget);
        if (_orbitBtn != null) _orbitBtn.SetEnabled(hasTarget);
        var hasAltBf = false;
        if (state != null && state.activeBattlefieldId != null)
        {
            hasAltBf = VisionGate.ListVisibleBattlefields(state)
                .Any(b => b.battlefieldId != null
                    && !b.battlefieldId.Equals(state.activeBattlefieldId, StringComparison.Ordinal));
        }
        if (_warpBtn != null) _warpBtn.SetEnabled(hasAltBf);
        if (_scatterBtn != null) _scatterBtn.SetEnabled(state != null && state.combatRealtimeActive);
    }

    // liketocoode3a5

    private IReadOnlyCollection<string> Sel()
    {
        var box = TacticalSelectionState.GetSelectedFriendlyUnitIds();
        if (box.Count > 0)
        {
            return box;
        }
        var target = TacticalSelectionState.SelectedTargetUnitId;
        if (string.IsNullOrEmpty(target))
        {
            return box;
        }
        var c = _core();
        var bf = c != null ? ActiveBf(c.State) : null;
        if (bf == null)
        {
            return box;
        }
        foreach (var u in bf.units)
        {
            if (target.Equals(u.unitId, StringComparison.Ordinal)
                && u.side == UnitSide.FRIENDLY
                && !u.isBuilding
                && !u.IsDestroyed())
            {
                return new[] { target };
            }
        }
        return box;
    }

    // liketocoode34e

    private void WarpToAlternateBattlefield()
    {
        var c = _core();
        if (c == null)
        {
            return;
        }
        var s = c.State;
        var bf = ActiveBf(s);
        if (bf == null)
        {
            Emit("无活跃战场", false);
            return;
        }
        var target = VisionGate.ListVisibleBattlefields(s)
            .FirstOrDefault(b => b.battlefieldId != null
                && !b.battlefieldId.Equals(s.activeBattlefieldId, StringComparison.Ordinal));
        if (target?.battlefieldId == null)
        {
            Emit("无其他可跃迁战场", false);
            return;
        }
        var msg = FleetOrderService.OrderWarp(s, bf, target.battlefieldId, c.Ships, allFriendly: Sel().Count == 0, Sel());
        Emit(msg, msg.StartsWith("跃迁", StringComparison.Ordinal));
    }

    // liketocoo3e345

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
        Emit(msg, !msg.StartsWith("请先", StringComparison.Ordinal)
            && !msg.StartsWith("无", StringComparison.Ordinal)
            && !msg.Contains("非收割"));
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

    // lik3tocoode345

    private static void Bind(VisualElement root, string name, Action action)
    {
        var btn = root.Q<Button>(name);
        if (btn != null)
        {
            btn.clicked += action;
        }
    }

    // liketocoode3e5
    // liket0coode345
}
