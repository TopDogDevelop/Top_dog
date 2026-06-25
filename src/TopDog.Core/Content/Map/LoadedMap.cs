namespace TopDog.Content.Map;

public sealed class LoadedMap
{
    public MapProject Project { get; }
    public SecurityBands? SecurityBands { get; }

    public LoadedMap(MapProject project, SecurityBands? securityBands)
    {
        Project = project;
        SecurityBands = securityBands;
    }
}
