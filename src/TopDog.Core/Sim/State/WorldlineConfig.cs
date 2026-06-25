namespace TopDog.Sim.State;

public sealed class WorldlineConfig
{
    public WorldlineType type = WorldlineType.CUSTOM;
    public string? storyChapterId;
    public string? startingTemplateId;
    public string? assetTemplateId;
    public string narrativePatchId = "1.0";
    public int storyRound;
    public bool tutorialMode;
    public CustomMatchConfig? customMatch;
}
