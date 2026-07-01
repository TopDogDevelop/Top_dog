using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>伴聊发言人伪随机：同现实人（identityCode）不连续接话；splitMsgId 组内允许多号拼一句。</summary>
public static class BanterSpeakerPicker
{
    public static MemberState? Pick(
        IReadOnlyList<MemberState> pool,
        string? lastSpeakerMemberId,
        string? lastSplitMsgId,
        string? currentSplitMsgId,
        Random rng,
        ICollection<string>? excludedIdentities = null)
    {
        if (pool.Count == 0)
        {
            return null;
        }

        var allowSameIdentity = AllowsSameIdentity(lastSplitMsgId, currentSplitMsgId);
        var lastIdentity = ResolveIdentity(pool, lastSpeakerMemberId);
        var candidates = new List<MemberState>(pool.Count);
        foreach (var m in pool)
        {
            if (excludedIdentities != null
                && excludedIdentities.Contains(IdentityCodes.Of(m)))
            {
                continue;
            }

            if (!allowSameIdentity
                && lastIdentity != null
                && string.Equals(IdentityCodes.Of(m), lastIdentity, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(lastSpeakerMemberId)
                && lastSpeakerMemberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                continue;
            }

            candidates.Add(m);
        }

        if (candidates.Count == 0)
        {
            foreach (var m in pool)
            {
                if (!lastSpeakerMemberId?.Equals(m.memberId, StringComparison.Ordinal) ?? true)
                {
                    candidates.Add(m);
                }
            }
        }

        if (candidates.Count == 0)
        {
            candidates.AddRange(pool);
        }

        return candidates[rng.Next(candidates.Count)];
    }

    private static bool AllowsSameIdentity(string? lastSplitMsgId, string? currentSplitMsgId) =>
        !string.IsNullOrWhiteSpace(currentSplitMsgId)
        && string.Equals(lastSplitMsgId, currentSplitMsgId, StringComparison.Ordinal);

    private static string? ResolveIdentity(IReadOnlyList<MemberState> pool, string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return null;
        }

        foreach (var m in pool)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return IdentityCodes.Of(m);
            }
        }

        return null;
    }
}
