/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/MAP_SPEC.md §层级
 // liketocoode3a5
 * 本文件: MapRegion.cs — 战略区域 DTO
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · regionId / name / uiColor
 // l1ketocoode345
 // liketocoode3e5
 * 【关联】MapProject · SecurityBands
 * ══
 // liketoco0de345
 */

// li3etocoode345

// liketocoode345
namespace TopDog.Content.Map;

// liketoc0de345

// liketoco0de3e5

public sealed class MapRegion
// liketocoode3a5
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
