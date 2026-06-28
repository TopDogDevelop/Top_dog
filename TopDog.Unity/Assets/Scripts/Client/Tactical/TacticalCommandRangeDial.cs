using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §3.2 刻度盘
 * 本文件: TacticalCommandRangeDial — 底栏指令同心圆拖距 UI
 * 【机制要点】PointerDown 弹出；LMB 拖放 → KmFromDialT；单击不拖=默认 null km
 * 【关联】FleetCommandBar · TacticalRangeScale
 * ══
 */

/// <summary>以按钮为中心同心圆拖动选距（1–1000 km）。</summary>
public sealed class TacticalCommandRangeDial
{
    private const float MinDragPx = 12f;
    private readonly VisualElement _host;
    private readonly Label _kmLabel;
    private readonly VisualElement _ringOuter;
    private Button _anchorBtn;
    private Action<float?> _onRelease;
    private Vector2 _pressLocal;
    private bool _dragging;
    private bool _released;

    public TacticalCommandRangeDial(VisualElement root)
    {
        _host = root.Q<VisualElement>("range-dial-host") ?? root;
        _host.style.display = DisplayStyle.None;
        _host.pickingMode = PickingMode.Position;

        _ringOuter = new VisualElement();
        _ringOuter.AddToClassList("rtcombat-range-dial-ring");
        _host.Add(_ringOuter);

        _kmLabel = new Label("500 km");
        _kmLabel.AddToClassList("rtcombat-range-dial-km");
        _host.Add(_kmLabel);

        _host.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _host.RegisterCallback<PointerUpEvent>(OnPointerUp);
        _host.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    public void Begin(Button anchor, Action<float?> onRelease, float? initialKm = null)
    {
        Cancel();
        _anchorBtn = anchor;
        _onRelease = onRelease;
        _dragging = false;
        _released = false;

        var btnRect = anchor.worldBound;
        var size = Mathf.Max(btnRect.width, btnRect.height) * 5f;
        _host.style.left = btnRect.x + btnRect.width * 0.5f - size * 0.5f;
        _host.style.top = btnRect.y + btnRect.height * 0.5f - size * 0.5f;
        _host.style.width = size;
        _host.style.height = size;
        _ringOuter.style.width = size;
        _ringOuter.style.height = size;
        _host.style.display = DisplayStyle.Flex;
        _host.BringToFront();
        _host.CapturePointer(PointerId.mousePointerId);
        var t = initialKm.HasValue
            ? TopDog.Sim.Realtime.TacticalRangeScale.DialTFromKm(initialKm.Value)
            : 0.5f;
        UpdateKm(t);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_anchorBtn == null)
        {
            return;
        }

        var local = _host.WorldToLocal(evt.position);
        var center = new Vector2(_host.layout.width * 0.5f, _host.layout.height * 0.5f);
        if (!_dragging)
        {
            if (Vector2.Distance(local, center) > MinDragPx)
            {
                _dragging = true;
            }
            else
            {
                return;
            }
        }

        var delta = local - center;
        var angle = Mathf.Atan2(delta.y, delta.x);
        var t = (angle + Mathf.PI) / (2f * Mathf.PI);
        UpdateKm(t);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (_anchorBtn == null)
        {
            return;
        }

        float? km = null;
        if (_dragging)
        {
            var local = _host.WorldToLocal(evt.position);
            var center = new Vector2(_host.layout.width * 0.5f, _host.layout.height * 0.5f);
            var delta = local - center;
            var angle = Mathf.Atan2(delta.y, delta.x);
            var t = (angle + Mathf.PI) / (2f * Mathf.PI);
            km = TopDog.Sim.Realtime.TacticalRangeScale.KmFromDialT(t);
        }

        Finish(km);
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (_released || _anchorBtn == null)
        {
            Cancel();
            return;
        }

        Finish(null);
    }

    private void Finish(float? km)
    {
        if (_released)
        {
            return;
        }

        _released = true;
        var cb = _onRelease;
        Cancel();
        cb?.Invoke(km);
    }

    private void UpdateKm(float t)
    {
        var km = TopDog.Sim.Realtime.TacticalRangeScale.KmFromDialT(t);
        _kmLabel.text = $"{km:0} km";
    }

    public void Cancel()
    {
        if (_host.HasPointerCapture(PointerId.mousePointerId))
        {
            _host.ReleasePointer(PointerId.mousePointerId);
        }

        _host.style.display = DisplayStyle.None;
        _anchorBtn = null;
        _onRelease = null;
        _dragging = false;
    }
}
