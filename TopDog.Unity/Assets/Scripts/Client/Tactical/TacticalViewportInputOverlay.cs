using System;
using System.Collections.Generic;
using TopDog.AgentDiag;
using TopDog.Client.Gestures;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §7 · docs/INPUT_PC_TOUCH_MAP.md
 * 本文件: TacticalViewportInputOverlay.cs — PC 中键/滚轮/左键框选 + 触摸映射
 * 【机制要点】
 * · PC：左键框选、右键指挥、中键 orbit、滚轮 zoom
 * · 触摸：单指拖 orbit、双指固定框选、捏合缩放、双击=右键导航
 * 【关联】TacticalSelectionState · TacticalViewportCamera · PointerActionMapper
 * ══
 */

namespace TopDog.Client.Tactical;

/// <summary>战术视野输入：PC 键位保留；触摸按 INPUT_PC_TOUCH_MAP。</summary>
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
    private readonly PointerActionMapper _touch = new();
    private bool _touchSession;

    public TacticalViewportInputOverlay()
    {
        name = "tactical-input-overlay";
        AddToClassList("rtcombat-input-overlay");
        pickingMode = PickingMode.Position;

        _selectionBox = new VisualElement();
        _selectionBox.AddToClassList("rtcombat-selection-box");
        _selectionBox.style.display = DisplayStyle.None;
        Add(_selectionBox);

        RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
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

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_camera == null)
        {
            return;
        }

        if (PointerActionMapper.IsTouchPointer(evt))
        {
            HandleTouchDown(evt);
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

    private void HandleTouchDown(PointerDownEvent evt)
    {
        _touchSession = true;
        _touch.OnDown(evt.pointerId, (Vector2)evt.localPosition);
        this.CapturePointer(evt.pointerId);

        if (_touch.ActiveCount >= 2 && _touch.TryGetTwoFingerCorners(out var a, out var b))
        {
            _middleDrag = false;
            _boxDrag = false;
            _selectionBox.style.display = DisplayStyle.Flex;
            UpdateSelectionBox(a, b);
        }

        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_camera == null)
        {
            return;
        }

        var pos = (Vector2)evt.localPosition;

        if (_touchSession && PointerActionMapper.IsTouchPointer(evt))
        {
            HandleTouchMove(evt, pos);
            return;
        }

        if (_middleDrag)
        {
            var delta = pos - _lastPointer;
            _lastPointer = pos;
            ApplyOrbitDelta(delta);
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
    }

    private void HandleTouchMove(PointerMoveEvent evt, Vector2 pos)
    {
        _touch.OnMove(evt.pointerId, pos);

        if (_touch.ActiveCount >= 2)
        {
            if (_touch.IsPinchMode)
            {
                _selectionBox.style.display = DisplayStyle.None;
                var zoom = _touch.PinchZoomDelta();
                if (zoom < 0f)
                {
                    _camera.ZoomIn();
                }
                else if (zoom > 0f)
                {
                    _camera.ZoomOut();
                }

                if (zoom != 0f)
                {
                    _onCameraChanged?.Invoke();
                }
            }
            else if (_touch.TryGetTwoFingerCorners(out var a, out var b))
            {
                _selectionBox.style.display = DisplayStyle.Flex;
                UpdateSelectionBox(a, b);
            }

            evt.StopPropagation();
            return;
        }

        if (_touch.ActiveCount == 1 && _touch.IsOneFingerDragging)
        {
            var delta = _touch.OneFingerDeltaFromLast(pos);
            ApplyOrbitDelta(delta);
            evt.StopPropagation();
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (_touchSession && PointerActionMapper.IsTouchPointer(evt))
        {
            HandleTouchUp(evt);
            return;
        }

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
                ApplyBoxSelection(_dragStart, end, evt.shiftKey);
            }
            evt.StopPropagation();
        }
    }

    private Vector2 _pendingBoxA;
    private Vector2 _pendingBoxB;
    private bool _pendingBoxSelect;

    private void HandleTouchUp(PointerUpEvent evt)
    {
        var pos = (Vector2)evt.localPosition;
        var wasPinch = _touch.IsPinchMode;
        var wasDrag = _touch.IsOneFingerDragging;
        if (_touch.ActiveCount >= 2 && !_touch.IsPinchMode && _touch.TryGetTwoFingerCorners(out var a, out var b))
        {
            _pendingBoxA = a;
            _pendingBoxB = b;
            _pendingBoxSelect = true;
        }

        _touch.OnUp(evt.pointerId);
        ReleaseIfCaptured(evt);

        if (_touch.ActiveCount == 0)
        {
            _selectionBox.style.display = DisplayStyle.None;
            _touchSession = false;

            if (wasPinch)
            {
                _pendingBoxSelect = false;
                evt.StopPropagation();
                return;
            }

            if (_pendingBoxSelect)
            {
                _pendingBoxSelect = false;
                ApplyBoxSelection(_pendingBoxA, _pendingBoxB, additive: false);
                evt.StopPropagation();
                return;
            }

            if (!wasDrag)
            {
                if (_touch.TryConsumeDoubleTap(pos, out var isDouble) && isDouble)
                {
                    _onContextCommand?.Invoke(pos, 1);
                    evt.StopPropagation();
                    return;
                }

                var picked = _presenter?.PickUnitAt(pos);
                if (picked != null)
                {
                    TacticalSelectionState.SetSelectedTarget(picked);
                    _onUnitPicked?.Invoke(pos, picked);
                }
            }

            evt.StopPropagation();
            return;
        }

        evt.StopPropagation();
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
                _selectionBox.style.display = DisplayStyle.None;
            }
        }

        ReleaseIfCaptured(evt);
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (_touchSession)
        {
            return;
        }

        _middleDrag = false;
        _boxDrag = false;
        _selectionBox.style.display = DisplayStyle.None;
        TacticalSelectionState.HoveredUnitId = null;
        ReleaseIfCaptured(evt);
    }

    private void ApplyOrbitDelta(Vector2 delta)
    {
        if (delta.x > 0)
        {
            _camera.OrbitRight();
        }
        else if (delta.x < 0)
        {
            _camera.OrbitLeft();
        }

        if (delta.y > 0)
        {
            _camera.OrbitDown();
        }
        else if (delta.y < 0)
        {
            _camera.OrbitUp();
        }

        _onCameraChanged?.Invoke();
    }

    private void ReleaseIfCaptured(EventBase evt)
    {
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

        if (evt.delta.y < 0)
        {
            _camera.ZoomIn();
        }
        else if (evt.delta.y > 0)
        {
            _camera.ZoomOut();
        }

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
}
