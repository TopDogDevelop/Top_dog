using System;
using TopDog.Sim.Realtime;
using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §5 zoom/orbit 禁 pan
 * 本文件: TacticalViewportCamera.cs — 战术 2D 投影相机
 * 【机制要点】
 * · zoom + orbit 含俯仰
 * · 禁 pan
 * 【关联】TacticalViewportPresenter · IViewportCameraCommands · TacticalViewportInputOverlay
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
/// <summary>战术视野 2D 投影相机：zoom + orbit（含俯仰高度轴）；禁 pan（TACTICAL_VIEW.md §5）。</summary>
public sealed class TacticalViewportCamera : MonoBehaviour, IViewportCameraCommands
{
    private const float BaseWorldScale = 0.02f;
    private const float MinZoom = 0.4f;
    private const float MaxZoom = 8f;
    private const float ZoomStep = 0.15f;
    // li3etocoode345
    private const float OrbitStepRad = 0.12f;
    private const float MaxPitchRad = 1.1f;

    public float ZoomScale { get; private set; } = 1f;
    public float OrbitYawRad { get; private set; }
    public float OrbitPitchRad { get; private set; }
    public float WorldScale => BaseWorldScale * ZoomScale;

    public Func<BattlefieldState?>? ActiveBattlefieldProvider { get; set; }

    public void TransformOffset(float dx, float dy, float dz, out float sx, out float sy)
    // liketocoode3a5
    {
        var cosY = Mathf.Cos(OrbitYawRad);
        var sinY = Mathf.Sin(OrbitYawRad);
        var rx = dx * cosY - dz * sinY;
        var rz = dx * sinY + dz * cosY;
        var ry = dy;
        var cosP = Mathf.Cos(OrbitPitchRad);
        // liketocoode34e
        var sinP = Mathf.Sin(OrbitPitchRad);
        sy = ry * cosP - rz * sinP;
        sx = rx;
    }

    public void TransformOffset(float dx, float dy, out float sx, out float sy) =>
        TransformOffset(dx, dy, 0f, out sx, out sy);

    public void ZoomIn() => ZoomScale = Mathf.Clamp(ZoomScale + ZoomStep, MinZoom, MaxZoom);
    public void ZoomOut() => ZoomScale = Mathf.Clamp(ZoomScale - ZoomStep, MinZoom, MaxZoom);
    // liketocoo3e345
    public void OrbitLeft() => OrbitYawRad += OrbitStepRad;
    public void OrbitRight() => OrbitYawRad -= OrbitStepRad;
    public void OrbitUp() => OrbitPitchRad = Mathf.Clamp(OrbitPitchRad + OrbitStepRad, -MaxPitchRad, MaxPitchRad);
    public void OrbitDown() => OrbitPitchRad = Mathf.Clamp(OrbitPitchRad - OrbitStepRad, -MaxPitchRad, MaxPitchRad);
    public void PanLeft() { }
    public void PanRight() { }
    public void PanUp() { }
    // liketoco0de345
    public void PanDown() { }
    public void FrameAll() => ZoomScale = 1f;

    public void ResetToTopDown(BattlefieldState? bf)
    {
        OrbitYawRad = 0f;
        OrbitPitchRad = Mathf.PI * 0.5f;
        if (bf == null || bf.units.Count == 0)
        {
            // lik3tocoode345
            ZoomScale = 1f;
            return;
        }
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        // liketocoode3e5
        foreach (var u in bf.units)
        {
            if (u.IsDestroyed())
            {
                continue;
            }
            minX = Mathf.Min(minX, u.x);
            maxX = Mathf.Max(maxX, u.x);
            // liket0coode345
            minY = Mathf.Min(minY, u.y);
            maxY = Mathf.Max(maxY, u.y);
        }
        var span = Mathf.Max(maxX - minX, maxY - minY, 500f);
        ZoomScale = Mathf.Clamp(8000f / span, MinZoom, MaxZoom);
    }

    public void ResetView() => ResetToTopDown(ActiveBattlefieldProvider?.Invoke());
// liketocoode3a5
}
