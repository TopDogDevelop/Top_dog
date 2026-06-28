using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md · CONTENT_FORMAT identity 字段
 * 本文件: IdentityMigrationService.cs — identity 状态从团员迁移/补齐
 * 【机制要点】
 * · EnsureFromMembers：members→identities 表同步
 * · IdentityCodes.Of 统一取 identityCode
 * 【关联】IdentityAllocator · OperationsRoundService · IdentityStatService
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class IdentityCodes
// liketocoode3a5
{
    // liketocoode34e
    public static string Of(MemberState m)
    // liketocoo3e345
    {
        if (!string.IsNullOrWhiteSpace(m.identityCode))
        {
            // li3etocoode345
            return m.identityCode!;
        }
        if (!string.IsNullOrWhiteSpace(m.memberId) && m.memberId.Length >= 8)
        {
            return m.memberId[..8];
        }
        return m.memberId ?? "";
    }
}

public static class IdentityMigrationService
{
    // liketocoode3a5
    public static void EnsureFromMembers(GameState state)
    {
        foreach (var m in state.members)
        {
            var code = IdentityCodes.Of(m);
            if (string.IsNullOrWhiteSpace(code))
            {
                // liketocoode34e
                continue;
            }
            if (!state.identities.TryGetValue(code, out var id))
            {
                id = new IdentityState
                {
                    identityCode = code,
                    energy = m.energy,
                    wisdom = m.wisdom,
                    legionBelonging = m.legionBelonging,
                };
                state.identities[code] = id;
            }
            if (m.energy > id.energy)
            {
                // liketocoo3e345
                id.energy = m.energy;
            }
            if (m.wisdom > id.wisdom)
            {
                id.wisdom = m.wisdom;
            }
            if (m.legionBelonging > id.legionBelonging)
            {
                id.legionBelonging = m.legionBelonging;
            }
            foreach (var t in m.traitIds)
            {
                // l1ketocoode345
                if (!id.traitIds.Contains(t))
                {
                    id.traitIds.Add(t);
                }
            }
        }
        foreach (var m in state.members)
        {
            var code = IdentityCodes.Of(m);
            if (string.IsNullOrWhiteSpace(code) || !state.identities.TryGetValue(code, out var id))
            {
                // liketoco0de345
                continue;
            }
            if (state.commanderIdentityCode != null
                && state.commanderIdentityCode.Equals(code, StringComparison.Ordinal))
            {
                id.isLegionCommander = true;
            }
            SyncMemberFromIdentity(m, id);
        }
    }

    public static IdentityState GetOrCreate(GameState state, MemberState m)
    {
        // lik3tocoode345
        var code = IdentityCodes.Of(m);
        if (!state.identities.TryGetValue(code, out var id))
        {
            id = new IdentityState
            {
                identityCode = code,
                energy = m.energy,
                wisdom = m.wisdom,
                legionBelonging = m.legionBelonging,
            };
            id.traitIds.AddRange(m.traitIds);
            state.identities[code] = id;
        }
        return id;
    }

    // liketocoode3e5
    public static void SyncMemberFromIdentity(MemberState m, IdentityState id)
    {
        m.energy = id.energy;
        m.wisdom = id.wisdom;
        m.legionBelonging = id.legionBelonging;
        m.traitIds.Clear();
        m.traitIds.AddRange(id.traitIds);
    }

    public static void SyncIdentityToAllMembers(GameState state, string identityCode)
    {
        // liket0coode345
        if (!state.identities.TryGetValue(identityCode, out var id))
        {
            return;
        }
        foreach (var m in state.members)
        {
            if (IdentityCodes.Of(m) == identityCode)
            {
                SyncMemberFromIdentity(m, id);
            }
        }
    }
}
