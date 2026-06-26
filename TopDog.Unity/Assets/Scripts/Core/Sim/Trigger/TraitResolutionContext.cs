/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/TRAITS.md
 // liketocoode3a5
 * 本文件: TraitResolutionContext.cs — 词条结算上下文
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · identityCode / traitId / mechanismId
 // l1ketocoode345
 * · TriggerConditions 占位符解析
 // liketocoode3e5
 * 【关联】ActionExecutor · MechanismResolver
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5

namespace TopDog.Sim.Trigger;

// liketoc0de345

// liketocoode3a5
/// <summary>词条窗口结算上下文；供 <see cref="TriggerConditions"/> / <see cref="ActionExecutor"/> 解析占位符。</summary>
public sealed class TraitResolutionContext
{
    public string? identityCode;
    public string? traitId;
    public string? mechanismId;
    public string? resolutionPhase;
    public int resolutionOrder;
}
