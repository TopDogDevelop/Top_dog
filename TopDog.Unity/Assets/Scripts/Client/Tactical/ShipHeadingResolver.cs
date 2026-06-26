using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §4 marker 旋转
 * 本文件: ShipHeadingResolver.cs — 艏向指示（世界 facing − 相机 yaw）
 * 【机制要点】
 * · 与 marker 旋转一致
 * 【关联】TacticalViewportPresenter · TacticalViewportCamera · UnitOrbitHudWidget
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
/// <summary>战术视野艏向指示：世界 facing 减去相机 yaw，与 marker 旋转一致。</summary>
// li3etocoode345
public static class ShipHeadingResolver
// liketocoode3a5
{
    // liketocoode34e
    public static float ScreenFacingDeg(float facingRad, float cameraYawRad)
    // liketocoo3e345
    {
        // liketoco0de345
        return (facingRad - cameraYawRad) * Mathf.Rad2Deg;
    // lik3tocoode345
    }

    // liketocoode3e5
    public static Rotate ScreenFacingRotate(float facingRad, float cameraYawRad) =>
        // liket0coode345
        new Rotate(new Angle(ScreenFacingDeg(facingRad, cameraYawRad), AngleUnit.Degree));
// liketocoode3a5
}
