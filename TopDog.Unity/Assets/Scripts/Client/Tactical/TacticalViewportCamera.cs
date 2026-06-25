using UnityEngine;

namespace TopDog.Client.Tactical;

/// <summary>战术视野 2D 投影相机：zoom + orbit（含俯仰高度轴）；禁 pan（TACTICAL_VIEW.md §5）。</summary>
public sealed class TacticalViewportCamera : MonoBehaviour, IViewportCameraCommands
{
    private const float BaseWorldScale = 0.02f;
    private const float MinZoom = 0.4f;
    private const float MaxZoom = 2.5f;
    private const float ZoomStep = 0.15f;
    private const float OrbitStepRad = 0.12f;
    private const float MaxPitchRad = 1.1f;

    public float ZoomScale { get; private set; } = 1f;
    public float OrbitYawRad { get; private set; }
    public float OrbitPitchRad { get; private set; }
    public float WorldScale => BaseWorldScale * ZoomScale;

    public void TransformOffset(float dx, float dy, float dz, out float sx, out float sy)
    {
        var cosY = Mathf.Cos(OrbitYawRad);
        var sinY = Mathf.Sin(OrbitYawRad);
        var rx = dx * cosY - dz * sinY;
        var rz = dx * sinY + dz * cosY;
        var ry = dy;
        var cosP = Mathf.Cos(OrbitPitchRad);
        var sinP = Mathf.Sin(OrbitPitchRad);
        sy = ry * cosP - rz * sinP;
        sx = rx;
    }

    public void TransformOffset(float dx, float dy, out float sx, out float sy) =>
        TransformOffset(dx, dy, 0f, out sx, out sy);

    public void ZoomIn() => ZoomScale = Mathf.Clamp(ZoomScale + ZoomStep, MinZoom, MaxZoom);
    public void ZoomOut() => ZoomScale = Mathf.Clamp(ZoomScale - ZoomStep, MinZoom, MaxZoom);
    public void OrbitLeft() => OrbitYawRad += OrbitStepRad;
    public void OrbitRight() => OrbitYawRad -= OrbitStepRad;
    public void OrbitUp() => OrbitPitchRad = Mathf.Clamp(OrbitPitchRad + OrbitStepRad, -MaxPitchRad, MaxPitchRad);
    public void OrbitDown() => OrbitPitchRad = Mathf.Clamp(OrbitPitchRad - OrbitStepRad, -MaxPitchRad, MaxPitchRad);
    public void PanLeft() { }
    public void PanRight() { }
    public void PanUp() { }
    public void PanDown() { }
    public void FrameAll() => ZoomScale = 1f;
    public void ResetView()
    {
        ZoomScale = 1f;
        OrbitYawRad = 0f;
        OrbitPitchRad = 0f;
    }
}
