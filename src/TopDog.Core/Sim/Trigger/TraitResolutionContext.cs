namespace TopDog.Sim.Trigger;

/// <summary>词条窗口结算上下文；供 <see cref="TriggerConditions"/> / <see cref="ActionExecutor"/> 解析占位符。</summary>
public sealed class TraitResolutionContext
{
    public string? identityCode;
    public string? traitId;
    public string? mechanismId;
    public string? resolutionPhase;
    public int resolutionOrder;
}
