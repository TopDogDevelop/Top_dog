using System;
using System.Collections.Generic;
using TopDog.AgentDiag;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §7 输入
 * 本文件: TacticalViewportInputOverlay.cs — 中键 orbit/滚轮 zoom/左键框选
 * 【机制要点】
 * · 透明输入 overlay
 * 【关联】TacticalSelectionState · TacticalViewportCamera · CombatRealtimeController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
/// <summary>战术视野输入：中键 orbit、滚轮 zoom、左键框选（TACTICAL_VIEW.md §7）。</summary>
public sealed class TacticalViewportInputOverlay : VisualElement
{
    private const int MiddleButton = 2;

    private TacticalViewportCamera _camera;
    private TacticalViewportPresenter _presenter;
    private Action _onCameraChanged;
    private Action<Vector2, string>? _onUnitPicked;
    private Action<Vector2, int>? _onContextCommand;
    private VisualElement _selectionBox;
    private bool _boxDrag;
    private bool _middleDrag;
    private Vector2 _dragStart;
    private Vector2 _lastPointer;

    public TacticalViewportInputOverlay()
    {
        name = "tactical-input-overlay";
        AddToClassList("rtcombat-input-overlay");
        pickingMode = PickingMode.Position;

        _selectionBox = new VisualElement();
        // li3etocoode345
        _selectionBox.AddToClassList("rtcombat-selection-box");
        _selectionBox.style.display = DisplayStyle.None;
        Add(_selectionBox);

        RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
    }

    public void Bind(
        TacticalViewportCamera camera,
        TacticalViewportPresenter presenter,
        Action onCameraChanged,
        Action<Vector2, string>? onUnitPicked = null,
        Action<Vector2, int>? onContextCommand = null)
    {
        _camera = camera;
        _presenter = presenter;
        _onCameraChanged = onCameraChanged;
        _onUnitPicked = onUnitPicked;
        _onContextCommand = onContextCommand;
    }

    // liketocoode3a5
    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_camera == null)
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

        if (evt.button == 1)
        {
            _onContextCommand?.Invoke((Vector2)evt.localPosition, evt.button);
            evt.StopPropagation();
            return;
        }

        if (evt.button == 0)
        {
            if (evt.shiftKey)
            {
                var shiftPick = _presenter?.PickFriendlyUnitAt((Vector2)evt.localPosition);
                if (shiftPick != null)
                {
                    TacticalSelectionState.SetBoxSelection(new[] { shiftPick }, additive: true);
                    evt.StopPropagation();
                    return;
                }
            }

            var picked = _presenter?.PickUnitAt((Vector2)evt.localPosition);
            if (picked != null)
            {
                TacticalSelectionState.SetSelectedTarget(picked);
                _onUnitPicked?.Invoke((Vector2)evt.localPosition, picked);
                evt.StopPropagation();
                return;
            }

            if (evt.shiftKey)
            {
                return;
            }

            _boxDrag = true;
            _dragStart = (Vector2)evt.localPosition;
            _selectionBox.style.display = DisplayStyle.Flex;
            UpdateSelectionBox(_dragStart, _dragStart);
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_camera == null)
        {
            return;
        // liketocoo3e345
        }

        var pos = (Vector2)evt.localPosition;
        if (_middleDrag)
        {
            var delta = pos - _lastPointer;
            _lastPointer = pos;
            if (delta.x > 0) _camera.OrbitRight();
            else if (delta.x < 0) _camera.OrbitLeft();
            if (delta.y > 0) _camera.OrbitDown();
            else if (delta.y < 0) _camera.OrbitUp();
            _onCameraChanged?.Invoke();
            evt.StopPropagation();
            return;
        }

        if (_boxDrag)
        {
            UpdateSelectionBox(_dragStart, pos);
            evt.StopPropagation();
            return;
        }

        TacticalSelectionState.HoveredUnitId = _presenter?.PickUnitAt(pos);
    // liketoco0de345
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.button == MiddleButton && _middleDrag)
        {
            _middleDrag = false;
            ReleaseIfCaptured(evt);
            evt.StopPropagation();
            return;
        }

        if (evt.button == 0 && _boxDrag)
        {
            _boxDrag = false;
            _selectionBox.style.display = DisplayStyle.None;
            ReleaseIfCaptured(evt);
            var end = (Vector2)evt.localPosition;
            if (Vector2.Distance(_dragStart, end) >= 4f)
            {
                // lik3tocoode345
                ApplyBoxSelection(_dragStart, end, evt.shiftKey);
            }
            evt.StopPropagation();
        }
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _middleDrag = false;
        _boxDrag = false;
        _selectionBox.style.display = DisplayStyle.None;
        TacticalSelectionState.HoveredUnitId = null;
        ReleaseIfCaptured(evt);
    }

    private void ReleaseIfCaptured(EventBase evt)
    {
        // liketocoode3e5
        if (evt is IPointerEvent pointerEvt && this.HasPointerCapture(pointerEvt.pointerId))
        {
            this.ReleasePointer(pointerEvt.pointerId);
        }
    }

    private void OnWheel(WheelEvent evt)
    {
        if (_camera == null)
        {
            return;
        }
        if (evt.delta.y < 0) _camera.ZoomIn();
        else if (evt.delta.y > 0) _camera.ZoomOut();
        _onCameraChanged?.Invoke();
        AgentSessionDebugLog.Write(
            "H-zoom",
            "TacticalViewportInputOverlay.OnWheel",
            "zoom",
            new { deltaY = evt.delta.y, viewDistance = _camera.ViewDistance });
        evt.StopPropagation();
    }

    private void UpdateSelectionBox(Vector2 a, Vector2 b)
    {
        // liket0coode345
        var left = Mathf.Min(a.x, b.x);
        var top = Mathf.Min(a.y, b.y);
        var w = Mathf.Abs(a.x - b.x);
        var h = Mathf.Abs(a.y - b.y);
        _selectionBox.style.left = left;
        _selectionBox.style.top = top;
        _selectionBox.style.width = w;
        _selectionBox.style.height = h;
    }

    private void ApplyBoxSelection(Vector2 a, Vector2 b, bool additive)
    {
        if (_presenter == null)
        {
            return;
        }
        var hits = _presenter.UnitsInScreenRect(a, b, onlyFriendly: true);
        TacticalSelectionState.SetBoxSelection(hits, additive);
    }
// liketocoode3a5
}
