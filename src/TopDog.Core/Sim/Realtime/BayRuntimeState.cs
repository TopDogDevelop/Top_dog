namespace TopDog.Sim.Realtime;

public enum CarriedUnitLifecycle
{
    Stored,
    Deploying,
    Deployed,
    Recalling,
    Recovering,
    Lost,
}

public sealed class BayRuntimeState
{
    public CarriedUnitLifecycle state = CarriedUnitLifecycle.Stored;
    public string? childUnitId;
    public string? shipInstanceId;
    public float transitionRemainingSec;
    public int reservedCapacity;
}

public sealed class CarriedShipPayload
{
    public string shipInstanceId = "";
    public string hullId = "";
    public string? operatorMemberId;
    public Dictionary<string, string> fittedModules = new(StringComparer.Ordinal);
    public Dictionary<string, CarriedShipPayload> carriedShipsBySlot = new(StringComparer.Ordinal);
    public float shieldHp = -1f;
    public float armorHp = -1f;
    public float structureHp = -1f;
    public bool destroyed;
}
