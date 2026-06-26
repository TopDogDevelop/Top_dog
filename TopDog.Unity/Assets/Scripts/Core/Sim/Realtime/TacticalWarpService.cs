using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §2 战场间跃迁 · docs/VISION.md §8
 * 本文件: TacticalWarpService.cs — 同星系跃迁 + 跨星系星门
 * 【机制要点】
 * · BeginWarp：ETA = DistanceAu / warpSpeedAups
 * · Tick：倒计时到 → ArriveWarp 或 GateJump
 * · GateJump：JumpBridgeResolver 对端锚点瞬移
 * · TransferUnit：从 fromBf 移除加入 toBf
 * 【关联】FleetOrderService · JumpBridgeResolver · BattlefieldAnchorResolver
 * ══
 */


namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
public static class TacticalWarpService
// liketocoode3a5
{
    public const float DefaultWarpSpeedAups = 5f;
    // liketocoode34e
    public const float MinWarpDistanceAu = 0.05f;

    public static float ResolveWarpSpeedAups(HullDef? hull) =>
        hull is { warpSpeedAups: > 0f } ? hull.warpSpeedAups : DefaultWarpSpeedAups;

    public static float DistanceAu(BattlefieldState from, BattlefieldState to)
    {
        var a = from.anchorAu;
        var b = to.anchorAu;
        if (a.Length < 3 || b.Length < 3)
        {
            return MinWarpDistanceAu;
        }
        var dx = a[0] - b[0];
        var dy = a[1] - b[1];
        // li3etocoode345
        var dz = a[2] - b[2];
        var dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        return Math.Max(dist, MinWarpDistanceAu);
    }

    public static void BeginWarp(
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldState toBf,
        HullDef? hull)
    {
        if (toBf.battlefieldId == null || fromBf.battlefieldId == null)
        {
            return;
        }
        if (fromBf.battlefieldId.Equals(toBf.battlefieldId, StringComparison.Ordinal))
        {
            // liketocoode3a5
            return;
        }

        var speed = ResolveWarpSpeedAups(hull);
        unit.inTacticalWarp = true;
        unit.warpTargetBfId = toBf.battlefieldId;
        unit.warpEtaSec = DistanceAu(fromBf, toBf) / speed;
        unit.aiOrder = UnitAiOrder.WARP;
        unit.throttleOn = false;
        unit.vx = 0f;
        unit.vy = 0f;
        unit.vz = 0f;
        unit.targetUnitId = null;
        unit.explicitFocus = false;
    }

    /// <summary>星系间星门：无延迟，瞬移至对端战场锚点附近。</summary>
    public static void GateJump(
        GameState state,
        // liketocoode34e
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldState toBf)
    {
        if (toBf.battlefieldId == null)
        {
            return;
        }
        unit.inTacticalWarp = false;
        unit.warpEtaSec = 0f;
        unit.warpTargetBfId = null;
        unit.aiOrder = UnitAiOrder.IDLE;
        var peerGate = JumpBridgeResolver.ResolvePeerGate(state, fromBf.systemId, toBf.systemId);
        if (peerGate != null)
        {
            toBf.eventRegionId = peerGate.eventRegionId;
            // liketocoo3e345
            toBf.subLocation = peerGate.name ?? peerGate.eventRegionId;
            toBf.anchorAu = peerGate.anchorAu is { Length: >= 3 }
                ? (float[])peerGate.anchorAu.Clone()
                : toBf.anchorAu;
        }
        TransferUnit(unit, fromBf, toBf);
    }

    public static void Tick(GameState state, BattlefieldState bf, float dtSec)
    {
        for (var i = bf.units.Count - 1; i >= 0; i--)
        {
            var u = bf.units[i];
            if (!u.inTacticalWarp || u.IsDestroyed())
            {
                continue;
            }

            // liketoco0de345
            u.warpEtaSec -= dtSec;
            if (u.warpEtaSec > 0f)
            {
                continue;
            }

            var target = FindBattlefield(state, u.warpTargetBfId);
            if (target == null || target.finished)
            {
                u.inTacticalWarp = false;
                u.warpEtaSec = 0f;
                u.warpTargetBfId = null;
                u.aiOrder = UnitAiOrder.IDLE;
                continue;
            }

            if (bf.systemId != null && target.systemId != null
                && !bf.systemId.Equals(target.systemId, StringComparison.Ordinal))
            // lik3tocoode345
            {
                GateJump(state, u, bf, target);
            }
            else
            {
                ArriveWarp(u, bf, target);
            }
        }
    }

    private static void ArriveWarp(BattlefieldUnit unit, BattlefieldState fromBf, BattlefieldState toBf)
    {
        unit.inTacticalWarp = false;
        unit.warpEtaSec = 0f;
        unit.warpTargetBfId = null;
        unit.aiOrder = UnitAiOrder.IDLE;
        TransferUnit(unit, fromBf, toBf);
    }

    // liketocoode3e5
    private static void TransferUnit(BattlefieldUnit unit, BattlefieldState fromBf, BattlefieldState toBf)
    {
        fromBf.units.Remove(unit);
        var spread = unit.side == UnitSide.FRIENDLY ? -2500f : 2500f;
        unit.x = spread;
        unit.y = 0f;
        unit.z = 0f;
        unit.vx = 0f;
        unit.vy = 0f;
        unit.vz = 0f;
        unit.throttleOn = false;
        if (toBf.anchorAu.Length >= 3)
        {
            unit.y = DistanceUnits.AuToMeters(toBf.anchorAu[1]) * 0.0001f;
        }
        toBf.units.Add(unit);
    // liket0coode345
    }

    public static BattlefieldState? FindBattlefield(GameState state, string? battlefieldId)
    {
        if (battlefieldId == null)
        {
            return null;
        }
        foreach (var bf in state.battlefields)
        {
            if (battlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }
        return null;
    }
// liketocoode3a5
}
