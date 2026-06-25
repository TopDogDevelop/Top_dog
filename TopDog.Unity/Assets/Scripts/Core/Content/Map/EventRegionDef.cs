namespace TopDog.Content.Map;

public sealed class EventRegionDef
{
    public string? eventRegionId;
    public string? kind;
    public string? name;
    /// <summary>Scene boundary radius in km (sim uses <see cref="DistanceUnits.KmToMeters"/>).</summary>
    public long radiusKm;
    /// <summary>Anchor within the solar system in AU. See <see cref="DistanceUnits.Au"/>.</summary>
    public float[] anchorAu = new float[3];
    public string? bridgeId;
    public string? targetSystemId;
    /// <summary>Primary mineable resource for oreBelt regions (e.g. res_inorganic).</summary>
    public string? primaryMineralId;

    public EventRegionDef Copy()
    {
        return new EventRegionDef
        {
            eventRegionId = eventRegionId,
            kind = kind,
            name = name,
            radiusKm = radiusKm,
            anchorAu = anchorAu == null ? new float[3] : (float[])anchorAu.Clone(),
            bridgeId = bridgeId,
            targetSystemId = targetSystemId,
            primaryMineralId = primaryMineralId,
        };
    }
}
