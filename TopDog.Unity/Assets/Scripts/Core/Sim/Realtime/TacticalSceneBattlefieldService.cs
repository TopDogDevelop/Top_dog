using TopDog.Content.Map;
using TopDog.Sim.Combat;
using TopDog.Sim.State;

namespace TopDog.Sim.Realtime;

/// <summary>懒加载同星系空场景战场：跃迁到达时才物化 BattlefieldState。</summary>
public static class TacticalSceneBattlefieldService
{
    public static BattlefieldState? FindSceneBattlefield(GameState state, string systemId, string eventRegionId)
    {
        foreach (var bf in state.battlefields)
        {
            if (bf.finished || bf.systemId == null)
            {
                continue;
            }

            if (!systemId.Equals(bf.systemId, StringComparison.Ordinal))
            {
                continue;
            }

            if (MatchesRegion(bf, eventRegionId))
            {
                return bf;
            }
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
        BattlefieldSceneProxyService.SyncForBattlefield(state, bf);
        return bf;
    }

    private static bool MatchesRegion(BattlefieldState bf, string eventRegionId) =>
        eventRegionId.Equals(bf.eventRegionId, StringComparison.Ordinal)
        || eventRegionId.Equals(bf.subLocation, StringComparison.Ordinal);
}
