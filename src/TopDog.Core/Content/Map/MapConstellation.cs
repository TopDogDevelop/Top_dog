namespace TopDog.Content.Map;

public sealed class MapConstellation
{
    public string? constellationId;
    public string? name;
    public string? regionId;

    public MapConstellation Copy()
    {
        return new MapConstellation
        {
            constellationId = constellationId,
            name = name,
            regionId = regionId,
        };
    }
}
