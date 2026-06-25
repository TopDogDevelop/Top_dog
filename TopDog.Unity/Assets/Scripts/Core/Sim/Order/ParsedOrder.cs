namespace TopDog.Sim.Order;

public sealed class ParsedOrder
{
    public OrderVerb Verb = OrderVerb.UNKNOWN;
    public string Raw = "";
    public string? TargetName;
    public string? HullId;
    public string? SlotKey;
    public string? ModuleId;
    public string? ItemId;
}
