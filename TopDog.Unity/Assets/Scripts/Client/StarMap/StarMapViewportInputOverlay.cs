using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.StarMap;

/// <summary>
/// Transparent overlay: wheel zoom, middle-button turn viewpoint (3D orbit), right-button pan.
/// </summary>
public sealed class StarMapViewportInputOverlay : VisualElement
{
    private const int MiddleButton = 2;
    private const int RightButton = 1;

    private StarMapOrbitCamera? _orbit;
    private bool _middleDrag;
    private bool _rightDrag;
    private Vector2 _lastPointer;

    public StarMapViewportInputOverlay()
    {
        name = "star-map-input-overlay";
        AddToClassList("ops-star-map-input-overlay");
        pickingMode = PickingMode.Position;
        RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
    }

    public void SetOrbitCamera(StarMapOrbitCamera? orbit) => _orbit = orbit;

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_orbit == null)
        {
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

        var delta = (Vector2)evt.localPosition - _lastPointer;
        _lastPointer = (Vector2)evt.localPosition;

        if (_middleDrag)
        {
            _orbit.OrbitBy(delta.x, delta.y);
            evt.StopPropagation();
            return;
        }

        if (_rightDrag)
        {
            _orbit.PanBy(delta.x, delta.y);
            evt.StopPropagation();
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
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

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
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
