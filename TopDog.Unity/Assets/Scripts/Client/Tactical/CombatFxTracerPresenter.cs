using System.Collections.Generic;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_FX.md
 * 本文件: CombatFxTracerPresenter.cs — 混伤火炮深蓝拖尾点（纯表现 · UITK）
 * 【机制要点】
 * · Drain pendingCombatFx；t∈[0,1] 每帧 lerp；与舰标同宿主 ProjectWorldOffset
 * · 首帧贴开火点（t=0）不推时；大步长时沿线段细分拖尾（≤20px / ≤10 圆）
 * · 场外开火打向场域庇护：终点夹到球壳（与 ResolveFieldRadiusM 同半径，无 ReadPixels）
 * 【关联】CombatFxEmit · CombatFloatingTextPresenter · TacticalViewportCamera · FieldAuraService
 * ══
 */

namespace TopDog.Client.Tactical;

public sealed class CombatFxTracerPresenter
{
    private const float DotSizePx = 5f;
    private const float TrailLengthPx = 20f;
    private const int TrailMaxCircles = 10;
    private const float TrailMinSizePx = 1.2f;
    private const float MaxAdvanceDtSec = 0.05f;
    private static readonly Color DotColor = new(0.12f, 0.32f, 0.92f, 0.85f);
    private static readonly Color TrailColor = new(0.08f, 0.22f, 0.78f, 0.55f);

    private readonly VisualElement _host;
    private readonly TacticalViewportCamera _camera;
    private readonly ModuleRegistry _modules;
    private readonly ShipRegistry _ships;
    private readonly List<ActiveTracer> _active = new();

    private sealed class ActiveTracer
    {
        public string FirerId = "";
        public string TargetId = "";
        public float DurationSec;
        public float ElapsedSec;
        public bool StartPosePending = true;
        public bool EndpointClippedToShell;
        public float Ax;
        public float Ay;
        public float Az;
        public float Bx;
        public float By;
        public float Bz;
        public VisualElement? Dot;
        public readonly VisualElement?[] Trail = new VisualElement?[TrailMaxCircles];
        public readonly List<Vector2> TrailHistory = new(TrailMaxCircles);
    }

    public CombatFxTracerPresenter(
        VisualElement host,
        TacticalViewportCamera camera,
        ModuleRegistry modules,
        ShipRegistry ships)
    {
        _host = host;
        _camera = camera;
        _modules = modules;
        _ships = ships;
    }

    public void Refresh(GameState? state, BattlefieldState? bf, float wallDtSec)
    {
        if (!ClientGameSettings.CombatFxEnabled || state == null || bf == null || _host == null)
        {
            ClearAll();
            bf?.pendingCombatFx.Clear();
            return;
        }

        var dt = Mathf.Min(Mathf.Max(0f, wallDtSec), MaxAdvanceDtSec);
        DrainPending(bf);
        Advance(state, bf, dt);
        // #region agent log
        if (_active.Count > 0 || (bf.pendingCombatFx?.Count ?? 0) > 0)
        {
            CombatFxAgentLog.Write(
                "B",
                "CombatFxTracerPresenter.Refresh",
                "refresh",
                "{\"active\":" + _active.Count
                + ",\"dt\":" + dt.ToString("F3")
                + ",\"wallDt\":" + wallDtSec.ToString("F3") + "}");
        }
        // #endregion
    }

    public void ClearAll()
    {
        for (var i = 0; i < _active.Count; i++)
        {
            DestroyTracer(_active[i]);
        }

        _active.Clear();
    }

    private void DrainPending(BattlefieldState bf)
    {
        if (bf.pendingCombatFx.Count == 0)
        {
            return;
        }

        foreach (var ev in bf.pendingCombatFx)
        {
            if (ev == null
                || !CombatFxEvent.KindHybridGunTracer.Equals(ev.kind, System.StringComparison.Ordinal)
                || string.IsNullOrEmpty(ev.firerUnitId)
                || string.IsNullOrEmpty(ev.targetUnitId))
            {
                continue;
            }

            var duration = CombatFxEmit.ResolveTracerDurationSec(ev.distAtSpawnM);
            if (duration <= 0f)
            {
                continue;
            }

            Spawn(bf, ev.firerUnitId!, ev.targetUnitId!, duration);
        }

        bf.pendingCombatFx.Clear();
    }

