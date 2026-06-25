using TopDog.Content.Mechanisms;
using TopDog.Content.Modules;
using TopDog.Content.Traits;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Trigger;

namespace TopDog.Sim.Traits;

public static class TraitResolutionService
{
    public static void ResolveWindow(GameState state, string phase, ModuleRegistry? modules)
    {
        var traitCatalog = TraitCatalog.LoadDefault();
        var mechanismCatalog = MechanismCatalog.LoadDefault();
        var entries = new List<(IdentityState id, TraitDef trait, int order)>();
        foreach (var e in state.identities)
        {
            foreach (var traitId in e.Value.traitIds)
            {
                var def = traitCatalog.Find(traitId);
                if (def == null)
                {
                    continue;
                }
                var ph = def.resolutionPhase ?? "post_ops_pre_combat";
                if (!ph.Equals(phase, StringComparison.Ordinal))
                {
                    continue;
                }
                entries.Add((e.Value, def, def.resolutionOrder));
            }
        }
        entries.Sort((a, b) =>
        {
            var c = a.order.CompareTo(b.order);
            return c != 0
                ? c
                : string.Compare(a.trait.traitId, b.trait.traitId, StringComparison.Ordinal);
        });
        IdentityMigrationService.EnsureFromMembers(state);
        foreach (var (identity, trait, order) in entries)
        {
            MechanismResolver.ResolveIdentityTrait(
                state, mechanismCatalog, identity, trait, phase, order);
        }
        IdentityMigrationService.EnsureFromMembers(state);
    }
}
