using System.Collections.Generic;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using TopDog.Sim.Vision;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/// <summary>主视野单位 marker + EVE 环绕 HUD + 屏外 bracket（TACTICAL_VIEW.md §4–5）。</summary>
public sealed class TacticalViewportPresenter
{
    private const float MarkerHalf = 16f;
    private const float EdgePad = 6f;

    private readonly VisualElement _host;
    private readonly TacticalViewportCamera _camera;
    private readonly Dictionary<string, MarkerBundle> _markers = new();
    private readonly Dictionary<string, (float left, float top)> _screenPositions = new();
    private GameState _lastState;
    private BattlefieldState _lastBf;

    public (float left, float top)? SelectedMarkerScreenPos { get; private set; }

    private sealed class MarkerBundle
    {
        public VisualElement Container;
        public UnitOrbitHudWidget Hud;
        public VisualElement IconHost;
    }

    public TacticalViewportPresenter(VisualElement markersHost, TacticalViewportCamera camera = null)
    {
        _host = markersHost;
        _camera = camera;
    }

    public void Refresh(GameState state, BattlefieldState bf)
    {
        _lastState = state;
        _lastBf = bf;
        SelectedMarkerScreenPos = null;
        _screenPositions.Clear();
        if (bf == null || _host == null)
        {
            ClearMarkers();
            return;
        }

        var focus = VisionAnchorService.ResolveDefaultFocus(state, bf);
        var fx = focus?.x ?? 0f;
        var fy = focus?.y ?? 0f;
        var fz = focus?.z ?? 0f;
        var scale = _camera != null ? _camera.WorldScale : 0.02f;
        var aliveIds = new HashSet<string>();
        var hostW = _host.resolvedStyle.width;
        var hostH = _host.resolvedStyle.height;
        if (float.IsNaN(hostW) || hostW < 1f) hostW = 400f;
        if (float.IsNaN(hostH) || hostH < 1f) hostH = 300f;

        foreach (var u in bf.units)
        {
            if (u.IsDestroyed() || !u.Arrived(bf.timeSec) || u.unitId == null)
            {
                continue;
            }
            aliveIds.Add(u.unitId);
            var bundle = GetOrCreateMarker(u.unitId);
            var dx = u.x - fx;
            var dy = u.y - fy;
            var dz = u.z - fz;
            if (_camera != null)
            {
                _camera.TransformOffset(dx, dy, dz, out dx, out dy);
            }
            PositionMarker(bundle.Container, dx, dy, scale, hostW, hostH);
            UpdateMarkerVisual(bundle, u, state, bf);

            var left = bundle.Container.resolvedStyle.left;
            var top = bundle.Container.resolvedStyle.top;
            _screenPositions[u.unitId] = (left + MarkerHalf, top + MarkerHalf);

            if (u.unitId.Equals(TacticalSelectionState.SelectedTargetUnitId, System.StringComparison.Ordinal))
            {
                SelectedMarkerScreenPos = (left + MarkerHalf, top + MarkerHalf);
            }
        }

        var remove = new List<string>();
        foreach (var kv in _markers)
        {
            if (!aliveIds.Contains(kv.Key))
            {
                remove.Add(kv.Key);
            }
        }
        foreach (var id in remove)
        {
            _host.Remove(_markers[id].Container);
            _markers.Remove(id);
        }
    }

    public IReadOnlyList<string> UnitsInScreenRect(Vector2 a, Vector2 b, bool onlyFriendly)
    {
        var hits = new List<string>();
        if (_lastBf == null)
        {
            return hits;
        }
        var left = Mathf.Min(a.x, b.x);
        var right = Mathf.Max(a.x, b.x);
        var top = Mathf.Min(a.y, b.y);
        var bottom = Mathf.Max(a.y, b.y);
        if (right - left < 4f && bottom - top < 4f)
        {
            return hits;
        }
        foreach (var u in _lastBf.units)
        {
            if (u.unitId == null || u.IsDestroyed() || !u.Arrived(_lastBf.timeSec))
            {
                continue;
            }
            if (onlyFriendly && u.side != UnitSide.FRIENDLY)
            {
                continue;
            }
            if (!_screenPositions.TryGetValue(u.unitId, out var pos))
            {
                continue;
            }
            if (pos.left >= left && pos.left <= right && pos.top >= top && pos.top <= bottom)
            {
                hits.Add(u.unitId);
            }
        }
        return hits;
    }

    public string? PickUnitAt(Vector2 localPos, float radiusPx = 22f)
    {
        string? bestId = null;
        var bestDist = float.MaxValue;
        foreach (var kv in _screenPositions)
        {
            var d = Vector2.Distance(localPos, new Vector2(kv.Value.left, kv.Value.top));
            if (d <= radiusPx && d < bestDist)
            {
                bestDist = d;
                bestId = kv.Key;
            }
        }

        return bestId;
    }

    public void FlashCommandAck(IReadOnlyCollection<string> unitIds)
    {
        if (unitIds == null)
        {
            return;
        }
        foreach (var id in unitIds)
        {
            if (_markers.TryGetValue(id, out var bundle))
            {
                bundle.Hud.FlashCommandAck();
            }
        }
    }

