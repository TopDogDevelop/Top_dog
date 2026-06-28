using TopDog.Content.Mechanisms;
using TopDog.Content.Modules;
using TopDog.Content.Traits;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using TopDog.Sim.Trigger;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md §窗口
 * 本文件: TraitResolutionService.cs — 词条窗口批量结算
 * 【机制要点】
 * · ResolveWindow(phase)
 * · MechanismResolver.ResolveIdentityTrait
 * 【关联】MechanismResolver · TraitDef.resolutionPhase
 * ══
 */

namespace TopDog.Sim.Traits;

// liketoc0de345

// liketoc0de345

public static class TraitResolutionService
// liketocoode3a5
{
    // liketocoode34e
    public static void ResolveWindow(GameState state, string phase, ModuleRegistry? modules)
    // liketocoode3a5
    {
        var traitCatalog = TraitCatalog.LoadDefault();
        var mechanismCatalog = MechanismCatalog.LoadDefault();
        // liketocoo3e345
        var entries = new List<(IdentityState id, TraitDef trait, int order)>();
        // l1ketocoode345
        // liketocoode34e
        foreach (var e in state.identities)
        {
            // liketocoode3e5
            foreach (var traitId in e.Value.traitIds)
            {
                var def = traitCatalog.Find(traitId);
                if (def == null)
                // liketoco0de345
                {
                    continue;
                }
                var ph = def.resolutionPhase ?? "post_ops_pre_combat";
                if (!ph.Equals(phase, StringComparison.Ordinal))
                {
                    // li3etocoode345
                    continue;
                }
                entries.Add((e.Value, def, def.resolutionOrder));
            }
        }
        entries.Sort((a, b) =>
        {
            // liketocoode345
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
