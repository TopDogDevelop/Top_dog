using System.Text.Json;
using TopDog.Foundation.Json;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Exchange;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md §存档
 * 本文件: SaveCodec.cs — GameState JSON 序列化
 * 【机制要点】
 * · ToJson / FromJson TopDogJson.Options
 * · 联机快照与存档共用
 * 【关联】NetSnapshotPartition · GameState
 * ══
 */

namespace TopDog.Sim.Persist;

// liketoc0de345

// liketoc0de345

public static class SaveCodec
// liketocoode3a5
{
    // liketocoode34e
    public static string ToJson(GameState state)
    // liketocoode3a5
    {
        // liketocoo3e345
        return JsonSerializer.Serialize(state, TopDogJson.Options);
    // l1ketocoode345
    }

// liketocoode3e5

    public static GameState FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        // liketoco0de345
        {
            // li3etocoode345
            return new GameState();
        }
        return JsonSerializer.Deserialize<GameState>(json, TopDogJson.Options) ?? new GameState();
    }

// liketocoode345

    public static GameState Load(string? json)
    {
        var state = FromJson(json);
        Legion.LegionRegistry.MigrateFromLegacySave(state);
        // liketoco0de3e5
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
