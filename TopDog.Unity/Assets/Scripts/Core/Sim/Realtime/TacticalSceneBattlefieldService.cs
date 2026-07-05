using TopDog.Content.Map;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_RIGHT_RAIL_SCENE_PROXY.md §3 · docs/TACTICAL_WARP_AND_ORDERS.md §2
 * 本文件: TacticalSceneBattlefieldService.cs — 同星系场景战场懒加载
 * 【机制要点】
 * · EnsureSceneBattlefield：无则创建 BattlefieldState 并加入 state.battlefields
 * · 新战场创建后 SeedSceneProxies（一次性密封占位）
 * 【实现逻辑】
 * · FindSceneBattlefield：按 systemId + eventRegionId 匹配
 * · 不在此文件内 RemoveAll proxy 或重复 sync
 * 【关联】BattlefieldSceneProxyService · TacticalWarpService · FleetOrderService
 * ══
 */

namespace TopDog.Sim.Realtime;

/// <summary>懒加载同星系空场景战场：跃迁到达时才物化 BattlefieldState。</summary>
public static class TacticalSceneBattlefieldService
{
    public static BattlefieldState? FindSceneBattlefield(GameState state, string systemId, string eventRegionId)
    {
        BattlefieldState? finishedMatch = null;
        foreach (var bf in state.battlefields)
        {
            if (bf.systemId == null)
            {
                continue;
            }

            if (!systemId.Equals(bf.systemId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!MatchesRegion(bf, eventRegionId))
            {
                continue;
            }

            if (!bf.finished)
            {
                return bf;
            }

            finishedMatch ??= bf;
        }

        if (finishedMatch != null && state.combatRealtimeActive)
        {
            finishedMatch.finished = false;
            finishedMatch.winnerSide = null;
            finishedMatch.winReason = null;
            return finishedMatch;
        }

        return null;
    }

    public static EventRegionDef? FindEventRegion(GameState state, string systemId, string eventRegionId)
    {
        var sys = state.map?.Project?.FindSystem(systemId);
        if (sys?.eventRegions == null)
        {
            return null;
        }

        foreach (var er in sys.eventRegions)
        {
            if (eventRegionId.Equals(er.eventRegionId, StringComparison.Ordinal)
                || eventRegionId.Equals(er.name, StringComparison.Ordinal))
            {
                return er;
            }
        }

        return null;
    }

    public static BattlefieldState EnsureSceneBattlefield(GameState state, string systemId, string eventRegionId)
    {
        var existing = FindSceneBattlefield(state, systemId, eventRegionId);
        if (existing != null)
        {
            return existing;
        }

        var er = FindEventRegion(state, systemId, eventRegionId);
        var active = state.activeBattlefieldId != null
            ? TacticalWarpService.FindBattlefield(state, state.activeBattlefieldId)
            : null;

        var bf = new BattlefieldState
        {
            battlefieldId = "bf-" + Guid.NewGuid().ToString("N")[..8],
            combatEntryId = active?.combatEntryId,
            combatSubtype = active?.combatSubtype,
            systemId = systemId,
            eventRegionId = er?.eventRegionId ?? eventRegionId,
            subLocation = er?.name ?? eventRegionId,
            anchorAu = BattlefieldAnchorResolver.Resolve(state, systemId, eventRegionId),
            resolveMode = CombatResolveMode.REALTIME,
        };
        state.battlefields.Add(bf);
        BattlefieldSceneProxyService.SeedSceneProxies(state, bf);
        return bf;
    }

    private static bool MatchesRegion(BattlefieldState bf, string eventRegionId) =>
        eventRegionId.Equals(bf.eventRegionId, StringComparison.Ordinal)
        || eventRegionId.Equals(bf.subLocation, StringComparison.Ordinal);
}
