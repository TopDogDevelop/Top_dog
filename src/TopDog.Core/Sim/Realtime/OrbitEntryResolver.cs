namespace TopDog.Sim.Realtime;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §1.3 ORBIT
 * 本文件: OrbitEntryResolver — 环绕切入点与默认半径
 * 【机制要点】舰首 vs 舰–目标连线几何选 entry；Seek → OrbitOnRing 两阶段
 * 【关联】BattlefieldSystem.TickOrbit · FleetOrderService.OrderOrbit
 * ══
 */

/// <summary>环绕切入点：由舰首与舰–目标连线几何确定，非随机 ±90°。</summary>
public static class OrbitEntryResolver
{
    public const float DefaultOrbitRadiusM = 5000f;
    public const float EntryArriveThresholdM = 800f;
    public const float OrbitPhaseSeek = 0f;
    public const float OrbitPhaseRing = 1f;

    public static float ResolveOrbitRadiusM(BattlefieldUnit u) =>
        u.orbitRadiusM >= 100f ? u.orbitRadiusM : DefaultOrbitRadiusM;

    public static void ComputeEntryPoint(
        BattlefieldUnit ship,
        BattlefieldUnit target,
        float radiusM,
        out float entryX,
        out float entryY,
        out float entryZ)
    {
        var stX = target.x - ship.x;
        var stY = target.y - ship.y;
        var stZ = target.z - ship.z;
        var stLen = MathF.Sqrt(stX * stX + stY * stY + stZ * stZ);
        if (stLen < 1e-3f)
        {
            stX = 1f;
            stY = 0f;
            stZ = 0f;
            stLen = 1f;
        }

        var invSt = 1f / stLen;
        var perpAX = -stY * invSt;
        var perpAY = stX * invSt;
        var perpAZ = 0f;
        var perpBX = stY * invSt;
        var perpBY = -stX * invSt;
        var perpBZ = 0f;

        var (bowX, bowY, _) = ShipMotionIntegrator.HeadingToUnitVector(ship.facingRad, ship.pitchRad);
        var dotA = bowX * perpAX + bowY * perpAY;
        var dotB = bowX * perpBX + bowY * perpBY;

        var sideX = dotA >= dotB ? perpAX : perpBX;
        var sideY = dotA >= dotB ? perpAY : perpBY;
        var sideZ = dotA >= dotB ? perpAZ : perpBZ;
        var sideLen = MathF.Sqrt(sideX * sideX + sideY * sideY + sideZ * sideZ);
        if (sideLen < 1e-3f)
        {
            sideX = perpAX;
            sideY = perpAY;
            sideZ = 0f;
            sideLen = 1f;
        }

        var scale = radiusM / sideLen;
        entryX = target.x + sideX * scale;
        entryY = target.y + sideY * scale;
        entryZ = target.z + sideZ * scale;
    }
}
