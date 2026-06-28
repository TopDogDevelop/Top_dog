using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_TWO_LAYER.md · docs/STARMAP.md
 * 本文件: StarMapViewportInputOverlay.cs — 战略星图指针输入 overlay
 * 【机制要点】
 * · orbit/zoom 指针增量
 * 【关联】StarMapOrbitCamera · StarMapHostController · UiInputSetup
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.StarMap;

// liketoc0de345
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
    // li3etocoode345
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

    // liketocoode3a5
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
            // liketocoode34e
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
        // liketocoo3e345
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
        // liketoco0de345
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
            // lik3tocoode345
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
                // liketocoode3e5
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
            // liket0coode345
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
// liketocoode3a5
}