    private MarkerBundle GetOrCreateMarker(string unitId)
    {
        if (_markers.TryGetValue(unitId, out var bundle))
        {
            return bundle;
        }

        var container = new VisualElement();
        container.AddToClassList("rtcombat-marker-container");
        container.name = "marker-" + unitId;

        var hud = new UnitOrbitHudWidget();
        container.Add(hud.Root);

        var marker = new VisualElement();
        marker.AddToClassList("rtcombat-marker");
        var icon = new VisualElement();
        icon.AddToClassList("rtcombat-marker-icon");
        var fallback = new Label();
        fallback.name = "marker-fallback";
        fallback.AddToClassList("rtcombat-marker-fallback");
        fallback.style.display = DisplayStyle.None;
        marker.Add(icon);
        marker.Add(fallback);
        var badge = new Label();
        badge.name = "marker-badge";
        badge.AddToClassList("rtcombat-marker-badge");
        marker.Add(badge);
        container.Add(marker);

        container.RegisterCallback<ClickEvent>(evt =>
        {
            TacticalSelectionState.SetSelectedTarget(unitId);
            evt.StopPropagation();
        });

        _host.Add(container);
        bundle = new MarkerBundle { Container = container, Hud = hud, IconHost = marker };
        _markers[unitId] = bundle;
        return bundle;
    }

    private static void PositionMarker(
        VisualElement marker,
        float dx,
        float dy,
        float worldScale,
        float hostW,
        float hostH)
    {
        var cx = hostW * 0.5f + dx * worldScale;
        var cy = hostH * 0.5f - dy * worldScale;
        var left = cx - MarkerHalf;
        var top = cy - MarkerHalf;
        var offscreen = left < EdgePad || top < EdgePad
            || left > hostW - EdgePad - MarkerHalf * 2f
            || top > hostH - EdgePad - MarkerHalf * 2f;
        if (offscreen)
        {
            left = Mathf.Clamp(left, EdgePad, hostW - EdgePad - MarkerHalf * 2f);
            top = Mathf.Clamp(top, EdgePad, hostH - EdgePad - MarkerHalf * 2f);
            marker.AddToClassList("rtcombat-marker-offscreen");
        }
        else
        {
            marker.RemoveFromClassList("rtcombat-marker-offscreen");
        }
        marker.style.left = left;
        marker.style.top = top;
    }

    private void UpdateMarkerVisual(MarkerBundle bundle, BattlefieldUnit u, GameState state, BattlefieldState bf)
    {
        var marker = bundle.IconHost;
        var icon = marker.Q(className: "rtcombat-marker-icon");
        var fallback = marker.Q<Label>("marker-fallback");
        var badge = marker.Q<Label>("marker-badge");
        if (icon != null)
        {
            var tex = TacticalIconCatalog.ResolveShipIcon(u.tonnageClass);
            if (tex != null)
            {
                icon.style.backgroundImage = new StyleBackground(tex);
                icon.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0));
                if (fallback != null) fallback.style.display = DisplayStyle.None;
            }
            else
            {
                icon.style.backgroundImage = StyleKeyword.None;
                icon.style.backgroundColor = new StyleColor(
                    u.side == UnitSide.ENEMY
                        ? new Color(0.55f, 0.15f, 0.15f, 0.85f)
                        : new Color(0.15f, 0.35f, 0.65f, 0.85f));
                if (fallback != null)
                {
                    var tc = u.tonnageClass ?? "?";
                    fallback.text = tc.Length >= 2 ? tc.Substring(0, 2) : tc;
                    fallback.style.display = DisplayStyle.Flex;
                }
            }
            icon.style.rotate = new Rotate(new Angle(u.facingRad * Mathf.Rad2Deg, AngleUnit.Degree));
        }
        if (badge != null)
        {
            if (u.isBuilding)
            {
                badge.text = "";
                badge.style.display = DisplayStyle.None;
            }
            else if (u.side == UnitSide.ENEMY)
            {
                badge.text = "−";
                badge.RemoveFromClassList("rtcombat-marker-badge-friendly");
                badge.AddToClassList("rtcombat-marker-badge-hostile");
                badge.style.display = DisplayStyle.Flex;
            }
            else
            {
                badge.text = "+";
                badge.RemoveFromClassList("rtcombat-marker-badge-hostile");
                badge.AddToClassList("rtcombat-marker-badge-friendly");
                badge.style.display = DisplayStyle.Flex;
            }
        }

        var selected = u.unitId != null
            && u.unitId.Equals(TacticalSelectionState.SelectedTargetUnitId, System.StringComparison.Ordinal);
        var boxSel = TacticalSelectionState.IsFriendlySelected(u.unitId);
        if (selected || boxSel)
        {
            marker.AddToClassList("rtcombat-marker-selected");
        }
        else
        {
            marker.RemoveFromClassList("rtcombat-marker-selected");
        }

        BattlefieldUnit? rangeTarget = null;
        var targetId = TacticalSelectionState.SelectedTargetUnitId;
        if (targetId != null)
        {
            foreach (var other in bf.units)
            {
                if (targetId.Equals(other.unitId, System.StringComparison.Ordinal))
                {
                    rangeTarget = other;
                    break;
                }
            }
        }

        bundle.Hud.Refresh(u, state, bf, selected, boxSel, rangeTarget);
    }

    private void ClearMarkers()
    {
        _host?.Clear();
        _markers.Clear();
    }
}
