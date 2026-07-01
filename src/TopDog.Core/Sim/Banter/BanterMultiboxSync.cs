using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>模拟同步器：同现实人名下多号同一时刻复读同一句。</summary>
public static class BanterMultiboxSync
{
    public const int ChancePercent = 10;

    public static bool Roll(Random rng) => rng.Next(100) < ChancePercent;

    /// <summary>收集主发言人同 identity 且在池内的全部账号；不足 2 个则失败。</summary>
    public static bool TryCollectSyncBurst(
        IReadOnlyList<MemberState> eligiblePool,
        string primaryMemberId,
        out List<string> memberIds)
    {
        memberIds = new List<string>();
        var identity = ResolveIdentity(eligiblePool, primaryMemberId);
        if (identity == null)
        {
            memberIds.Add(primaryMemberId);
            return false;
        }

        foreach (var m in eligiblePool)
        {
            if (string.IsNullOrWhiteSpace(m.memberId))
            {
                continue;
            }

            if (identity.Equals(IdentityCodes.Of(m), StringComparison.Ordinal))
            {
                memberIds.Add(m.memberId);
            }
        }

        memberIds.Sort(StringComparer.Ordinal);
        return memberIds.Count >= 2;
    }

    private static string? ResolveIdentity(IReadOnlyList<MemberState> pool, string memberId)
    {
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
