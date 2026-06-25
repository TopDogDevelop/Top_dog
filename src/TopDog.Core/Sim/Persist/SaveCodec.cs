using System.Text.Json;
using TopDog.Foundation.Json;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Exchange;
using TopDog.Sim.State;

namespace TopDog.Sim.Persist;

public static class SaveCodec
{
    public static string ToJson(GameState state)
    {
        return JsonSerializer.Serialize(state, TopDogJson.Options);
    }

    public static GameState FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new GameState();
        }
        return JsonSerializer.Deserialize<GameState>(json, TopDogJson.Options) ?? new GameState();
    }

    public static GameState Load(string? json)
    {
        var state = FromJson(json);
        Legion.LegionRegistry.MigrateFromLegacySave(state);
        MigrateAssaultQueue(state);
        if (state.schemaVersion >= 4)
        {
            Legion.LegionPlayerRegistry.PartitionMembers(state);
        }
        state.exchange.infiltrationByIdentity ??= new Dictionary<string, InfiltrationRecord>(StringComparer.Ordinal);
        return state;
    }

    private static void MigrateAssaultQueue(GameState state)
    {
        if (state.aiPendingAssaults.Count > 0 || state.aiPendingAssaultBuildingIds.Count == 0)
        {
            return;
        }
        foreach (var buildingId in state.aiPendingAssaultBuildingIds)
        {
            state.aiPendingAssaults.Add(new AiPendingAssaultOp
            {
                attackerLegionId = CampaignLegionIds.Ai,
                buildingId = buildingId,
            });
        }
        state.aiPendingAssaultBuildingIds.Clear();
    }
}
