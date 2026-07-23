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
        MigrateShipInstances(state);
        // liketoco0de3e5
        MigrateAssaultQueue(state);
        if (state.schemaVersion >= 4)
        {
            Legion.LegionPlayerRegistry.PartitionMembers(state);
        }
        state.exchange.infiltrationByIdentity ??= new Dictionary<string, InfiltrationRecord>(StringComparer.Ordinal);
        return state;
    }

    private static void MigrateShipInstances(GameState state)
    {
        state.shipInstances ??= new List<ShipInstanceState>();
        foreach (var member in state.members
                     .Where(member => !string.IsNullOrWhiteSpace(member.memberId)
                                      && !string.IsNullOrWhiteSpace(member.equippedHullId)))
        {
            var memberId = member.memberId!;
            var hullId = member.equippedHullId!;
            var existing = !string.IsNullOrWhiteSpace(member.equippedShipInstanceId)
                ? state.shipInstances.FirstOrDefault(ship =>
                    member.equippedShipInstanceId.Equals(ship.shipInstanceId, StringComparison.Ordinal))
                : state.shipInstances.FirstOrDefault(ship =>
                    memberId.Equals(ship.ownerMemberId, StringComparison.Ordinal)
                    && hullId.Equals(ship.hullId, StringComparison.Ordinal));
            if (existing == null)
            {
                var shipInstanceId = $"ship_{memberId}";
                var suffix = 1;
                while (state.shipInstances.Any(ship =>
                           shipInstanceId.Equals(ship.shipInstanceId, StringComparison.Ordinal)))
                {
                    shipInstanceId = $"ship_{memberId}_{suffix++}";
                }
                state.memberFittedModules.TryGetValue(memberId, out var fitted);
                existing = new ShipInstanceState
                {
                    shipInstanceId = shipInstanceId,
                    hullId = hullId,
                    ownerMemberId = memberId,
                    fittedModules = fitted != null
                        ? new Dictionary<string, string>(fitted, StringComparer.Ordinal)
                        : new Dictionary<string, string>(StringComparer.Ordinal),
                };
                state.shipInstances.Add(existing);
            }
            member.equippedShipInstanceId = existing.shipInstanceId;
        }
        state.schemaVersion = Math.Max(state.schemaVersion, 6);
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
