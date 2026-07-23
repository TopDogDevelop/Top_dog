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
 * 权威: docs/FIELD_AURA_MODULES.md §6 · docs/COMBAT_FX.md
 * 本文件: FieldAuraUitkPresenter.cs — 场域噪声球壳投影（UITK · 可见兜底/俯视主路径）
 * 【机制要点】
 * · 与弹道同套 ProjectWorldOffset；直径 = 2×ResolveFieldRadiusM
 * · SG 噪声合成环（壳缘亮、体内半透明）；CombatFxEnabled 门控
 * 【关联】FieldAuraVfxCatalog · FieldAuraVfxPresenter · CombatRealtimeController
 * ══
 */

namespace TopDog.Client.Tactical;

public sealed class FieldAuraUitkPresenter
{
    private readonly VisualElement _host;
    private readonly TacticalViewportCamera _camera;
    private readonly ShipRegistry _ships;
    private readonly ModuleRegistry _modules;
    private readonly Dictionary<string, VisualElement> _discs = new();

    public FieldAuraUitkPresenter(
        VisualElement host,
        TacticalViewportCamera camera,
        ShipRegistry ships,
        ModuleRegistry modules)
    {
        _host = host;
        _camera = camera;
        _ships = ships;
        _modules = modules;
    }

    public void Refresh(GameState? state, BattlefieldState? bf)
    {
        if (!ClientGameSettings.CombatFxEnabled || state == null || bf == null || _host == null)
        {
            ClearAll();
            return;
        }

        ResolveHostSize(out var hostW, out var hostH);
        var focus = VisionAnchorService.ResolveDefaultFocus(state, bf);
        var fx = focus?.x ?? 0f;
        var fy = focus?.y ?? 0f;
        var fz = focus?.z ?? 0f;

        var expected = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var holder in bf.units)
        {
            if (holder.IsDestroyed() || holder.isBuilding || holder.unitId == null)
            {
                continue;
            }

            if (holder.fieldAuraEnabledAtSec <= 0f
                || holder.fieldAuraCollapseCooldownSec > bf.timeSec)
            {
                continue;
            }

            var shieldMod = FieldAuraService.FindFieldModule(holder, _modules, "shield_fusion_field");
            var armorMod = FieldAuraService.FindFieldModule(holder, _modules, "armor_link_field");

            if (shieldMod != null && holder.fieldAuraShieldDominant)
            {
                var key = holder.unitId + "|shield";
                expected.Add(key);
                UpdateDisc(
                    holder,
                    shieldMod,
                    key,
                    FieldAuraVfxCatalog.ShieldUitkTexture,
                    FieldAuraVfxCatalog.ShieldTint,
                    fx,
                    fy,
                    fz,
                    hostW,
                    hostH);
            }

            if (armorMod != null && holder.fieldAuraArmorDominant)
            {
                var key = holder.unitId + "|armor";
                expected.Add(key);
                UpdateDisc(
                    holder,
                    armorMod,
                    key,
                    FieldAuraVfxCatalog.ArmorUitkTexture,
                    FieldAuraVfxCatalog.ArmorTint,
                    fx,
                    fy,
                    fz,
                    hostW,
                    hostH);
            }
        }

        var stale = new List<string>();
        foreach (var key in _discs.Keys)
        {
            if (!expected.Contains(key))
            {
                stale.Add(key);
            }
        }

        foreach (var key in stale)
        {
            if (_discs.TryGetValue(key, out var ve) && ve != null)
            {
                ve.RemoveFromHierarchy();
            }

            _discs.Remove(key);
        }

        // #region agent log
        if (expected.Count > 0 && Time.frameCount % 90 == 0)
        {
            CombatFxAgentLog.Write(
                "C",
                "FieldAuraUitkPresenter.Refresh",
                "uitk-discs",
                "{\"count\":" + expected.Count
                + ",\"host\":\"" + (_host != null ? _host.name : "null") + "\"}");
        }
        // #endregion
    }

    public void ClearAll()
    {
        foreach (var ve in _discs.Values)
        {
            ve?.RemoveFromHierarchy();
        }

        _discs.Clear();
    }

    private void UpdateDisc(
        BattlefieldUnit holder,
        ModuleDef mod,
        string key,
        Texture2D? tex,
        Color tint,
        float fx,
        float fy,
        float fz,
        float hostW,
        float hostH)
    {
        if (!_discs.TryGetValue(key, out var ve) || ve == null)
        {
            ve = new VisualElement { name = "field-aura-uitk-" + key };
            ve.pickingMode = PickingMode.Ignore;
            ve.style.position = Position.Absolute;
            if (tex != null)
            {
                ve.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
            }

            ve.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
            ve.style.unityBackgroundImageTintColor = tint;
            ve.style.borderTopLeftRadius = Length.Percent(50);
            ve.style.borderTopRightRadius = Length.Percent(50);
            ve.style.borderBottomLeftRadius = Length.Percent(50);
            ve.style.borderBottomRightRadius = Length.Percent(50);
            _host.Add(ve);
            // Keep discs under ship markers: insert at back of host children.
            ve.SendToBack();
            _discs[key] = ve;
        }

        var hull = holder.hullId != null ? _ships.FindHull(holder.hullId) : null;
        var radiusM = FieldAuraService.ResolveFieldRadiusM(holder, mod, hull);
        var cx = holder.x - fx;
        var cy = holder.y - fy;
        var cz = holder.z - fz;
        var center = Project(cx, cy, cz, hostW, hostH);
        var edge = Project(cx + radiusM, cy, cz, hostW, hostH);
        var radiusPx = Vector2.Distance(
            new Vector2(center.CenterX, center.CenterY),
            new Vector2(edge.CenterX, edge.CenterY));
        radiusPx = Mathf.Clamp(radiusPx, 8f, Mathf.Max(hostW, hostH) * 1.5f);
        var diameterPx = radiusPx * 2f;

        var show = center.InFront && diameterPx >= 4f;
        ve.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (!show)
        {
            return;
        }

        ve.style.width = diameterPx;
        ve.style.height = diameterPx;
        ve.style.left = center.CenterX - radiusPx;
        ve.style.top = center.CenterY - radiusPx;
        ve.style.opacity = 0.95f;

        // #region agent log
        if (Time.frameCount % 90 == 0)
        {
            CombatFxAgentLog.Write(
                "A",
                "FieldAuraUitkPresenter.UpdateDisc",
                "disc-align",
                "{\"key\":\"" + key.Replace("\"", "'") + "\""
                + ",\"cx\":" + center.CenterX.ToString("F1")
                + ",\"cy\":" + center.CenterY.ToString("F1")
                + ",\"rPx\":" + radiusPx.ToString("F1")
                + ",\"hostW\":" + hostW.ToString("F0")
                + ",\"hostH\":" + hostH.ToString("F0")
                + ",\"host\":\"" + (_host != null ? _host.name : "null") + "\""
                + ",\"hasTex\":" + (tex != null ? "true" : "false")
                + ",\"inFront\":" + (center.InFront ? "true" : "false")
                + ",\"radiusM\":" + radiusM.ToString("F0") + "}");
        }
        // #endregion
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
}
