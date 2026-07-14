using TopDog.Client.Gestures;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_TWO_LAYER.md · docs/STARMAP.md · docs/INPUT_PC_TOUCH_MAP.md
 * 本文件: StarMapViewportInputOverlay.cs — 战略星图指针输入 overlay
 * 【机制要点】
 * · PC：中键 orbit、右键 pan、滚轮 zoom
 * · 触摸：单指拖 orbit、捏合缩放
 * 【关联】StarMapOrbitCamera · StarMapHostController · PointerActionMapper
 * ══
 */

namespace TopDog.Client.StarMap;

/// <summary>
/// Transparent overlay: wheel zoom, middle-button orbit, right-button pan; touch one-finger orbit + pinch zoom.
/// </summary>
public sealed class StarMapViewportInputOverlay : VisualElement
{
    private const int MiddleButton = 2;
    private const int RightButton = 1;

    private StarMapOrbitCamera? _orbit;
    private bool _middleDrag;
    private bool _rightDrag;
    private Vector2 _lastPointer;
    private readonly PointerActionMapper _touch = new();
    private bool _touchSession;

    public StarMapViewportInputOverlay()
    {
        name = "star-map-input-overlay";
        AddToClassList("ops-star-map-input-overlay");
        pickingMode = PickingMode.Position;
        RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
        RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
    }

    public void SetOrbitCamera(StarMapOrbitCamera? orbit) => _orbit = orbit;

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_orbit == null)
        {
            return;
        }

        if (PointerActionMapper.IsTouchPointer(evt))
        {
            _touchSession = true;
            _touch.OnDown(evt.pointerId, (Vector2)evt.localPosition);
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
            return;
        }

        if (evt.button == MiddleButton)
        {
            _middleDrag = true;
            _lastPointer = (Vector2)evt.localPosition;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
            return;
        }

        if (evt.button == RightButton)
        {
            _rightDrag = true;
            _lastPointer = (Vector2)evt.localPosition;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_orbit == null)
        {
            return;
        }

        if (_touchSession && PointerActionMapper.IsTouchPointer(evt))
        {
            var pos = (Vector2)evt.localPosition;
            _touch.OnMove(evt.pointerId, pos);
            if (_touch.ActiveCount >= 2 && _touch.IsPinchMode)
            {
                var zoom = _touch.PinchZoomDelta();
                if (zoom != 0f)
                {
                    _orbit.ZoomByWheel(zoom, 0f);
                }
            }
            else if (_touch.ActiveCount == 1 && _touch.IsOneFingerDragging)
            {
                var delta = _touch.OneFingerDeltaFromLast(pos);
                _orbit.OrbitBy(delta.x, delta.y);
            }

            evt.StopPropagation();
            return;
        }

        var mouseDelta = (Vector2)evt.localPosition - _lastPointer;
        _lastPointer = (Vector2)evt.localPosition;

        if (_middleDrag)
        {
            _orbit.OrbitBy(mouseDelta.x, mouseDelta.y);
            evt.StopPropagation();
            return;
        }

        if (_rightDrag)
        {
            _orbit.PanBy(mouseDelta.x, mouseDelta.y);
            evt.StopPropagation();
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (_touchSession && PointerActionMapper.IsTouchPointer(evt))
        {
            _touch.OnUp(evt.pointerId);
            if (this.HasPointerCapture(evt.pointerId))
            {
                this.ReleasePointer(evt.pointerId);
            }

            if (_touch.ActiveCount == 0)
            {
                _touch.Clear();
                _touchSession = false;
            }

            evt.StopPropagation();
            return;
        }

        if (evt.button == MiddleButton && _middleDrag)
        {
            _middleDrag = false;
            if (this.HasPointerCapture(evt.pointerId))
            {
                this.ReleasePointer(evt.pointerId);
            }

            evt.StopPropagation();
            return;
        }

        if (evt.button == RightButton && _rightDrag)
        {
            _rightDrag = false;
            if (this.HasPointerCapture(evt.pointerId))
            {
                this.ReleasePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }
    }

    private void OnPointerCancel(PointerCancelEvent evt)
    {
        if (_touchSession)
        {
            _touch.OnUp(evt.pointerId);
            if (_touch.ActiveCount == 0)
            {
                _touch.Clear();
                _touchSession = false;
            }
        }

        if (this.HasPointerCapture(evt.pointerId))
        {
            this.ReleasePointer(evt.pointerId);
        }
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (_touchSession)
        {
            return;
        }

        _middleDrag = false;
        _rightDrag = false;
        if (this.HasPointerCapture(evt.pointerId))
        {
            this.ReleasePointer(evt.pointerId);
        }
    }

    private void OnWheel(WheelEvent evt)
    {
        if (_orbit == null)
        {
            return;
        }

        _orbit.ZoomByWheel(evt.delta.y, evt.delta.x);
        evt.StopPropagation();
    }
}
