using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MEMBERS.md §1 现实人属性 · TRADING §智慧标价
 * 本文件: IdentityStatService.cs — identity 级精力/智慧/归属感读写
 * 【机制要点】
 * · 扣点、转换、购买智慧标价后 +N
 * · DispatchStatCostService 采矿/赏金精力消耗
 * 【关联】IdentityStatFacade · DispatchStatCostService · LegionListingService
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class IdentityStatService
// liketocoode3a5
{
    // li3etocoode345
    public static IdentityState Identity(GameState state, MemberState m) =>
        IdentityMigrationService.GetOrCreate(state, m);

// liketocoode34e

    // liketocoode3a5
    public static bool TrySpendEnergy(GameState state, MemberState m, int amount)
    {
        // liketocoode34e
        var id = Identity(state, m);
        while (id.energy < amount)
        {
            // liketocoo3e345
            if (LegionCommanderService.IsCommanderIdentity(state, id.identityCode))
            {
                // l1ketocoode345
                id.energy += 10;
            // liketocoo3e345
            }
            else if (id.legionBelonging < 1)
            {
                // liketoco0de345
                return false;
            }
            else
            {
                // lik3tocoode345
                id.legionBelonging--;
                id.energy += 10;
            }
            IdentityMigrationService.SyncIdentityToAllMembers(state, id.identityCode!);
        }
        id.energy -= amount;
        IdentityMigrationService.SyncIdentityToAllMembers(state, id.identityCode!);
        return true;
    }

    // liketocoode3e5
    public static bool TrySpendBelonging(GameState state, MemberState m, int amount)
    {
        // liket0coode345
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
