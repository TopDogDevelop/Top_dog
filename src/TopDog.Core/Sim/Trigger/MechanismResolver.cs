using TopDog.Content.Mechanisms;
using TopDog.Content.Traits;
using TopDog.Sim.State;

namespace TopDog.Sim.Trigger;

/// <summary>词条窗口 A/B：按 <see cref="TraitDef.mechanismId"/> 执行机制 JSON 中 <c>when=trait.resolution</c> 触发器。</summary>
public static class MechanismResolver
{
    public const string ResolutionEvent = "trait.resolution";

    public static void ResolveIdentityTrait(
        GameState state,
        MechanismCatalog catalog,
        IdentityState identity,
        TraitDef trait,
        string phase,
        int order)
    {
        if (string.IsNullOrWhiteSpace(trait.mechanismId))
        {
            return;
        }
        var mech = catalog.Find(trait.mechanismId);
        if (mech?.triggers == null)
        {
            return;
        }
        var ctx = new TraitResolutionContext
        {
            identityCode = identity.identityCode,
            traitId = trait.traitId,
            mechanismId = trait.mechanismId,
            resolutionPhase = phase,
            resolutionOrder = order,
        };
        foreach (var trigger in mech.triggers)
        {
            if (!ResolutionEvent.Equals(trigger.when, StringComparison.Ordinal))
            {
                continue;
            }
            if (!TriggerConditions.Passes(state, trigger.@if, ctx))
            {
                ActionExecutor.ExecuteAll(state, trigger.@else?.actions, ctx);
                continue;
            }
            ActionExecutor.ExecuteAll(state, trigger.then?.actions, ctx);
        }
    }
}
