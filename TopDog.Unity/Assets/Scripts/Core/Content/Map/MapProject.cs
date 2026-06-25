namespace TopDog.Content.Map;

public sealed class MapProject
{
    public string projectName = "Untitled";
    public string version = "1";
    public Dictionary<string, object?> editorMetadata = new();

    public List<MapRegion> regions = new();
    public List<MapConstellation> constellations = new();
    public List<SolarSystemDef> systems = new();
    public List<JumpBridgeDef> bridges = new();

    public SolarSystemDef? FindSystem(string? id)
    {
        if (id == null)
        {
            return null;
        }
        foreach (var s in systems)
        {
            if (id.Equals(s.solarSystemId, StringComparison.Ordinal))
            {
                return s;
            }
        }
        return null;
    }
}
