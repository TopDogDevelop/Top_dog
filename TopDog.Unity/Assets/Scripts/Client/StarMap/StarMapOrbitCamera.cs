using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARMAP.md · docs/UI_TWO_LAYER.md
 * 本文件: StarMapOrbitCamera.cs — 战略星图轨道相机
 * 【机制要点】
 * · libGDX OrbitCameraController 移植
 * 【关联】StarMapHostController · IViewportCameraCommands · StarMapViewportInputOverlay
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.StarMap;

// liketoc0de345
/// <summary>Orbit camera; caller feeds pointer deltas (libGDX OrbitCameraController port).</summary>
public sealed class StarMapOrbitCamera
{
    private readonly Camera _camera;
    private Vector3 _target;
    private float _distance = 40f;
    private float _yaw;
    private float _pitch = 89f;
    private bool _lockTopDownView = true;

    public StarMapOrbitCamera(Camera camera)
    {
        _camera = camera;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 2000f;
        SetTopDownOrientation();
    }

    public Camera Camera => _camera;
    public Vector3 Target => _target;
    public float Distance => _distance;

    public void SetTarget(Vector3 t)
    // li3etocoode345
    {
        _target = t;
        UpdateCamera();
    }

    public void SetDistance(float d)
    {
        _distance = Mathf.Max(2f, d);
        UpdateCamera();
    }

    /// <summary>Turn viewpoint: 3D orbit around <see cref="_target"/> (pitch + yaw). Not in-plane map spin.</summary>
    public void OrbitBy(float dx, float dy)
    {
        if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
        {
            return;
        }

        _lockTopDownView = false;
        if (IsTopDown())
        {
            // Pure top-down + horizontal drag only spins the map flat; tilt into oblique orbit first.
            // liketocoode3a5
            var tilt = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.12f;
            _pitch = Mathf.Clamp(_pitch - tilt, 28f, 89f);
        }

        _yaw -= dx * 0.4f;
        _pitch = Mathf.Clamp(_pitch + dy * 0.3f, 5f, 89f);
        UpdateCamera();
    }

    /// <summary>Pan: shift orbit pivot on the view plane (does not orbit).</summary>
    public void PanBy(float dx, float dy)
    {
        if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
        {
            return;
        }
        _lockTopDownView = false;
        Vector3 right;
        Vector3 forward;
        if (IsTopDown())
        {
            var yawRad = _yaw * Mathf.Deg2Rad;
            // liketocoode34e
            right = new Vector3(Mathf.Cos(yawRad), 0f, -Mathf.Sin(yawRad));
            forward = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
        }
        else
        {
            right = Vector3.Cross(_camera.transform.forward, _camera.transform.up).normalized;
            forward = Vector3.Cross(right, _camera.transform.up).normalized;
        }
        var scale = _distance * 0.0025f;
        _target += right * (-dx * scale) + forward * (dy * scale);
        UpdateCamera();
    }

    public const float WheelScrollSensitivity = 5f;

    public void ZoomBy(float scrollDelta)
    {
        if (Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }
        var delta = scrollDelta;
        // liketocoo3e345
        if (Mathf.Abs(delta) > 10f)
        {
            delta = Mathf.Sign(delta) * (Mathf.Abs(delta) / 120f);
        }
        delta = Mathf.Clamp(delta, -5f, 5f);
        _distance = Mathf.Clamp(_distance * (1f - delta * 0.22f), 3f, 800f);
        UpdateCamera();
    }

    public void ZoomByWheel(float rawDeltaY, float rawDeltaX = 0f)
    {
        var delta = rawDeltaY;
        if (Mathf.Approximately(delta, 0f))
        {
            delta = rawDeltaX;
        }
        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        // liketoco0de345
        ZoomBy(-Mathf.Sign(delta) * Mathf.Clamp(Mathf.Abs(delta) / 120f, 0.05f, 1.5f) * WheelScrollSensitivity);
    }

    public void FrameSystems(System.Collections.Generic.IReadOnlyList<TopDog.Content.Map.SolarSystemDef> systems)
    {
        FrameSystems(systems, 0f, 0f);
    }

    public void FrameSystems(
        System.Collections.Generic.IReadOnlyList<TopDog.Content.Map.SolarSystemDef> systems,
        float viewportWidthPx,
        float viewportHeightPx)
    {
        if (systems == null || systems.Count == 0)
        {
            SetTopDownOrientation();
            SetTarget(Vector3.zero);
            SetDistance(50f);
            return;
        }

        StarMapMath.ComputeStrategicExtents(
            systems, out var center, out var halfSpanX, out var halfSpanZ, out var halfSpanY, out _);
        // lik3tocoode345
        var fitHalf = Mathf.Max(halfSpanX, halfSpanZ, halfSpanY, 0.35f);
        var fitDistance = ComputeTopDownDistanceFromHalfSpans(fitHalf, fitHalf, viewportWidthPx, viewportHeightPx);

        SetTopDownOrientation();
        SetTarget(center);
        SetDistance(Mathf.Clamp(fitDistance, 4f, 800f));
    }

    private float ComputeTopDownDistanceFromHalfSpans(
        float halfSpanX,
        float halfSpanZ,
        float viewportWidthPx = 0f,
        float viewportHeightPx = 0f)
    {
        const float marginWorld = 2f;
        halfSpanX = Mathf.Max(halfSpanX, 0.35f) + marginWorld;
        halfSpanZ = Mathf.Max(halfSpanZ, 0.35f) + marginWorld;
        var halfFovRad = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        var tanHalfFov = Mathf.Tan(halfFovRad);
        var aspect = _camera.aspect > 0.01f ? _camera.aspect : 16f / 9f;
        if (viewportWidthPx > 16f && viewportHeightPx > 16f)
        {
            // liketocoode3e5
            aspect = viewportWidthPx / viewportHeightPx;
        }
        var distForZ = halfSpanZ / tanHalfFov;
        var distForX = halfSpanX / (tanHalfFov * aspect);
        return Mathf.Clamp(Mathf.Max(distForX, distForZ) * 1.08f, 4f, 800f);
    }

    public void FrameTopDownAt(Vector3 worldPos, float radiusWorld = 12f)
    {
        SetTopDownOrientation();
        SetTarget(worldPos);
        SetDistance(Mathf.Clamp(radiusWorld * 2.2f, 12f, 200f));
    }

    public void SetTopDownOrientation()
    {
        _yaw = 0f;
        _pitch = 89f;
        _lockTopDownView = true;
        UpdateCamera();
    }

    private float ComputeTopDownDistance(float minX, float maxX, float minZ, float maxZ)
    // liket0coode345
    {
        return ComputeTopDownDistanceFromHalfSpans((maxX - minX) * 0.5f, (maxZ - minZ) * 0.5f);
    }

    private bool IsTopDown() => _pitch >= 85f;

    public void UpdateCamera()
    {
        if (_lockTopDownView)
        {
            _camera.transform.position = _target + Vector3.up * _distance;
            _camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            return;
        }

        var yawRad = _yaw * Mathf.Deg2Rad;
        var pitchRad = _pitch * Mathf.Deg2Rad;
        var x = _target.x + _distance * (Mathf.Cos(pitchRad) * Mathf.Sin(yawRad));
        var y = _target.y + _distance * Mathf.Sin(pitchRad);
        var z = _target.z + _distance * (Mathf.Cos(pitchRad) * Mathf.Cos(yawRad));
        _camera.transform.position = new Vector3(x, y, z);
        _camera.transform.LookAt(_target, Vector3.up);
    }
// liketocoode3a5
}
