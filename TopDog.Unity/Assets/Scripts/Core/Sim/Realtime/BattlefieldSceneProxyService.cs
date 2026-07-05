using TopDog.AgentDiag;
using TopDog.Content.Map;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_RIGHT_RAIL_SCENE_PROXY.md §3 · docs/TACTICAL_VIEW.md §4.7
 * 本文件: BattlefieldSceneProxyService.cs — 同星系场景外占位（边界投射 + 跃迁目标）
 * 【机制要点】
 * · SeedSceneProxies：加载时一次性写入 proxy，sceneProxiesSealed 后不再增删
 * · ListOffSceneLinks：从 map.eventRegions 导出静态链接（UI fallback）
 * · SyncForBattlefield：已密封 → no-op（仅跳桥）；未密封 → Seed
 * 【实现逻辑】
 * · BuildPlacements：当前 bf.eventRegionId 以外、IsIntraSystemWarpTarget 的区域 → AU 球面落点
 * · SameAuDirection：共线 AU 站点不做 MinAngleSepRad 散开（透视叠放）
 * · CreateProxy：确定性 unitId = TacticalSceneRoute.ProxyUnitId；写 azimuth/elevation
 * · 禁止 RemoveAllProxies 在 sealed 后或 tick/指令路径调用
 * 【关联】TacticalSceneRoute · SkirmishSpawnService · TacticalRightRail · TacticalViewportPresenter
 * ══
 */

namespace TopDog.Sim.Realtime;

/// <summary>
/// 实时战场边界场景占位：按同星系 map eventRegion AU 锚点相对方向，在本场景边界内生成可交互占位实体。
/// 不要求目标场景已有参战单位或已加载战场。
/// </summary>
public static class BattlefieldSceneProxyService
{
    public const string TonnageClass = "SCENE_PROXY";
    public const string UnitIdPrefix = "scene-proxy-";
    private const float MinDirectionAu = 1e-4f;
    private const float MinAngleSepRad = 0.18f;

    public static bool IsSceneProxy(BattlefieldUnit? u) =>
        u != null
        && (u.isSceneProxy
            || TonnageClass.Equals(u.tonnageClass, StringComparison.Ordinal));

    public static bool TryGetTargetScene(
        BattlefieldUnit? u,
        out string systemId,
        out string eventRegionId)
    {
        systemId = "";
        eventRegionId = "";
        if (!IsSceneProxy(u))
        {
            return false;
        }

        if (u!.sceneProxyTargetSystemId != null && u.sceneProxyTargetEventRegionId != null)
        {
            systemId = u.sceneProxyTargetSystemId;
            eventRegionId = u.sceneProxyTargetEventRegionId;
            return true;
        }

        return TacticalSceneRoute.TryParse(u.sceneProxyTargetBattlefieldId, out systemId, out eventRegionId);
    }

    [Obsolete("Use TryGetTargetScene + TacticalSceneBattlefieldService.EnsureSceneBattlefield")]
    public static string? ResolveTargetBattlefieldId(BattlefieldUnit? u)
    {
        if (!TryGetTargetScene(u, out var systemId, out var eventRegionId))
        {
            return null;
        }

        return TacticalSceneRoute.Key(systemId, eventRegionId);
    }

    public static bool TryGetProxyPosition(
        BattlefieldState bf,
        string targetSystemId,
        string targetEventRegionId,
        out float x,
        out float y,
        out float z)
    {
        var proxy = FindProxy(bf, targetSystemId, targetEventRegionId);
        if (proxy != null)
        {
            x = proxy.x;
            y = proxy.y;
            z = proxy.z;
            return true;
        }

        x = y = z = 0f;
        return false;
    }