    private void Spawn(BattlefieldState bf, string firerId, string targetId, float durationSec)
    {
        var firer = FindUnit(bf, firerId);
        var target = FindUnit(bf, targetId);
        if (firer == null || target == null)
        {
            return;
        }

        var dot = MakeDot(DotSizePx, DotColor);
        _host.Add(dot);
        var tracer = new ActiveTracer
        {
            FirerId = firerId,
            TargetId = targetId,
            DurationSec = durationSec,
            ElapsedSec = 0f,
            StartPosePending = true,
            Ax = firer.x,
            Ay = firer.y,
            Az = firer.z,
            Bx = target.x,
            By = target.y,
            Bz = target.z,
            Dot = dot,
        };
        TryClipEndpointToFieldShell(bf, tracer, firer, target);
        if (tracer.EndpointClippedToShell)
        {
            var clippedDist = Mathf.Sqrt(
                (tracer.Bx - tracer.Ax) * (tracer.Bx - tracer.Ax)
                + (tracer.By - tracer.Ay) * (tracer.By - tracer.Ay)
                + (tracer.Bz - tracer.Az) * (tracer.Bz - tracer.Az));
            tracer.DurationSec = CombatFxEmit.ResolveTracerDurationSec(clippedDist);
        }

        for (var i = 0; i < TrailMaxCircles; i++)
        {
            var trail = MakeDot(TrailMinSizePx, TrailColor);
            trail.style.display = DisplayStyle.None;
            _host.Add(trail);
            tracer.Trail[i] = trail;
        }

        _active.Add(tracer);
        // #region agent log
        CombatFxAgentLog.Write(
            "J",
            "CombatFxTracerPresenter.Spawn",
            "spawn",
            "{\"firer\":\"" + firerId + "\",\"target\":\"" + targetId
            + "\",\"dur\":" + tracer.DurationSec.ToString("F2")
            + ",\"clipped\":" + (tracer.EndpointClippedToShell ? "true" : "false")
            + ",\"startPose\":true,\"host\":\"" + (_host.name ?? "") + "\"}");
        // #endregion
    }

    /// <summary>场外→场内：弹道终点夹到与 VFX 同半径的球壳表面。</summary>
    private void TryClipEndpointToFieldShell(
        BattlefieldState bf,
        ActiveTracer tr,
        BattlefieldUnit firer,
        BattlefieldUnit target)
    {
        if (!TryResolveFieldShell(bf, target, out var host, out var radiusM) || radiusM <= 1f)
        {
            return;
        }

        var firerDist = FieldAuraService.DistanceM(firer, host);
        if (firerDist < radiusM)
        {
            return;
        }

        var ox = firer.x - host.x;
        var oy = firer.y - host.y;
        var oz = firer.z - host.z;
        var dx = target.x - firer.x;
        var dy = target.y - firer.y;
        var dz = target.z - firer.z;
        var a = dx * dx + dy * dy + dz * dz;
        if (a < 1e-4f)
        {
            return;
        }

        var b = 2f * (ox * dx + oy * dy + oz * dz);
        var c = ox * ox + oy * oy + oz * oz - radiusM * radiusM;
        var disc = b * b - 4f * a * c;
        if (disc < 0f)
        {
            return;
        }

        var sqrt = Mathf.Sqrt(disc);
        var t0 = (-b - sqrt) / (2f * a);
        var t1 = (-b + sqrt) / (2f * a);
        var tHit = -1f;
        if (t0 >= 0f && t0 <= 1f)
        {
            tHit = t0;
        }
        else if (t1 >= 0f && t1 <= 1f)
        {
            tHit = t1;
        }

        if (tHit < 0f)
        {
            return;
        }

        tr.Bx = firer.x + dx * tHit;
        tr.By = firer.y + dy * tHit;
        tr.Bz = firer.z + dz * tHit;
        tr.EndpointClippedToShell = true;
        // #region agent log
        CombatFxAgentLog.Write(
            "K",
            "CombatFxTracerPresenter.TryClipEndpointToFieldShell",
            "clip",
            "{\"host\":\"" + (host.unitId ?? "") + "\""
            + ",\"radiusM\":" + radiusM.ToString("F0")
            + ",\"firerDist\":" + firerDist.ToString("F0")
            + ",\"tHit\":" + tHit.ToString("F3") + "}");
        // #endregion
    }

