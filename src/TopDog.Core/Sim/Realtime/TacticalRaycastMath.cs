/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_NAVIGATION.md §3 平面 Raycast
 * 本文件: TacticalRaycastMath.cs — 过注视点、法线=相机前向的平面求交
 * ══
 */

namespace TopDog.Sim.Realtime;

public static class TacticalRaycastMath
{
    /// <summary>
    /// 射线与平面求交。平面过 <paramref name="planeOrigin"/>，法线为 <paramref name="planeNormal"/>（需归一化）。
    /// </summary>
    public static bool TryRayPlaneIntersect(
        float rayOx, float rayOy, float rayOz,
        float rayDx, float rayDy, float rayDz,
        float planeOx, float planeOy, float planeOz,
        float planeNx, float planeNy, float planeNz,
        out float hitX, out float hitY, out float hitZ)
    {
        hitX = hitY = hitZ = 0f;
        var denom = rayDx * planeNx + rayDy * planeNy + rayDz * planeNz;
        if (Math.Abs(denom) < 1e-6f)
        {
            return false;
        }

        var ox = planeOx - rayOx;
        var oy = planeOy - rayOy;
        var oz = planeOz - rayOz;
        var t = (ox * planeNx + oy * planeNy + oz * planeNz) / denom;
        if (t < 0f)
        {
            return false;
        }

        hitX = rayOx + rayDx * t;
        hitY = rayOy + rayDy * t;
        hitZ = rayOz + rayDz * t;
        return true;
    }

    /// <summary>
    /// 屏幕局部坐标 → 相对注视点的世界偏移（与 <see cref="TacticalViewportCamera.ProjectWorldOffset"/> 同一相机模型）。
    /// 平面过注视点，法线 = 相机前向。
    /// </summary>
    public static bool TryScreenToFocusPlaneOffset(
        float screenLocalX,
        float screenLocalY,
        float viewportWidth,
        float viewportHeight,
        float verticalFovDeg,
        float orbitYawRad,
        float orbitPitchRad,
        float viewDistanceM,
        out float worldDx,
        out float worldDy,
        out float worldDz)
    {
        worldDx = worldDy = worldDz = 0f;
        if (viewportWidth < 1f || viewportHeight < 1f || viewDistanceM < 1f)
        {
            return false;
        }

        var halfW = viewportWidth * 0.5f;
        var halfH = viewportHeight * 0.5f;
        var ndcX = (screenLocalX - halfW) / halfW;
        var ndcY = (halfH - screenLocalY) / halfH;
        var aspect = viewportWidth / MathF.Max(viewportHeight, 1f);
        var tanHalf = MathF.Tan(verticalFovDeg * MathF.PI / 180f * 0.5f);
        var dirVx = (float)(ndcX * tanHalf * aspect);
        var dirVy = (float)(ndcY * tanHalf);
        var dirVz = 1f;
        var len = MathF.Sqrt(dirVx * dirVx + dirVy * dirVy + dirVz * dirVz);
        if (len < 1e-6f)
        {
            return false;
        }

        dirVx /= len;
        dirVy /= len;
        dirVz /= len;
        if (dirVz < 1e-4f)
        {
            return false;
        }

        var t = viewDistanceM / dirVz;
        var hitVx = dirVx * t;
        var hitVy = dirVy * t;
        ViewOffsetToWorldOffset(hitVx, hitVy, 0f, orbitYawRad, orbitPitchRad, out worldDx, out worldDy, out worldDz);
        return true;
    }

    public static void ViewOffsetToWorldOffset(
        float vx,
        float vy,
        float vz,
        float orbitYawRad,
        float orbitPitchRad,
        out float dx,
        out float dy,
        out float dz)
    {
        var sinP = MathF.Sin(orbitPitchRad);
        var cosP = MathF.Cos(orbitPitchRad);
        var ry = vy * cosP + vz * sinP;
        var rz = -vy * sinP + vz * cosP;
        dy = ry;

        var sinY = MathF.Sin(orbitYawRad);
        var cosY = MathF.Cos(orbitYawRad);
        dx = vx * cosY + rz * sinY;
        dz = -vx * sinY + rz * cosY;
    }

    public static void WorldOffsetToViewOffset(
        float dx,
        float dy,
        float dz,
        float orbitYawRad,
        float orbitPitchRad,
        out float vx,
        out float vy,
        out float vz)
    {
        var cosY = MathF.Cos(orbitYawRad);
        var sinY = MathF.Sin(orbitYawRad);
        var rx = dx * cosY - dz * sinY;
        var rz = dx * sinY + dz * cosY;
        var ry = dy;
        var cosP = MathF.Cos(orbitPitchRad);
        var sinP = MathF.Sin(orbitPitchRad);
        vx = rx;
        vy = ry * cosP - rz * sinP;
        vz = ry * sinP + rz * cosP;
    }
}
