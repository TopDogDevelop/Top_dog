using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/INPUT_PC_TOUCH_MAP.md
 * 本文件: PointerActionMapper.cs — 触摸手势归一（单指拖/双指框选/捏合/双击）
 * 【机制要点】
 * · 与 PC 鼠标键位并行；不替代 button==0/1/2
 * · 双指：间距超阈值→Zoom，否则固定对角框选
 * 【关联】TacticalViewportInputOverlay · StarMapViewportInputOverlay · MemberListView
 * ══
 */

namespace TopDog.Client.Gestures;

/// <summary>Tracks multi-touch pointers and classifies gestures for viewport overlays.</summary>
public sealed class PointerActionMapper
{
    public const float PinchDistanceThresholdPx = 24f;
    public const float DragSlopPx = 8f;
    public const float DoubleTapMaxMs = 320f;
    public const float DoubleTapMaxDistPx = 40f;

    private readonly Dictionary<int, Vector2> _pointers = new();
    private Vector2 _oneFingerStart;
    private Vector2 _oneFingerLast;
    private bool _oneFingerDragging;
    private float _pinchStartDist = -1f;
    private bool _pinchMode;
    private bool _boxMode;
    private Vector2 _lastTapPos;
    private float _lastTapUnscaledTime = -999f;

    public int ActiveCount => _pointers.Count;
    public bool IsPinchMode => _pinchMode;
    public bool IsBoxMode => _boxMode;
    public bool IsOneFingerDragging => _oneFingerDragging;

    public static bool IsTouchPointer(IPointerEvent evt) =>
        evt.pointerType == UnityEngine.UIElements.PointerType.touch
        || (evt.pointerId != PointerId.mousePointerId && evt.pointerId != PointerId.touchPointerIdBase - 1);

    public void Clear()
    {
        _pointers.Clear();
        _oneFingerDragging = false;
        _pinchMode = false;
        _boxMode = false;
        _pinchStartDist = -1f;
    }

    public void OnDown(int pointerId, Vector2 localPos)
    {
        _pointers[pointerId] = localPos;
        if (_pointers.Count == 1)
        {
            _oneFingerStart = localPos;
            _oneFingerLast = localPos;
            _oneFingerDragging = false;
            _pinchMode = false;
            _boxMode = false;
            _pinchStartDist = -1f;
        }
        else if (_pointers.Count == 2)
        {
            _oneFingerDragging = false;
            _pinchStartDist = CurrentDistance();
            _pinchMode = false;
            _boxMode = true;
        }
    }

    public void OnMove(int pointerId, Vector2 localPos)
    {
        if (!_pointers.ContainsKey(pointerId))
        {
            return;
        }

        _pointers[pointerId] = localPos;

        if (_pointers.Count == 1)
        {
            var delta = localPos - _oneFingerStart;
            if (!_oneFingerDragging && delta.magnitude >= DragSlopPx)
            {
                _oneFingerDragging = true;
            }

            // Do not advance _oneFingerLast here — callers use OneFingerDeltaFromLast(pos)
            // after OnMove; updating last here zeros the orbit/pan delta.
            return;
        }

        if (_pointers.Count >= 2 && _pinchStartDist > 0f)
        {
            var d = CurrentDistance();
            if (!_pinchMode && Mathf.Abs(d - _pinchStartDist) >= PinchDistanceThresholdPx)
            {
                _pinchMode = true;
                _boxMode = false;
            }
        }
    }

    public void OnUp(int pointerId)
    {
        _pointers.Remove(pointerId);
        if (_pointers.Count < 2)
        {
            _pinchMode = false;
            _boxMode = false;
            _pinchStartDist = -1f;
        }

        if (_pointers.Count == 0)
        {
            _oneFingerDragging = false;
        }
        else if (_pointers.Count == 1)
        {
            foreach (var kv in _pointers)
            {
                _oneFingerStart = kv.Value;
                _oneFingerLast = kv.Value;
                break;
            }

            _oneFingerDragging = false;
        }
    }

    public Vector2 OneFingerDeltaFromLast(Vector2 localPos)
    {
        var delta = localPos - _oneFingerLast;
        _oneFingerLast = localPos;
        return delta;
    }

    public bool TryGetTwoFingerCorners(out Vector2 a, out Vector2 b)
    {
        a = default;
        b = default;
        if (_pointers.Count < 2)
        {
            return false;
        }

        var i = 0;
        foreach (var kv in _pointers)
        {
            if (i == 0)
            {
                a = kv.Value;
            }
            else
            {
                b = kv.Value;
                return true;
            }

            i++;
        }

        return false;
    }

    public float PinchZoomDelta()
    {
        if (!_pinchMode || _pinchStartDist <= 0.01f || _pointers.Count < 2)
        {
            return 0f;
        }

        var d = CurrentDistance();
        // Positive = spread (zoom in), negative = pinch (zoom out) — match wheel: deltaY<0 zoom in
        var ratio = d / _pinchStartDist;
        _pinchStartDist = d;
        if (ratio > 1.01f)
        {
            return -1f;
        }

        if (ratio < 0.99f)
        {
            return 1f;
        }

        return 0f;
    }

    public bool TryConsumeDoubleTap(Vector2 localPos, out bool isDouble)
    {
        isDouble = false;
        var now = Time.unscaledTime * 1000f;
        if (now - _lastTapUnscaledTime <= DoubleTapMaxMs
            && (localPos - _lastTapPos).magnitude <= DoubleTapMaxDistPx)
        {
            isDouble = true;
            _lastTapUnscaledTime = -999f;
            return true;
        }

        _lastTapPos = localPos;
        _lastTapUnscaledTime = now;
        return true;
    }

    private float CurrentDistance()
    {
        if (!TryGetTwoFingerCorners(out var a, out var b))
        {
            return 0f;
        }

        return Vector2.Distance(a, b);
    }
}
