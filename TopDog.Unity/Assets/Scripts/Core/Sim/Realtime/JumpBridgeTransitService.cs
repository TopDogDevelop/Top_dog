using TopDog.Content.Map;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_WARP_AND_ORDERS.md §2.2 进入建筑
 * 本文件: JumpBridgeTransitService — 跨星系跳桥无延迟瞬移
 * 【机制要点】OrderEnterBuilding → ResolvePeerGate → 对端 anchor 战术坐标；拒绝 closed 桥
 * 【关联】JumpBridgeUnitService · JumpBridgeResolver · TacticalSceneBattlefieldService
 * ══
 */

/// <summary>跨星系跳桥：进入建筑 → 无延迟出现在对端配对跳桥场景。</summary>
public static class JumpBridgeTransitService
{
    public static bool TryTransit(
        GameState state,
        BattlefieldUnit unit,
        BattlefieldState fromBf,
        BattlefieldUnit gateUnit,
        out string? error)
    {
        error = null;
        if (!JumpBridgeUnitService.IsJumpBridgeBuilding(gateUnit) || gateUnit.bridgeId == null)
        {
            error = "目标不是跳桥";
            return false;
        }

        if (fromBf.systemId == null)
        {
            error = "当前场景无效";
            return false;
        }

        var bridge = ResolveBridgeById(state, gateUnit.bridgeId);
        if (bridge == null)
        {
            error = "跳桥数据无效";
            return false;
        }

        var toSystemId = fromBf.systemId.Equals(bridge.fromSystemId, StringComparison.Ordinal)
            ? bridge.toSystemId
            : bridge.fromSystemId;
        if (toSystemId == null)
        {
            error = "目标星系无效";
            return false;
        }

        var peer = JumpBridgeResolver.ResolvePeerGate(state, fromBf.systemId, toSystemId);
        if (peer?.eventRegionId == null)
        {
            peer = FindStarRegion(state, toSystemId);
        }

        if (peer?.eventRegionId == null)
        {
            error = "对端跳桥无效";
            return false;
        }

        var toBf = TacticalSceneBattlefieldService.EnsureSceneBattlefield(
            state,
            toSystemId,
            peer.eventRegionId);

        CancelWarp(state, unit, fromBf);
        fromBf.units.Remove(unit);
        toBf.units.Add(unit);

        unit.x = 0f;
        unit.y = 0f;
        unit.z = 0f;
        unit.vx = 0f;
        unit.vy = 0f;
        unit.vz = 0f;
        unit.aiOrder = UnitAiOrder.STOP;
        unit.approachTargetUnitId = null;
        unit.orbitTargetUnitId = null;
        unit.throttleOn = false;
        unit.inTacticalWarp = false;
        unit.warpPhase = TacticalWarpPhase.None;

        if (unit.memberId != null)
        {
            foreach (var m in state.members)
            {
                if (unit.memberId.Equals(m.memberId, StringComparison.Ordinal))
                {
                    m.currentSolarSystemId = toSystemId;
                    break;
                }
            }
        }

        BattlefieldSceneProxyService.SyncForBattlefield(state, fromBf);
        BattlefieldSceneProxyService.SyncForBattlefield(state, toBf);
        JumpBridgeUnitService.SyncForBattlefield(state, fromBf);
        JumpBridgeUnitService.SyncForBattlefield(state, toBf);
        return true;
    }

    public static JumpBridgeDef? ResolveBridgeById(GameState state, string bridgeId)
    {
        if (state.map?.Project?.bridges == null)
        {
            return null;
        }

        foreach (var b in state.map.Project.bridges)
        {
            if (bridgeId.Equals(b.bridgeId, StringComparison.Ordinal))
            {
                return b;
            }
        }

        return null;
    }

    private static EventRegionDef? FindStarRegion(GameState state, string systemId)
    {
        var sys = state.map?.Project?.FindSystem(systemId);
        if (sys?.eventRegions == null)
        {
            return null;
        }

        foreach (var er in sys.eventRegions)
        {
            if (EventRegionKinds.Star.Equals(er.kind, StringComparison.Ordinal))
            {
                return er;
            }
        }

        return sys.eventRegions.Count > 0 ? sys.eventRegions[0] : null;
    }

    private static void CancelWarp(GameState state, BattlefieldUnit unit, BattlefieldState bf)
    {
        if (!unit.inTacticalWarp)
        {
            return;
        }

        unit.inTacticalWarp = false;
        unit.warpPhase = TacticalWarpPhase.None;
        unit.warpEtaSec = 0f;
        state.tacticalWarpInTransit.RemoveAll(t =>
            t.unit != null
            && unit.unitId != null
            && unit.unitId.Equals(t.unit.unitId, StringComparison.Ordinal));
        if (!bf.units.Contains(unit))
        {
            bf.units.Add(unit);
        }
    }
}
