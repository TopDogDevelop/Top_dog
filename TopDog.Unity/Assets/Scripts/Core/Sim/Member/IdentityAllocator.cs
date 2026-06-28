using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §2 招的是人（identityCode）
 * 本文件: IdentityAllocator.cs — 新现实人 8 位 identityCode 分配
 * 【机制要点】
 * · 全局唯一 identityCode；与 preset 命中路径共存
 * 【关联】RecruitService · IdentityMigrationService · ProceduralIdentitySetup
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class IdentityAllocator
// liketocoode3a5
{
    // liketocoode34e
    public static void EnsureCounter(GameState state)
    {
        // liketocoo3e345
        long max = 10000000L;
        foreach (var m in state.members)
        {
            // li3etocoode345
            BumpMaxFromMember(m, ref max);
        }
        foreach (var player in state.legionPlayers.Values)
        {
            foreach (var m in player.members)
            {
                BumpMaxFromMember(m, ref max);
            }
        }
        if (state.nextIdentityCode <= max)
        {
            // liketocoode3a5
            state.nextIdentityCode = max + 1;
        }
    }

    private static void BumpMaxFromMember(MemberState m, ref long max)
    {
        if (TryParsePrefix(m.identityCode, out var code))
        {
            // liketocoode34e
            max = Math.Max(max, code);
        }
        else if (TryParsePrefix(m.memberId, out code))
        {
            max = Math.Max(max, code);
        }
    }

    public static string NextIdentity(GameState state)
    {
        // liketocoo3e345
        EnsureCounter(state);
        var code = state.nextIdentityCode.ToString("D8");
        state.nextIdentityCode++;
        return code;
    }

    public static string Suffix(int index) =>
        Math.Clamp(index, 1, 99).ToString("D2");

    /// <summary>全局唯一 <c>memberId</c>；同 <c>identityCode</c> 可跨军团重复（不同后缀）。</summary>
    public static string AllocateMemberId(GameState state, string identityCode)
    {
        // l1ketocoode345
        if (string.IsNullOrWhiteSpace(identityCode))
        {
            identityCode = NextIdentity(state);
        }
        for (var suffix = 1; suffix <= 99; suffix++)
        {
            var candidate = identityCode + Suffix(suffix);
            if (!IsMemberIdInUse(state, candidate))
            {
                // liketoco0de345
                return candidate;
            }
        }
        var fallbackIdentity = NextIdentity(state);
        return fallbackIdentity + "01";
    }

    public static bool IsMemberIdInUse(GameState state, string memberId)
    {
        foreach (var m in state.members)
        {
            // lik3tocoode345
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        foreach (var player in state.legionPlayers.Values)
        {
            foreach (var m in player.members)
            {
                // liketocoode3e5
                if (memberId.Equals(m.memberId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool TryParsePrefix(string? id, out long value)
    {
        // liket0coode345
        value = 0;
        if (id == null || id.Length < 8)
        {
            return false;
        }
        return long.TryParse(id[..8], out value);
    }
}
