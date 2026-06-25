using TopDog.Sim.Legion;
using TopDog.Sim.Persist;
using TopDog.Sim.State;

namespace TopDog.Net;

/// <summary>联机 Guest 快照裁剪：仅保留本地军团完整域与交换/战场公开面。</summary>
public static class NetSnapshotPartition
{
    public static GameState ForGuest(GameState full, string? localLegionId)
    {
        var snapshot = SaveCodec.FromJson(SaveCodec.ToJson(full));
        if (string.IsNullOrWhiteSpace(localLegionId))
        {
            snapshot.legionPlayers.Clear();
            snapshot.members.Clear();
            return snapshot;
        }

        LegionPlayerRegistry.EnsureFromLegions(snapshot);
        var local = snapshot.legionPlayers.GetValueOrDefault(localLegionId);
        snapshot.legionPlayers.Clear();
        if (local != null)
        {
            snapshot.legionPlayers[localLegionId] = local;
        }

        snapshot.members.Clear();
        if (local != null)
        {
            snapshot.members.AddRange(local.members);
        }

        return snapshot;
    }
}