    public static bool TryResolveProxyPosition(
        GameState state,
        BattlefieldState bf,
        string targetSystemId,
        string targetEventRegionId,
        out float x,
        out float y,
        out float z)
    {
        if (TryGetProxyPosition(bf, targetSystemId, targetEventRegionId, out x, out y, out z))
        {
            return true;
        }

        foreach (var p in BuildPlacements(state, bf))
        {
            if (targetSystemId.Equals(p.SystemId, StringComparison.Ordinal)
                && targetEventRegionId.Equals(p.EventRegionId, StringComparison.Ordinal))
            {
                x = p.X;
                y = p.Y;
                z = p.Z;
                return true;
            }
        }

        x = y = z = 0f;
        return false;
    }

    public static bool TryResolveProxyPositionForBattlefield(
        GameState state,
        BattlefieldState bf,
        BattlefieldState targetBf,
        out float x,
        out float y,
        out float z)
    {
        if (targetBf.systemId == null || targetBf.eventRegionId == null)
        {
            x = y = z = 0f;
            return false;
        }

        return TryResolveProxyPosition(state, bf, targetBf.systemId, targetBf.eventRegionId, out x, out y, out z);
    }

    public static float ResolveSceneBoundaryM(GameState state, BattlefieldState bf)
    {
        if (state.map?.Project?.systems != null
            && bf.systemId != null
            && bf.eventRegionId != null)
        {
            foreach (var sys in state.map.Project.systems)
            {
                if (!bf.systemId.Equals(sys.solarSystemId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (sys.eventRegions == null)
                {
                    break;
                }

                foreach (var er in sys.eventRegions)
                {
                    if (!bf.eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal)
                        && !bf.eventRegionId.Equals(er.name, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (er.radiusKm > 0)
                    {
                        return DistanceUnits.KmToMeters(er.radiusKm);
                    }
                }

                break;
            }
        }

        return BuildingCombatRules.AssaultStartDistanceM;
    }

    /// <summary>AU 方向向量 → 场景边界球面落点（水平角 + 垂直角，距中心 = radiusM）。</summary>
    internal static (float x, float y, float z, float azimuthRad, float elevationRad) SphericalPlacementFromAuDelta(
        float dxAu,
        float dyAu,
        float dzAu,
        float radiusM,
        string? fallbackId)
    {
        var distAu = MathF.Sqrt(dxAu * dxAu + dyAu * dyAu + dzAu * dzAu);
        float az;
        float el;
        if (distAu < MinDirectionAu)
        {
            var hash = StableHash(fallbackId);
            az = hash * MathF.PI * 2f;
            el = 0f;
        }
        else
        {
            az = MathF.Atan2(dyAu, dxAu);
            var horizAu = MathF.Sqrt(dxAu * dxAu + dyAu * dyAu);
            el = MathF.Atan2(dzAu, MathF.Max(horizAu, MinDirectionAu));
        }

        var (x, y, z) = SphericalToCartesian(radiusM, az, el);
        return (x, y, z, az, el);
    }

    internal static (float x, float y, float z) SphericalToCartesian(float radiusM, float azimuthRad, float elevationRad)
    {
        var cosEl = MathF.Cos(elevationRad);
        return (
            cosEl * MathF.Cos(azimuthRad) * radiusM,
            cosEl * MathF.Sin(azimuthRad) * radiusM,
            MathF.Sin(elevationRad) * radiusM);
    }

    public const float TacticalEdgeBaseFovDeg = 72f;

    /// <summary>
    /// 场景边界占位 → 战术屏边缘方向（单位向量经 orbit yaw/pitch 投影到 XY）。
    /// 与 <see cref="TacticalViewportCamera.WorldOffsetToViewSpace"/> 的 XY 分量一致（方向、无距离）。
    /// </summary>
    public static (float dirX, float dirY) ComputeScreenEdgeDirection(
        float azimuthRad,
        float elevationRad,
        float orbitYawRad,
        float orbitPitchRad)
    {
        var (vx, vy, _) = ComputeViewDirection(azimuthRad, elevationRad, orbitYawRad, orbitPitchRad);
        return (vx, vy);
    }

    /// <summary>
    /// 透视相机（近大远小）将场景相对角映射到视口像素；视锥内 onScreen=true，否则钳到边缘。
    /// </summary>
    public static (float left, float top, float centerX, float centerY, float dirX, float dirY, bool onScreen)
        ComputePerspectiveScreenPlacement(
            float azimuthRad,
            float elevationRad,
            float orbitYawRad,
            float orbitPitchRad,
            float verticalFovDeg,
            float viewportWidth,
            float viewportHeight,
            float edgePad,
            float markerHalf)
    {
        var (vx, vy, vz) = ComputeViewDirection(azimuthRad, elevationRad, orbitYawRad, orbitPitchRad);
        var halfW = viewportWidth * 0.5f;
        var halfH = viewportHeight * 0.5f;
        var maxPxX = halfW - edgePad - markerHalf;
        var maxPxY = halfH - edgePad - markerHalf;
        const float depthEps = 0.01f;

        if (vz > depthEps)
        {
            var aspect = viewportWidth / MathF.Max(viewportHeight, 1f);
            var tanHalf = MathF.Tan(verticalFovDeg * MathF.PI / 360f);
            var ndcX = vx / (vz * tanHalf * aspect);
            var ndcY = vy / (vz * tanHalf);
            if (MathF.Abs(ndcX) <= 1f && MathF.Abs(ndcY) <= 1f)
            {
                var cx = halfW + ndcX * halfW;
                var cy = halfH - ndcY * halfH;
                return (cx - markerHalf, cy - markerHalf, cx, cy, ndcX, ndcY, true);
            }

            var absX = MathF.Abs(ndcX);
            var absY = MathF.Abs(ndcY);
            var scale = absX < 1e-5f && absY < 1e-5f
                ? 1f
                : MathF.Min(1f / MathF.Max(absX, 1e-5f), 1f / MathF.Max(absY, 1e-5f));
            ndcX *= scale;
            ndcY *= scale;
            var edgeCx = halfW + ndcX * maxPxX;
            var edgeCy = halfH - ndcY * maxPxY;
            return (edgeCx - markerHalf, edgeCy - markerHalf, edgeCx, edgeCy, ndcX, ndcY, false);
        }

        if (vz < -depthEps)
        {
            var backX = -vx;
            var backY = -vy;
            var absBackX = MathF.Abs(backX);
            var absBackY = MathF.Abs(backY);
            float ndcBackX;
            float ndcBackY;
            if (absBackX < 1e-5f && absBackY < 1e-5f)
            {
                ndcBackX = 0f;
                ndcBackY = -1f;
            }
            else
            {
                var backScale = MathF.Min(
                    1f / MathF.Max(absBackX, 1e-5f),
                    1f / MathF.Max(absBackY, 1e-5f));
                ndcBackX = backX * backScale;
                ndcBackY = backY * backScale;
            }

            var behindCx = halfW + ndcBackX * maxPxX;
            var behindCy = halfH - ndcBackY * maxPxY;
            return (behindCx - markerHalf, behindCy - markerHalf, behindCx, behindCy, ndcBackX, ndcBackY, false);
        }

        // Top-down / grazing: direction-only orthographic edge fallback
        var orthoCx = halfW + vx * maxPxX;
        var orthoCy = halfH - vy * maxPxY;
        var orthoOnScreen = MathF.Abs(vx) <= 1f && MathF.Abs(vy) <= 1f;
        return (orthoCx - markerHalf, orthoCy - markerHalf, orthoCx, orthoCy, vx, vy, orthoOnScreen);
    }

    /// <summary>
    /// 透视相机（近大远小）将场景相对角映射到视口边缘像素，FOV 与背景相机一致。
    /// </summary>
    public static (float left, float top, float dirX, float dirY) ComputePerspectiveScreenEdge(
        float azimuthRad,
        float elevationRad,
        float orbitYawRad,
        float orbitPitchRad,
        float verticalFovDeg,
        float viewportWidth,
        float viewportHeight,
        float edgePad,
        float markerHalf)
    {
        var (left, top, _, _, dirX, dirY, _) = ComputePerspectiveScreenPlacement(
            azimuthRad,
            elevationRad,
            orbitYawRad,
            orbitPitchRad,
            verticalFovDeg,
            viewportWidth,
            viewportHeight,
            edgePad,
            markerHalf);
        return (left, top, dirX, dirY);
    }

    private static (float vx, float vy, float vz) ComputeViewDirection(
        float azimuthRad,
        float elevationRad,
        float orbitYawRad,
        float orbitPitchRad)
    {
        var cosEl = MathF.Cos(elevationRad);
        var wx = cosEl * MathF.Cos(azimuthRad);
        var wy = cosEl * MathF.Sin(azimuthRad);
        var wz = MathF.Sin(elevationRad);

        var cosY = MathF.Cos(orbitYawRad);
        var sinY = MathF.Sin(orbitYawRad);
        var rx = wx * cosY - wz * sinY;
        var rz = wx * sinY + wz * cosY;
        var cosP = MathF.Cos(orbitPitchRad);
        var sinP = MathF.Sin(orbitPitchRad);
        var vy = wy * cosP - rz * sinP;
        var vx = rx;
        var vz = wy * sinP + rz * cosP;
        return (vx, vy, vz);
    }

    /// <summary>加载时一次性写入场景外占位；密封后 SyncForBattlefield 不再 mutate。</summary>
    public static void SeedSceneProxies(GameState state, BattlefieldState bf)
    {
        if (bf.finished || bf.battlefieldId == null || bf.systemId == null)
        {
            return;
        }

        if (state.map?.Project == null)
        {
            return;
        }

        if (bf.sceneProxiesSealed && CountSceneProxies(bf) > 0)
        {
            JumpBridgeUnitService.SyncForBattlefield(state, bf);
            return;
        }

        EnsureBattlefieldAnchor(state, bf);
        var placements = BuildPlacements(state, bf);
        foreach (var p in placements)
        {
            var routeKey = TacticalSceneRoute.Key(p.SystemId, p.EventRegionId);
            var unitId = TacticalSceneRoute.ProxyUnitId(p.SystemId, p.EventRegionId);
            var unit = FindProxy(bf, p.SystemId, p.EventRegionId);
            if (unit == null)
            {
                bf.units.Add(CreateProxy(p, unitId, routeKey));
            }
            else
            {
                ApplyPlacement(unit, p, routeKey);
            }
        }

        if (placements.Count > 0 || !HasOffSceneTargets(state, bf))
        {
            bf.sceneProxiesSealed = true;
        }

        JumpBridgeUnitService.SyncForBattlefield(state, bf);
    }

    /// <summary>跃迁/指令前确保占位单位存在于 units（含 UI fallback 的确定性 unitId）。</summary>
    public static BattlefieldUnit? EnsureProxyUnit(
        GameState state,
        BattlefieldState bf,
        string targetSystemId,
        string targetEventRegionId)
    {
        if (bf.systemId == null
            || !bf.systemId.Equals(targetSystemId, StringComparison.Ordinal)
            || state.map?.Project == null)
        {
            return null;
        }

        var existing = FindProxy(bf, targetSystemId, targetEventRegionId);
        if (existing != null)
        {
            return existing;
        }

        EnsureBattlefieldAnchor(state, bf);
        foreach (var p in BuildPlacements(state, bf))
        {
            if (!targetSystemId.Equals(p.SystemId, StringComparison.Ordinal)
                || !targetEventRegionId.Equals(p.EventRegionId, StringComparison.Ordinal))
            {
                continue;
            }

            var routeKey = TacticalSceneRoute.Key(p.SystemId, p.EventRegionId);
            var unitId = TacticalSceneRoute.ProxyUnitId(p.SystemId, p.EventRegionId);
            var unit = CreateProxy(p, unitId, routeKey);
            bf.units.Add(unit);
            if (bf.sceneProxiesSealed)
            {
                return unit;
            }

            bf.sceneProxiesSealed = true;
            return unit;
        }

        return null;
    }

    public static bool TryParseProxyUnitId(string? unitId, out string systemId, out string eventRegionId) =>
        TryParseProxyUnitId(unitId, null, out systemId, out eventRegionId);

    public static bool TryParseProxyUnitId(
        string? unitId,
        string? hintSystemId,
        out string systemId,
        out string eventRegionId)
    {
        systemId = "";
        eventRegionId = "";
        if (unitId == null || !unitId.StartsWith(UnitIdPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var body = unitId[UnitIdPrefix.Length..];
        if (!string.IsNullOrEmpty(hintSystemId))
        {
            var prefix = hintSystemId + "-";
            if (body.StartsWith(prefix, StringComparison.Ordinal))
            {
                systemId = hintSystemId;
                eventRegionId = body[prefix.Length..];
                return eventRegionId.Length > 0;
            }
        }

        var dash = body.IndexOf('-');
        if (dash <= 0 || dash >= body.Length - 1)
        {
            return false;
        }

        systemId = body[..dash];
        eventRegionId = body[(dash + 1)..];
        return systemId.Length > 0 && eventRegionId.Length > 0;
    }

    private static int CountSceneProxies(BattlefieldState bf)
    {
        var count = 0;
        foreach (var u in bf.units)
        {
            if (IsSceneProxy(u))
            {
                count++;
            }
        }

        return count;
    }

    private static bool HasOffSceneTargets(GameState state, BattlefieldState bf)
    {
        var sys = state.map?.Project?.FindSystem(bf.systemId);
        if (sys?.eventRegions == null)
        {
            return false;
        }

        foreach (var er in sys.eventRegions)
        {
            if (er.eventRegionId == null || !EventRegionKinds.IsIntraSystemWarpTarget(er.kind))
            {
                continue;
            }

            if (!IsCurrentRegion(bf, er))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>地图拓扑导出的 off-scene 链接（UI fallback，不依赖 units 是否被误删）。</summary>
    public static List<TacticalOffSceneLink> ListOffSceneLinks(GameState state, BattlefieldState bf)
    {
        var list = new List<TacticalOffSceneLink>();
        if (bf.systemId == null || bf.eventRegionId == null || state.map?.Project == null)
        {
            return list;
        }

        EnsureBattlefieldAnchor(state, bf);
        foreach (var p in BuildPlacements(state, bf))
        {
            var unitId = TacticalSceneRoute.ProxyUnitId(p.SystemId, p.EventRegionId);
            list.Add(new TacticalOffSceneLink
            {
                UnitId = unitId,
                SystemId = p.SystemId,
                EventRegionId = p.EventRegionId,
                Kind = p.Kind,
                DisplayName = p.DisplayName,
                AzimuthRad = p.AzimuthRad,
                ElevationRad = p.ElevationRad,
                X = p.X,
                Y = p.Y,
                Z = p.Z,
            });
        }

        return list;
    }

    public static void SyncForBattlefield(GameState state, BattlefieldState bf)
    {
        if (bf.sceneProxiesSealed)
        {
            JumpBridgeUnitService.SyncForBattlefield(state, bf);
            return;
        }

        SeedSceneProxies(state, bf);
    }

    private static void EnsureBattlefieldAnchor(GameState state, BattlefieldState bf)
    {
        if (bf.systemId == null || bf.eventRegionId == null)
        {
            return;
        }

        if (bf.anchorAu is { Length: >= 3 })
        {
            return;
        }

        bf.anchorAu = BattlefieldAnchorResolver.Resolve(state, bf.systemId, bf.eventRegionId);
    }

    private struct ProxyPlacement
    {
        public string SystemId;
        public string EventRegionId;
        public string? Kind;
        public string DisplayName;
        public float AzimuthRad;
        public float ElevationRad;
        public float X;
        public float Y;
        public float Z;
    }

    private struct RawPlacement
    {
        public EventRegionDef Er;
        public float AzimuthRad;
        public float ElevationRad;
        public float X;
        public float Y;
        public float Z;
        public float DirX;
        public float DirY;
        public float DirZ;
    }

    private static List<ProxyPlacement> BuildPlacements(GameState state, BattlefieldState bf)
    {
        var radiusM = ResolveSceneBoundaryM(state, bf);
        var fromAu = bf.anchorAu is { Length: >= 3 } ? bf.anchorAu : new[] { 0f, 0f, 0f };
        var sys = state.map?.Project?.FindSystem(bf.systemId);
        if (sys?.eventRegions == null)
        {
            return new List<ProxyPlacement>();
        }

        var raw = new List<RawPlacement>();

        foreach (var er in sys.eventRegions)
        {
            if (er.eventRegionId == null || !EventRegionKinds.IsIntraSystemWarpTarget(er.kind))
            {
                continue;
            }

            if (IsCurrentRegion(bf, er))
            {
                continue;
            }

            var toAu = er.anchorAu is { Length: >= 3 } ? er.anchorAu : new[] { 0f, 0f, 0f };
            var dxAu = toAu[0] - fromAu[0];
            var dyAu = toAu[1] - fromAu[1];
            var dzAu = toAu[2] - fromAu[2];
            var (x, y, z, az, el) = SphericalPlacementFromAuDelta(dxAu, dyAu, dzAu, radiusM, er.eventRegionId);
            var distAu = MathF.Sqrt(dxAu * dxAu + dyAu * dyAu + dzAu * dzAu);
            float dirX = 0f;
            float dirY = 0f;
            float dirZ = 0f;
            if (distAu >= MinDirectionAu)
            {
                dirX = dxAu / distAu;
                dirY = dyAu / distAu;
                dirZ = dzAu / distAu;
            }

            raw.Add(new RawPlacement
            {
                Er = er,
                AzimuthRad = az,
                ElevationRad = el,
                X = x,
                Y = y,
                Z = z,
                DirX = dirX,
                DirY = dirY,
                DirZ = dirZ,
            });
        }

        raw.Sort((a, b) => a.AzimuthRad.CompareTo(b.AzimuthRad));
        var collinearStacked = 0;
        for (var i = 1; i < raw.Count; i++)
        {
            var prev = raw[i - 1];
            var cur = raw[i];
            if (cur.AzimuthRad - prev.AzimuthRad < MinAngleSepRad)
            {
                if (SameAuDirection(prev, cur))
                {
                    collinearStacked++;
                    continue;
                }

                var bumpedAz = cur.AzimuthRad + (MinAngleSepRad - (cur.AzimuthRad - prev.AzimuthRad));
                var (bx, by, bz) = SphericalToCartesian(radiusM, bumpedAz, cur.ElevationRad);
                raw[i] = new RawPlacement
                {
                    Er = cur.Er,
                    AzimuthRad = bumpedAz,
                    ElevationRad = cur.ElevationRad,
                    X = bx,
                    Y = by,
                    Z = bz,
                    DirX = cur.DirX,
                    DirY = cur.DirY,
                    DirZ = cur.DirZ,
                };
            }
        }

        if (collinearStacked > 0)
        {
            AgentSessionDebugLog.Write(
                "H1",
                "BattlefieldSceneProxyService.BuildPlacements",
                "collinear_stack",
                new { bfId = bf.battlefieldId, collinearStacked, total = raw.Count });
        }

        var list = new List<ProxyPlacement>(raw.Count);
        foreach (var item in raw)
        {
            list.Add(new ProxyPlacement
            {
                SystemId = bf.systemId!,
                EventRegionId = item.Er.eventRegionId!,
                Kind = item.Er.kind,
                DisplayName = FormatLabel(state, sys, item.Er),
                AzimuthRad = item.AzimuthRad,
                ElevationRad = item.ElevationRad,
                X = item.X,
                Y = item.Y,
                Z = item.Z,
            });
        }

        return list;
    }

    private static bool SameAuDirection(RawPlacement a, RawPlacement b)
    {
        var aLen = MathF.Sqrt(a.DirX * a.DirX + a.DirY * a.DirY + a.DirZ * a.DirZ);
        var bLen = MathF.Sqrt(b.DirX * b.DirX + b.DirY * b.DirY + b.DirZ * b.DirZ);
        if (aLen < 1e-5f || bLen < 1e-5f)
        {
            return MathF.Abs(a.AzimuthRad - b.AzimuthRad) < 1e-4f
                && MathF.Abs(a.ElevationRad - b.ElevationRad) < 1e-4f;
        }

        var dot = (a.DirX * b.DirX + a.DirY * b.DirY + a.DirZ * b.DirZ) / (aLen * bLen);
        return dot > 0.9999f;
    }

    private static bool IsCurrentRegion(BattlefieldState bf, EventRegionDef er)
    {
        if (bf.eventRegionId == null || er.eventRegionId == null)
        {
            return false;
        }

        return bf.eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal);
    }

    private static float StableHash(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return 0.37f;
        }

        unchecked
        {
            var h = 17;
            foreach (var c in id)
            {
                h = h * 31 + c;
            }

            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    private static string FormatLabel(GameState state, SolarSystemDef sys, EventRegionDef er)
    {
        if (SkirmishBuildingRules.IsSkirmish(state))
        {
            return SkirmishDisplayNames.FormatEventRegionPlace(state, sys.solarSystemId, er);
        }

        var systemName = sys.name ?? sys.solarSystemId ?? "?";
        var place = er.name ?? er.eventRegionId ?? "场景";
        return systemName + " · " + place;
    }

    private static BattlefieldUnit? FindProxy(
        BattlefieldState bf,
        string targetSystemId,
        string targetEventRegionId)
    {
        foreach (var u in bf.units)
        {
            if (IsSceneProxy(u)
                && TryGetTargetScene(u, out var sys, out var region)
                && targetSystemId.Equals(sys, StringComparison.Ordinal)
                && targetEventRegionId.Equals(region, StringComparison.Ordinal))
            {
                return u;
            }
        }

        return null;
    }

    private static BattlefieldUnit CreateProxy(ProxyPlacement p, string unitId, string routeKey) =>
        new()
        {
            unitId = unitId,
            displayName = p.DisplayName,
            tonnageClass = TonnageClass,
            isSceneProxy = true,
            sceneProxyTargetSystemId = p.SystemId,
            sceneProxyTargetEventRegionId = p.EventRegionId,
            sceneProxyTargetKind = p.Kind,
            sceneProxyTargetBattlefieldId = routeKey,
            side = UnitSide.FRIENDLY,
            alive = true,
            arrivalAtSec = 0f,
            pinnedToBattlefield = true,
            structureHp = 1f,
            structureMax = 1f,
            salvoRoundDmg = 0f,
            attackRangeM = 0f,
            maxSpeedMps = 0f,
            x = p.X,
            y = p.Y,
            z = p.Z,
            sceneProxyAzimuthRad = p.AzimuthRad,
            sceneProxyElevationRad = p.ElevationRad,
        };

    private static void ApplyPlacement(BattlefieldUnit unit, ProxyPlacement p, string routeKey)
    {
        unit.displayName = p.DisplayName;
        unit.x = p.X;
        unit.y = p.Y;
        unit.z = p.Z;
        unit.sceneProxyAzimuthRad = p.AzimuthRad;
        unit.sceneProxyElevationRad = p.ElevationRad;
        unit.alive = true;
        unit.isSceneProxy = true;
        unit.sceneProxyTargetSystemId = p.SystemId;
        unit.sceneProxyTargetEventRegionId = p.EventRegionId;
        unit.sceneProxyTargetKind = p.Kind;
        unit.sceneProxyTargetBattlefieldId = routeKey;
        unit.tonnageClass = TonnageClass;
        unit.pinnedToBattlefield = true;
        unit.salvoRoundDmg = 0f;
        unit.attackRangeM = 0f;
    }
}

/// <summary>同星系内、当前场景以外的可跃迁目标（静态拓扑）。</summary>
public sealed class TacticalOffSceneLink
{
    public string UnitId { get; init; } = "";
    public string SystemId { get; init; } = "";
    public string EventRegionId { get; init; } = "";
    public string? Kind { get; init; }
    public string DisplayName { get; init; } = "";
    public float AzimuthRad { get; init; }
    public float ElevationRad { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
}
