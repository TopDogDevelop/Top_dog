using TopDog.Sim.Realtime;
using TopDog.Sim.State;

namespace TopDog.Sim.Skirmish;

public readonly struct SkirmishRegionPresence
{
    public int FriendlyCount { get; }
    public int EnemyCount { get; }

    public SkirmishRegionPresence(int friendlyCount, int enemyCount)
    {
        FriendlyCount = friendlyCount;
        EnemyCount = enemyCount;
    }
}

public static class SkirmishPresenceService
{
    public static SkirmishRegionPresence CountRegion(GameState state, string? eventRegionId, string? localLegionId)
    {
        if (!SkirmishBuildingRules.IsSkirmish(state)
            || string.IsNullOrEmpty(eventRegionId)
            || string.IsNullOrEmpty(localLegionId))
        {
            return default;
        }

        var friendly = 0;
        var enemy = 0;
        foreach (var bf in state.battlefields)
        {
            if (!eventRegionId.Equals(bf.eventRegionId, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var u in bf.units)
            {
                if (u.IsDestroyed() || u.isBuilding || u.IsBallisticMissile())
                {
                    continue;
                }

                if (u.legionId != null && u.legionId.Equals(localLegionId, StringComparison.Ordinal))
                {
                    friendly++;
                }
                else if (u.side == UnitSide.ENEMY)
                {
                    enemy++;
                }
                else if (u.side == UnitSide.FRIENDLY)
                {
                    friendly++;
                }
            }
        }

        return new SkirmishRegionPresence(friendly, enemy);
    }
}