    private bool TryResolveFieldShell(
        BattlefieldState bf,
        BattlefieldUnit target,
        out BattlefieldUnit host,
        out float radiusM)
    {
        host = target;
        radiusM = 0f;
        BattlefieldUnit? candidate = null;
        ModuleDef? mod = null;

        if (!string.IsNullOrEmpty(target.shieldFieldHostUnitId))
        {
            candidate = FindUnit(bf, target.shieldFieldHostUnitId!);
            if (candidate != null)
            {
                mod = FieldAuraService.FindFieldModule(candidate, _modules, "shield_fusion_field");
            }
        }

        if (mod == null && !string.IsNullOrEmpty(target.armorFieldHostUnitId))
        {
            candidate = FindUnit(bf, target.armorFieldHostUnitId!);
            if (candidate != null)
            {
                mod = FieldAuraService.FindFieldModule(candidate, _modules, "armor_link_field");
            }
        }

        if (mod == null
            && target.fieldAuraEnabledAtSec > 0f
            && target.fieldAuraCollapseCooldownSec <= bf.timeSec)
        {
            candidate = target;
            if (target.fieldAuraShieldDominant)
            {
                mod = FieldAuraService.FindFieldModule(target, _modules, "shield_fusion_field");
            }

            if (mod == null && target.fieldAuraArmorDominant)
            {
                mod = FieldAuraService.FindFieldModule(target, _modules, "armor_link_field");
            }

            mod ??= FieldAuraService.FindFieldModule(target, _modules, "shield_fusion_field")
                    ?? FieldAuraService.FindFieldModule(target, _modules, "armor_link_field");
        }

        if (candidate == null || mod == null)
        {
            return false;
        }

        host = candidate;
        var hull = _ships.FindHull(host.hullId);
        radiusM = FieldAuraService.ResolveFieldRadiusM(host, mod, hull);
        return radiusM > 0f;
    }

    private static VisualElement MakeDot(float sizePx, Color color)
    {
        var el = new VisualElement();
        el.name = "combat-fx-tracer-dot";
        el.pickingMode = PickingMode.Ignore;
        el.style.position = Position.Absolute;
        el.style.width = sizePx;
        el.style.height = sizePx;
        el.style.borderTopLeftRadius = sizePx * 0.5f;
        el.style.borderTopRightRadius = sizePx * 0.5f;
        el.style.borderBottomLeftRadius = sizePx * 0.5f;
        el.style.borderBottomRightRadius = sizePx * 0.5f;
        el.style.backgroundColor = color;
        return el;
    }

    private void Advance(GameState state, BattlefieldState bf, float dt)
    {
        var focus = VisionAnchorService.ResolveDefaultFocus(state, bf);
        var fx = focus?.x ?? 0f;
        var fy = focus?.y ?? 0f;
        var fz = focus?.z ?? 0f;
        ResolveHostSize(out var hostW, out var hostH);
        var minSpacing = TrailLengthPx / Mathf.Max(1, TrailMaxCircles - 1);

        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var tr = _active[i];
            var firer = FindUnit(bf, tr.FirerId);
            var target = FindUnit(bf, tr.TargetId);
            if (firer != null && !firer.IsDestroyed())
            {
                tr.Ax = firer.x;
                tr.Ay = firer.y;
                tr.Az = firer.z;
            }

            if (target != null && !target.IsDestroyed() && !tr.EndpointClippedToShell)
            {
                tr.Bx = target.x;
                tr.By = target.y;
                tr.Bz = target.z;
            }

            // 首帧：贴开火点，不推时（避免 t>0 离舰 + 重表现间隔造成的「原地停」）
            float t;
            if (tr.StartPosePending)
            {
                tr.StartPosePending = false;
                tr.ElapsedSec = 0f;
                t = 0f;
            }
            else
            {
                tr.ElapsedSec += dt;
                t = tr.DurationSec > 1e-4f ? Mathf.Clamp01(tr.ElapsedSec / tr.DurationSec) : 1f;
            }

            var wx = tr.Ax + (tr.Bx - tr.Ax) * t;
            var wy = tr.Ay + (tr.By - tr.Ay) * t;
            var wz = tr.Az + (tr.Bz - tr.Az) * t;
            var proj = Project(wx - fx, wy - fy, wz - fz, hostW, hostH);
            var firerProj = Project(tr.Ax - fx, tr.Ay - fy, tr.Az - fz, hostW, hostH);

            var cx = proj.CenterX;
            var cy = proj.CenterY;
            var head = new Vector2(cx, cy);
            var show = proj.InFront && proj.OnScreen;
            if (tr.Dot != null)
            {
                tr.Dot.style.left = cx - DotSizePx * 0.5f;
                tr.Dot.style.top = cy - DotSizePx * 0.5f;
                tr.Dot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            PushTrailSample(tr, head, minSpacing);
            PaintTrail(tr, show);

            // #region agent log
            if (t <= 0.001f || tr.ElapsedSec <= dt + 0.001f)
            {
                CombatFxAgentLog.Write(
                    "J",
                    "CombatFxTracerPresenter.Advance",
                    "place",
                    "{\"t\":" + t.ToString("F3")
                    + ",\"cx\":" + cx.ToString("F1")
                    + ",\"cy\":" + cy.ToString("F1")
                    + ",\"firerCx\":" + firerProj.CenterX.ToString("F1")
                    + ",\"firerCy\":" + firerProj.CenterY.ToString("F1")
                    + ",\"dFirer\":" + Vector2.Distance(head, new Vector2(firerProj.CenterX, firerProj.CenterY)).ToString("F1")
                    + ",\"show\":" + (show ? "true" : "false")
                    + ",\"trailN\":" + tr.TrailHistory.Count
                    + ",\"hostW\":" + hostW.ToString("F0")
                    + ",\"host\":\"" + (_host.name ?? "") + "\"}");
            }
            // #endregion

            if (t >= 1f)
            {
                DestroyTracer(tr);
                _active.RemoveAt(i);
            }
        }
    }

