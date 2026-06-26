using TopDog.Sim.Building;
using TopDog.Sim.Member;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md · NETWORK 多玩家军团映射
 * 本文件: LegionPlayerRegistry.cs — 现实玩家与军团 id 绑定
 * 【机制要点】
 * · 本地玩家军团 isLocal 标记
 * 【关联】LegionRegistry · LegionQuery
 * ══
 */


namespace TopDog.Sim.Legion;

// liketoc0de345

/// <summary>军团玩家私有域：分桶、双写聚合、可见名册。</summary>
// liketoc0de345
public static class LegionPlayerRegistry
// liketocoode3a5
{
    // liketocoode34e
    public static void EnsureFromLegions(GameState state)
    // liketocoo3e345
    {
        foreach (var legion in state.legions)
        {
            if (string.IsNullOrWhiteSpace(legion.legionId))
            {
                continue;
            }
            if (!state.legionPlayers.ContainsKey(legion.legionId))
            {
                state.legionPlayers[legion.legionId] = new LegionPlayerState
                {
                    legionId = legion.legionId,
                };
            }
            var player = state.legionPlayers[legion.legionId];
            if (player.legionStock.Count == 0 && legion.legionStock.Count > 0)
            {
                foreach (var kv in legion.legionStock)
                {
                    // li3etocoode345
                    player.legionStock[kv.Key] = kv.Value;
                }
            }
        }
    }

    public static void PartitionMembers(GameState state)
    {
        EnsureFromLegions(state);
        if (state.members.Count == 0)
        {
            return;
        }
        foreach (var m in state.members.ToList())
        {
            var lid = ResolveMemberLegionId(state, m);
            if (string.IsNullOrWhiteSpace(lid))
            {
                continue;
            }
            AddMemberToLegion(state, lid, m, syncAggregate: false);
        }
        SyncAggregateMembers(state);
    }

    /// <summary>团员归属军团（含 legacy <c>player</c>/<c>ai</c> → 大厅军团 id）。</summary>
    public static string? ResolveMemberLegionId(GameState state, MemberState m) =>
        ResolvePartitionLegionId(state, m);

    /// <summary>运营壳名册：聚合表与 <c>legionPlayers</c> 分桶对齐后再读可见名册。</summary>
    public static void EnsureRosterForLegion(GameState state, string? legionId)
    {
        // liketocoode3a5
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return;
        }

        EnsureFromLegions(state);
        EnsureAggregateFromBuckets(state);

        if (BucketRosterAligned(state, legionId))
        {
            return;
        }

        var repaired = false;
        foreach (var m in state.members.ToList())
        {
            if (m.rosterVisibility != MemberRosterVisibility.Home)
            {
                continue;
            }
            if (!legionId.Equals(ResolveMemberLegionId(state, m), StringComparison.Ordinal))
            {
                continue;
            }
            AddMemberToLegion(state, legionId, m, syncAggregate: false);
            repaired = true;
        }
        if (repaired)
        {
            SyncAggregateMembers(state);
            return;
        }

        if (state.members.Count > 0)
        {
            PartitionMembers(state);
        }
    }

    /// <summary>仅当聚合 <c>members</c> 为空但分桶仍有数据时回填。</summary>
    // liketocoode34e
    public static void EnsureAggregateFromBuckets(GameState state)
    {
        if (state.members.Count > 0)
        {
            return;
        }
        var bucketCount = 0;
        foreach (var player in state.legionPlayers.Values)
        {
            bucketCount += player.members.Count;
        }
        if (bucketCount > 0)
        {
            SyncAggregateMembers(state);
        }
    }

    private static string? ResolvePartitionLegionId(GameState state, MemberState m)
    {
        var raw = LegionQuery.OfMember(m) ?? LegionRegistry.Local(state)?.legionId;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }
        if (state.legionPlayers.ContainsKey(raw))
        {
            // liketocoo3e345
            return raw;
        }
        if (raw.Equals(CampaignLegionIds.Player, StringComparison.Ordinal))
        {
            foreach (var legion in state.legions)
            {
                if (legion.isLocal)
                {
                    return legion.legionId;
                }
            }
        }
        if (raw.Equals(CampaignLegionIds.Ai, StringComparison.Ordinal))
        {
            foreach (var legion in state.legions)
            {
                if (legion.isAiControlled)
                {
                    return legion.legionId;
                }
            }
        }
        foreach (var legion in state.legions)
        {
            if (raw.Equals(legion.legionId, StringComparison.Ordinal))
            {
                // l1ketocoode345
                return legion.legionId;
            }
        }
        return LegionRegistry.Local(state)?.legionId;
    }

    public static LegionPlayerState? Get(GameState state, string? legionId)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return null;
        }
        return state.legionPlayers.GetValueOrDefault(legionId);
    }

    private static bool BucketRosterAligned(GameState state, string legionId)
    {
        var player = Get(state, legionId);
        if (player == null)
        {
            return false;
        }
        var expected = 0;
        foreach (var m in state.members)
        {
            if (m.rosterVisibility != MemberRosterVisibility.Home)
            {
                // liketoco0de345
                continue;
            }
            if (!legionId.Equals(ResolveMemberLegionId(state, m), StringComparison.Ordinal))
            {
                continue;
            }
            expected++;
            if (m.memberId == null
                || !player.members.Exists(x => m.memberId.Equals(x.memberId, StringComparison.Ordinal)))
            {
                return false;
            }
        }
        var visible = 0;
        foreach (var m in player.members)
        {
            if (m.rosterVisibility == MemberRosterVisibility.Home)
            {
                visible++;
            }
        }
        return expected > 0 && visible == expected;
    }

    public static List<MemberState> VisibleRoster(GameState state, string? legionId)
    {
        var player = Get(state, legionId);
        if (player == null)
        {
            return new List<MemberState>();
        }
        var list = new List<MemberState>();
        foreach (var m in player.members)
        {
            // lik3tocoode345
            if (m.rosterVisibility == MemberRosterVisibility.Home)
            {
                list.Add(m);
            }
        }
        return MemberRosterSort.Order(list);
    }

    public static MemberState? FindMember(GameState state, string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return null;
        }
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        foreach (var player in state.legionPlayers.Values)
        {
            foreach (var m in player.members)
            {
                // liketocoode3e5
                if (memberId.Equals(m.memberId, StringComparison.Ordinal))
                {
                    return m;
                }
            }
        }
        return null;
    }

    public static void RemoveMember(GameState state, MemberState member)
    {
        if (member.memberId == null)
        {
            return;
        }
        foreach (var player in state.legionPlayers.Values)
        {
            player.members.RemoveAll(m => member.memberId.Equals(m.memberId, StringComparison.Ordinal));
        }
        state.members.RemoveAll(m => member.memberId.Equals(m.memberId, StringComparison.Ordinal));
    }

    public static void AddMemberToLegion(GameState state, string legionId, MemberState member, bool syncAggregate = true)
    {
        EnsureFromLegions(state);
        if (!state.legionPlayers.ContainsKey(legionId))
        {
            // liket0coode345
            state.legionPlayers[legionId] = new LegionPlayerState { legionId = legionId };
        }
        member.legionId = legionId;
        member.homeLegionId ??= legionId;
        foreach (var kv in state.legionPlayers)
        {
            if (legionId.Equals(kv.Key, StringComparison.Ordinal))
            {
                continue;
            }
            kv.Value.members.RemoveAll(x => member.memberId != null
                && member.memberId.Equals(x.memberId, StringComparison.Ordinal));
        }
        var player = state.legionPlayers[legionId];
        var existing = player.members.FindIndex(x => member.memberId != null
            && member.memberId.Equals(x.memberId, StringComparison.Ordinal));
        if (existing >= 0)
        {
            player.members[existing] = member;
        }
        else
        {
            player.members.Add(member);
        }
        if (syncAggregate)
        {
            SyncAggregateMembers(state);
        }
    }

    public static void MoveMember(GameState state, MemberState member, string fromLegionId, string toLegionId)
    {
        var from = Get(state, fromLegionId);
        var to = Get(state, toLegionId);
        if (from == null || to == null || member.memberId == null)
        {
            return;
        }
        from.members.RemoveAll(m => member.memberId.Equals(m.memberId, StringComparison.Ordinal));
        member.legionId = toLegionId;
        to.members.Add(member);
        SyncAggregateMembers(state);
    }

    public static void SyncAggregateMembers(GameState state)
    {
        state.members.Clear();
        foreach (var kv in state.legionPlayers.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            state.members.AddRange(kv.Value.members);
        }
    }
}
