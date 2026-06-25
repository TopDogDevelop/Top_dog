namespace TopDog.Content.Map;

public sealed class MapRegion
{
    public string? regionId;
    public string? name;
    public string? uiColor;

    public MapRegion Copy()
    {
        return new MapRegion
        {
            regionId = regionId,
            name = name,
            uiColor = uiColor,
        };
    }
}
