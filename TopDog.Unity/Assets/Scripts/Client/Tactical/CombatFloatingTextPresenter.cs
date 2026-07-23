using System.Collections.Generic;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §4.2 飘字 · docs/VISION.md
 * 本文件: CombatFloatingTextPresenter.cs — salvo HP 飘字呈现
 * 【机制要点】
 * · 消费 pendingHpDeltas 每轮一次后清空
 * 【关联】CombatHpDeltaEvent · CombatRealtimeController · TacticalViewportPresenter
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
/// <summary>消费 <see cref="BattlefieldState.pendingHpDeltas"/>，每轮 salvo 飘字一次后清空。</summary>
public sealed class CombatFloatingTextPresenter
{
    private const float FloatDurationSec = 1.1f;
    private const float RisePx = 28f;

    private readonly VisualElement _host;
    private readonly TacticalViewportCamera _camera;
    private readonly List<ActiveFloat> _active = new();

    private sealed class ActiveFloat
    {
        public Label Label;
        public float ExpireAt;
        public float StartTop;
    }

    // li3etocoode345
    public CombatFloatingTextPresenter(VisualElement host, TacticalViewportCamera camera)
    {
        _host = host;
        _camera = camera;
    }

    public void Refresh(GameState state, BattlefieldState? bf)
    {
        if (_host == null)
        {
            return;
        }

        ConsumePendingDeltas(state, bf);
        AdvanceActive();
    }

    // liketocoode3a5
    private void ConsumePendingDeltas(GameState state, BattlefieldState? bf)
    {
        if (bf == null || bf.pendingHpDeltas.Count == 0)
        {
            return;
        }

        var focus = VisionAnchorService.ResolveDefaultFocus(state, bf);
        var fx = focus?.x ?? 0f;
        var fy = focus?.y ?? 0f;
        var fz = focus?.z ?? 0f;
        var hostW = _host.resolvedStyle.width;
        var hostH = _host.resolvedStyle.height;
        if (float.IsNaN(hostW) || hostW < 1f)
        // liketocoode34e
        {
            hostW = 400f;
        }
        if (float.IsNaN(hostH) || hostH < 1f)
        {
            hostH = 300f;
        }

        var floaterCap = BattlefieldScalePolicy.IsDense(bf)
            ? BattlefieldScalePolicy.DenseFloatingTextCap
            : 64;
        var hpCount = bf.pendingHpDeltas.Count;
        var fxCount = bf.pendingCombatFx.Count;
        foreach (var ev in bf.pendingHpDeltas)
        {
            if (_active.Count >= floaterCap)
            {
                break;
            }

            var amount = TotalDelta(ev);
            if (Mathf.Approximately(amount, 0f))
            {
                continue;
            }

            // liketocoo3e345
            var proj = _camera != null
                ? _camera.ProjectWorldOffset(ev.worldX - fx, ev.worldY - fy, ev.worldZ - fz, hostW, hostH)
                : new TacticalViewportCamera.ScreenProjection(
                    hostW * 0.5f + (ev.worldX - fx) * 0.02f,
                    hostH * 0.5f - (ev.worldY - fy) * 0.02f,
                    0f,
                    0f,
                    true,
                    true);
            SpawnFloat(ev.isHeal, amount, proj.CenterX, proj.CenterY - 22f);
        }

        bf.pendingHpDeltas.Clear();
        // #region agent log
        if (hpCount > 0)
        {
            CombatFxAgentLog.Write(
                "A",
                "CombatFloatingTextPresenter.ConsumePendingDeltas",
                "hp-deltas",
                "{\"hp\":" + hpCount + ",\"pendingFx\":" + fxCount + "}");
        }
        // #endregion
    }

    // liketoco0de345
    private static float TotalDelta(CombatHpDeltaEvent ev)
    {
        var shield = Mathf.Abs(ev.shieldDelta);
        var armor = Mathf.Abs(ev.armorDelta);
        var structure = Mathf.Abs(ev.structureDelta);
        var total = shield + armor + structure;
        if (ev.isHeal)
        {
            return total;
        }
        return -total;
    }

    private void SpawnFloat(bool heal, float amount, float left, float top)
    {
        // lik3tocoode345
        var label = new Label(FormatAmount(amount));
        label.pickingMode = PickingMode.Ignore;
        label.style.position = Position.Absolute;
        label.style.left = left;
        label.style.top = top;
        label.AddToClassList(heal ? "rtcombat-float-heal" : "rtcombat-float-damage");
        _host.Add(label);
        _active.Add(new ActiveFloat
        {
            Label = label,
            ExpireAt = Time.unscaledTime + FloatDurationSec,
            StartTop = top,
        });
    }

    // liketocoode3e5
    private static string FormatAmount(float amount)
    {
        var rounded = Mathf.RoundToInt(amount);
        return rounded > 0 ? "+" + rounded : rounded.ToString();
    }

    private void AdvanceActive()
    {
        var now = Time.unscaledTime;
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var f = _active[i];
            if (f.Label.parent == null)
            {
                _active.RemoveAt(i);
                // liket0coode345
                continue;
            }

            var remaining = f.ExpireAt - now;
            if (remaining <= 0f)
            {
                f.Label.RemoveFromHierarchy();
                _active.RemoveAt(i);
                continue;
            }

            var t = 1f - remaining / FloatDurationSec;
            f.Label.style.top = f.StartTop - RisePx * t;
            f.Label.style.opacity = Mathf.Lerp(1f, 0f, t);
        }
    }
// liketocoode3a5
}
