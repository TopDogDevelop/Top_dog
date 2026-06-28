using TopDog.Content.Map;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>战术战场内跳桥建筑单位（进入建筑指令目标）。</summary>
public static class JumpBridgeUnitService
{
    public const string UnitIdPrefix = "jbgate-";
    public const string TonnageClass = "JUMP_BRIDGE";

    public static bool IsJumpBridgeBuilding(BattlefieldUnit? u) =>
        u != null && u.isBuilding && u.bridgeId != null;

    public static void SyncForBattlefield(GameState state, BattlefieldState bf)
    {
        if (!state.combatRealtimeActive || bf.finished || bf.systemId == null)
        {
            RemoveAllJumpBridges(bf);
            return;
        }

        var sys = state.map?.Project?.FindSystem(bf.systemId);
        if (sys?.eventRegions == null)
        {
            RemoveAllJumpBridges(bf);
            return;
        }

        var bfAu = bf.anchorAu is { Length: >= 3 } ? bf.anchorAu : new[] { 0f, 0f, 0f };
        var alive = new HashSet<string>(StringComparer.Ordinal);
        foreach (var er in sys.eventRegions)
        {
            if (!EventRegionKinds.JumpBridge.Equals(er.kind, StringComparison.Ordinal)
                || er.bridgeId == null)
            {
                continue;
            }

            if (!IsCurrentJumpBridgeRegion(bf, er))
            {
                continue;
            }

            alive.Add(er.bridgeId);
            var unitId = UnitIdPrefix + er.bridgeId;
            var gateAu = er.anchorAu is { Length: >= 3 } ? er.anchorAu : new[] { 0f, 0f, 0f };
            var dxAu = gateAu[0] - bfAu[0];
            var dyAu = gateAu[1] - bfAu[1];
            var horizAu = MathF.Sqrt(dxAu * dxAu + dyAu * dyAu);
            float x;
            float y;
            if (horizAu > 1e-6f)
            {
                var sceneRadiusM = BattlefieldSceneProxyService.ResolveSceneBoundaryM(state, bf) * 0.35f;
                x = dxAu / horizAu * sceneRadiusM;
                y = dyAu / horizAu * sceneRadiusM;
            }
            else
            {
                x = 0f;
                y = 0f;
            }

            var unit = FindJumpBridge(bf, er.bridgeId);
            if (unit == null)
            {
                unit = new BattlefieldUnit
                {
                    unitId = unitId,
                    displayName = er.name ?? "跳桥",
                    tonnageClass = TonnageClass,
                    bridgeId = er.bridgeId,
                    isBuilding = true,
                    side = UnitSide.FRIENDLY,
                    alive = true,
                    arrivalAtSec = 0f,
                    structureMax = 1f,
                    structureHp = 1f,
                    salvoRoundDmg = 0f,
                    attackRangeM = 0f,
                    maxSpeedMps = 0f,
                };
                bf.units.Add(unit);
            }

            unit.displayName = er.name ?? "跳桥";
            unit.x = x;
            unit.y = y;
            unit.z = 0f;
            unit.alive = true;
            unit.bridgeId = er.bridgeId;
        }

        bf.units.RemoveAll(u =>
            u.bridgeId != null
            && TonnageClass.Equals(u.tonnageClass, StringComparison.Ordinal)
            && !alive.Contains(u.bridgeId));
    }

    private static bool IsCurrentJumpBridgeRegion(BattlefieldState bf, EventRegionDef er) =>
        (bf.eventRegionId != null
            && (bf.eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal)
                || bf.eventRegionId.Equals(er.name, StringComparison.Ordinal)))
        || (bf.subLocation != null
            && (bf.subLocation.Equals(er.eventRegionId, StringComparison.Ordinal)
                || bf.subLocation.Equals(er.name, StringComparison.Ordinal)));

    private static BattlefieldUnit? FindJumpBridge(BattlefieldState bf, string bridgeId)
    {
        foreach (var u in bf.units)
        {
            if (bridgeId.Equals(u.bridgeId, StringComparison.Ordinal)
                && u.isBuilding)
            {
                return u;
            }
        }

        return null;
    }

    private static void RemoveAllJumpBridges(BattlefieldState bf) =>
        bf.units.RemoveAll(u =>
            u.bridgeId != null
            && TonnageClass.Equals(u.tonnageClass, StringComparison.Ordinal));
}
