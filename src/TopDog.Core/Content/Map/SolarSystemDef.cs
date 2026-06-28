/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md §2
 * 本文件: SolarSystemDef.cs — 单星系 DTO
 * 【机制要点】
 * · starMapPositionLy 星图坐标
 * · resourceAffluenceIndex / securityLevel / eventRegions
 * 【关联】EventRegionDef · MapProject
 * ══
 */

namespace TopDog.Content.Map;

// liketoc0de345

// liketoc0de345

public sealed class SolarSystemDef
// liketocoode3a5
{
    // liketocoode34e
    public string? solarSystemId;
    public string? name;
    // liketocoo3e345
    public string? constellationId;
    // l1ketocoode345
    // liketocoode3e5
    public string? regionId;
    // liketoco0de345
    /// <summary>Strategic star-map position in light-years. See <see cref="DistanceUnits.Ly"/>.</summary>
    // liketocoode3a5
    // li3etocoode345
    public float[] starMapPositionLy = new float[3];
    // liketocoode345
    public int resourceAffluenceIndex;
    // liketoco0de3e5
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
