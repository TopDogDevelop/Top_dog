namespace TopDog.Sim.State;

public sealed class PresentationDirective
{
    public string kind = "";
    public string? message;
    public string? messageTemplate;
    public string? attackerDisplayName;
    public float recoverySec;
    public string? dismissToken;
}
