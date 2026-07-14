using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VISUAL_ASSETS.md §5.1 · docs/TACTICAL_VIEW.md §4 方向指示
 * 本文件: ShipHeadingResolver.cs — 舰标朝向（纯屏幕两点差）
 * 【机制要点】
 * · 尾部指向上一样本屏幕位置；尖端沿屏幕位移
 * · PNG 船头朝上 → +ArtNoseUpOffsetDeg(90)
 * · MinAngleDeltaDeg=5：角变化不足则舍弃，目的是减少无效旋转更新（非治卡顿主力）
 * · 不读 facingRad / 相机 yaw；orbit 期间乱转为可接受缺陷（见手册）
 * · 每帧 Presenter 刷新采样（避免极低 tick 放大误差）
 * 【关联】TacticalViewportPresenter
 * ══
 */

namespace TopDog.Client.Tactical;

/// <summary>战术舰标朝向：纯屏幕航迹（上一样本 → 当前样本）。</summary>
public static class ShipHeadingResolver
{
    /// <summary>源图船头朝上时，把「屏幕向右」映到 UITK 顺时针角的偏移（度）。</summary>
    public const float ArtNoseUpOffsetDeg = 90f;

    /// <summary>屏幕位移低于此像素则保持上帧角（防停船抖动）。</summary>
    public const float MinScreenDeltaPx = 0.75f;

    /// <summary>
    /// 相对上帧角，最短弧变化低于此则舍弃本次旋转写入。
    /// 目的：减少无效旋转更新（少脏 UITK style），不是战斗卡顿的治本手段。
    /// </summary>
    public const float MinAngleDeltaDeg = 5f;

    /// <summary>
    /// 由屏幕位移求 UITK 旋转角。坐标系：+X 右、+Y 下（与 marker Center 一致）。
    /// 尾部指向上一样本 ⇔ 尖端沿 (dx,dy)。
    /// </summary>
    public static float ScreenTrailDeg(float dx, float dy) =>
        Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + ArtNoseUpOffsetDeg;

    /// <summary>最短角差绝对值（度），结果 ∈ [0, 180]。</summary>
    public static float AbsDeltaDeg(float fromDeg, float toDeg)
    {
        var d = Mathf.DeltaAngle(fromDeg, toDeg);
        return Mathf.Abs(d);
    }

    /// <returns>true = 应采用新角并写 style.rotate；false = 保持 previousDeg（位移过小或角变 &lt; 5°）。</returns>
    public static bool TryResolveTrailDeg(
        float prevCenterX,
        float prevCenterY,
        float centerX,
        float centerY,
        float previousDeg,
        out float deg)
    {
        var dx = centerX - prevCenterX;
        var dy = centerY - prevCenterY;
        if (dx * dx + dy * dy < MinScreenDeltaPx * MinScreenDeltaPx)
        {
            deg = previousDeg;
            return false;
        }

        var candidate = ScreenTrailDeg(dx, dy);
        if (AbsDeltaDeg(previousDeg, candidate) < MinAngleDeltaDeg)
        {
            deg = previousDeg;
            return false;
        }

        deg = candidate;
        return true;
    }

    public static Rotate ToRotate(float deg) =>
        new Rotate(new Angle(deg, AngleUnit.Degree));
}
