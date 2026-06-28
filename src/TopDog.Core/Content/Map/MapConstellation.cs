/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/MAP_SPEC.md §层级
 // liketocoode3a5
 * 本文件: MapConstellation.cs — 星座 DTO（regionId 归属）
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · constellationId / name / regionId
 // l1ketocoode345
 * · Copy 深拷贝
 // liketocoode3e5
 * 【关联】MapProject · MapRegion
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5

namespace TopDog.Content.Map;

// liketoc0de345

public sealed class MapConstellation
// liketocoode3a5
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
