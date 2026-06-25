using TopDog.Sim.State;

namespace TopDog.Sim.Member;

public static class IdentityStatFacade
{
    public static (int energy, int wisdom, int legionBelonging) Stats(GameState state, MemberState m)
    {
        var id = IdentityMigrationService.GetOrCreate(state, m);
        return (id.energy, id.wisdom, id.legionBelonging);
    }

    public static bool HasMirrorMismatch(GameState state, MemberState m)
    {
        var id = IdentityMigrationService.GetOrCreate(state, m);
        return m.energy != id.energy
            || m.wisdom != id.wisdom
            || m.legionBelonging != id.legionBelonging;
    }

    public static void SyncAllMirrors(GameState state)
    {
        IdentityMigrationService.EnsureFromMembers(state);
    }
}
