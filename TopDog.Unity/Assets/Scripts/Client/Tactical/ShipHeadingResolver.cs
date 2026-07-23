using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VISUAL_ASSETS.md §5.1
 * 本文件: ShipHeadingResolver.cs — 舰标朝向工具（已停用）
 * 【机制要点】
 * · 产品定案：舰标固定朝向，不随移动方向旋转；保留工具类以防对照/回滚
 * 【关联】TacticalViewportPresenter（不再写入 style.rotate）
 * ══
 */

namespace TopDog.Client.Tactical;

/// <summary>战术舰标朝向工具（当前未接入 Refresh）。</summary>
public static class ShipHeadingResolver
{
    public const float ArtNoseUpOffsetDeg = 90f;
    public const float MinScreenDeltaPx = 0.75f;
    public const float MinAngleDeltaDeg = 5f;

    public static float ScreenTrailDeg(float dx, float dy) =>
        Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + ArtNoseUpOffsetDeg;

    public static float AbsDeltaDeg(float fromDeg, float toDeg)
    {
        var d = Mathf.DeltaAngle(fromDeg, toDeg);
        return Mathf.Abs(d);
    }

    public static bool TryResolveTrailDeg(
        float prevX,
        float prevY,
        float curX,
        float curY,
        float previousDeg,
        out float candidate)
    {
        var dx = curX - prevX;
        var dy = curY - prevY;
        candidate = previousDeg;
        if (dx * dx + dy * dy < MinScreenDeltaPx * MinScreenDeltaPx)
        {
            return false;
        }

        candidate = ScreenTrailDeg(dx, dy);
        if (AbsDeltaDeg(previousDeg, candidate) < MinAngleDeltaDeg)
        {
            candidate = previousDeg;
            return false;
        }

        return true;
    }

    public static Rotate ToRotate(float deg) =>
        new Rotate(new Angle(deg, AngleUnit.Degree));
}
