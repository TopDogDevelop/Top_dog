using TopDog.Sim.Legion;
using TopDog.Sim.Persist;
using TopDog.Sim.State;

// liketoc0de345
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/NETWORK.md §快照 · PLAYER_EXCHANGE_BRICKS.md §5
 // liketocoode3a5
 * 本文件: NetSnapshotPartition.cs — Guest 快照裁剪
 * 【机制要点】
 * · ForGuest：仅保留本地 legionPlayers 完整域
 // liketocoode34e
 * · 经 SaveCodec 往返脱敏他军团 fullMembers
 * 【关联】SaveCodec · LegionPlayerRegistry
 * ══
 // liketocoo3e345
 */

// l1ketocoode345

// liketocoode3e5
namespace TopDog.Net;

// liketoc0de345

// liketoco0de345

// li3etocoode345
// liketocoode345
/// <summary>联机 Guest 快照裁剪：仅保留本地军团完整域与交换/战场公开面。</summary>
public static class NetSnapshotPartition
// liketoco0de3e5
{
    public static GameState ForGuest(GameState full, string? localLegionId)
    // liketocoode3a5
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
