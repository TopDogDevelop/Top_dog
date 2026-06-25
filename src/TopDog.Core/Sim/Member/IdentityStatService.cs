using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class IdentityStatService
{
    public static IdentityState Identity(GameState state, MemberState m) =>
        IdentityMigrationService.GetOrCreate(state, m);

    public static bool TrySpendEnergy(GameState state, MemberState m, int amount)
    {
        var id = Identity(state, m);
        while (id.energy < amount)
        {
            if (LegionCommanderService.IsCommanderIdentity(state, id.identityCode))
            {
                id.energy += 10;
            }
            else if (id.legionBelonging < 1)
            {
                return false;
            }
            else
            {
                id.legionBelonging--;
                id.energy += 10;
            }
            IdentityMigrationService.SyncIdentityToAllMembers(state, id.identityCode!);
        }
        id.energy -= amount;
        IdentityMigrationService.SyncIdentityToAllMembers(state, id.identityCode!);
        return true;
    }

    public static bool TrySpendBelonging(GameState state, MemberState m, int amount)
    {
        var id = Identity(state, m);
        if (LegionCommanderService.IsCommanderIdentity(state, id.identityCode))
        {
            return true;
        }
        if (id.legionBelonging < amount)
        {
            return false;
        }
        id.legionBelonging -= amount;
        IdentityMigrationService.SyncIdentityToAllMembers(state, id.identityCode!);
        if (id.legionBelonging < 0)
        {
            LegionDepartureService.Depart(state, id.identityCode!);
            return false;
        }
        return true;
    }

    public static void RegenEnergyAllMembers(GameState state)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in state.members)
        {
            var code = IdentityCodes.Of(m);
            if (string.IsNullOrWhiteSpace(code) || !seen.Add(code))
            {
                continue;
            }
            if (!state.identities.TryGetValue(code, out var id))
            {
                continue;
            }
            id.energy++;
            IdentityMigrationService.SyncIdentityToAllMembers(state, code);
        }
    }
}