    private void ResolveHostSize(out float hostW, out float hostH)
    {
        hostW = _host.worldBound.width;
        hostH = _host.worldBound.height;
        if (float.IsNaN(hostW) || hostW < 1f)
        {
            hostW = _host.resolvedStyle.width;
        }

        if (float.IsNaN(hostH) || hostH < 1f)
        {
            hostH = _host.resolvedStyle.height;
        }

        if (float.IsNaN(hostW) || hostW < 1f)
        {
            hostW = 400f;
        }

        if (float.IsNaN(hostH) || hostH < 1f)
        {
            hostH = 300f;
        }
    }

    private TacticalViewportCamera.ScreenProjection Project(
        float dx,
        float dy,
        float dz,
        float hostW,
        float hostH)
    {
        if (_camera != null)
        {
            return _camera.ProjectWorldOffset(dx, dy, dz, hostW, hostH);
        }

        return new TacticalViewportCamera.ScreenProjection(
            hostW * 0.5f + dx * 0.02f,
            hostH * 0.5f - dy * 0.02f,
            0f,
            0f,
            true,
            true);
    }

    private static void PushTrailSample(ActiveTracer tr, Vector2 head, float minSpacing)
    {
        var hist = tr.TrailHistory;
        if (hist.Count == 0)
        {
            hist.Add(head);
            return;
        }

        var prev = hist[0];
        var dist = Vector2.Distance(prev, head);
        if (dist < 0.05f)
        {
            hist[0] = head;
            return;
        }

        if (dist < minSpacing)
        {
            hist[0] = head;
            return;
        }

        // 单帧位移常 >20px：沿线段细分，否则拖尾会被长度裁成 1 点
        var steps = Mathf.Max(1, Mathf.CeilToInt(dist / minSpacing));
        for (var s = 1; s <= steps; s++)
        {
            hist.Insert(0, Vector2.Lerp(prev, head, s / (float)steps));
        }

        while (hist.Count > TrailMaxCircles)
        {
            hist.RemoveAt(hist.Count - 1);
        }

        while (hist.Count > 1 && PathLengthPx(hist) > TrailLengthPx + 0.01f)
        {
            hist.RemoveAt(hist.Count - 1);
        }
    }

    private static float PathLengthPx(List<Vector2> hist)
    {
        var len = 0f;
        for (var i = 0; i < hist.Count - 1; i++)
        {
            len += Vector2.Distance(hist[i], hist[i + 1]);
        }

        return len;
    }

    private static void PaintTrail(ActiveTracer tr, bool show)
    {
        var n = tr.TrailHistory.Count;
        for (var s = 0; s < TrailMaxCircles; s++)
        {
            var el = tr.Trail[s];
            if (el == null)
            {
                continue;
            }

            if (!show || s >= n)
            {
                el.style.display = DisplayStyle.None;
                continue;
            }

            var u = TrailMaxCircles <= 1 ? 0f : s / (float)(TrailMaxCircles - 1);
            var size = Mathf.Lerp(DotSizePx * 0.95f, TrailMinSizePx, u);
            var a = Mathf.Lerp(TrailColor.a, 0.12f, u);
            var p = tr.TrailHistory[s];
            el.style.width = size;
            el.style.height = size;
            el.style.borderTopLeftRadius = size * 0.5f;
            el.style.borderTopRightRadius = size * 0.5f;
            el.style.borderBottomLeftRadius = size * 0.5f;
            el.style.borderBottomRightRadius = size * 0.5f;
            el.style.left = p.x - size * 0.5f;
            el.style.top = p.y - size * 0.5f;
            el.style.backgroundColor = new Color(TrailColor.r, TrailColor.g, TrailColor.b, a);
            el.style.display = DisplayStyle.Flex;
        }
    }

    private static BattlefieldUnit? FindUnit(BattlefieldState bf, string unitId)
    {
        foreach (var u in bf.units)
        {
            if (unitId.Equals(u.unitId, System.StringComparison.Ordinal))
            {
                return u;
            }
        }

        return null;
    }

    private static void DestroyTracer(ActiveTracer tr)
    {
        tr.Dot?.RemoveFromHierarchy();
        tr.Dot = null;
        tr.TrailHistory.Clear();
        for (var i = 0; i < TrailMaxCircles; i++)
        {
            tr.Trail[i]?.RemoveFromHierarchy();
            tr.Trail[i] = null;
        }
    }
}
