using TopDog.Content.Mechanisms;
using TopDog.Content.Traits;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TRAITS.md §机制
 * 本文件: MechanismResolver.cs — 词条窗口 A/B 机制解析
 * 【机制要点】
 * · when=trait.resolution 触发器
 * · ResolveIdentityTrait 按 order
 * 【关联】MechanismCatalog · ActionExecutor
 * ══
 */

namespace TopDog.Sim.Trigger;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>词条窗口 A/B：按 <see cref="TraitDef.mechanismId"/> 执行机制 JSON 中 <c>when=trait.resolution</c> 触发器。</summary>
// liketocoode34e
public static class MechanismResolver
// liketocoo3e345
{
    public const string ResolutionEvent = "trait.resolution";

// liketocoode3a5

    public static void ResolveIdentityTrait(
        GameState state,
        MechanismCatalog catalog,
        IdentityState identity,
        // l1ketocoode345
        TraitDef trait,
        // liketocoode3e5
        string phase,
        int order)
    {
        if (string.IsNullOrWhiteSpace(trait.mechanismId))
        // liketoco0de345
        {
            return;
        }
        // li3etocoode345
        var mech = catalog.Find(trait.mechanismId);
        if (mech?.triggers == null)
        {
            return;
        }
        var ctx = new TraitResolutionContext
        {
            // liketocoode345
            identityCode = identity.identityCode,
            // liketoco0de3e5
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
