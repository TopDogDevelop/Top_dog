namespace TopDog.Content.Map;

public sealed class SolarSystemDef
{
    public string? solarSystemId;
    public string? name;
    public string? constellationId;
    public string? regionId;
    /// <summary>Strategic star-map position in light-years. See <see cref="DistanceUnits.Ly"/>.</summary>
    public float[] starMapPositionLy = new float[3];
    public int resourceAffluenceIndex;
    public int developmentDifficulty;
    public float securityLevel;
    public List<EventRegionDef> eventRegions = new();
    public List<string> jumpBridgeIds = new();

    public SolarSystemDef Copy()
    {
        var copy = new SolarSystemDef
        {
            solarSystemId = solarSystemId,
            name = name,
            constellationId = constellationId,
            regionId = regionId,
            starMapPositionLy = starMapPositionLy == null ? new float[3] : (float[])starMapPositionLy.Clone(),
            resourceAffluenceIndex = resourceAffluenceIndex,
            developmentDifficulty = developmentDifficulty,
            securityLevel = securityLevel,
            jumpBridgeIds = new List<string>(jumpBridgeIds),
        };
        foreach (var er in eventRegions)
        {
            copy.eventRegions.Add(er.Copy());
        }
        return copy;
    }
}
