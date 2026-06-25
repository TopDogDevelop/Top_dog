using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class IdentityCodes
{
    public static string Of(MemberState m)
    {
        if (!string.IsNullOrWhiteSpace(m.identityCode))
        {
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
    public static void EnsureFromMembers(GameState state)
    {
        foreach (var m in state.members)
        {
            var code = IdentityCodes.Of(m);
            if (string.IsNullOrWhiteSpace(code))
            {
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
