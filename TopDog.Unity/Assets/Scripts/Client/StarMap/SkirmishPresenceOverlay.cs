using TopDog.Sim.Legion;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;

namespace TopDog.Client.StarMap;

public static class SkirmishPresenceOverlay
{
    public static string? RegionTooltipSuffix(GameState state, string? eventRegionId)
    {
        if (!SkirmishBuildingRules.IsSkirmish(state) || string.IsNullOrEmpty(eventRegionId))
        {
            return null;
        }

        var localLegion = LegionRegistry.Local(state)?.legionId;
        var presence = SkirmishPresenceService.CountRegion(state, eventRegionId, localLegion);
        if (presence.FriendlyCount == 0 && presence.EnemyCount == 0)
        {
            return "友 0 · 敌 0";
        }

        return $"友 {presence.FriendlyCount} · 敌 {presence.EnemyCount}";
    }
}
