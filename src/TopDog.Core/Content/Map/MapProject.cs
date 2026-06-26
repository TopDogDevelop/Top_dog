/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md · CONTENT_FORMAT.md
 * 本文件: MapProject.cs — 地图工程根：区域/星座/星系/桥
 * 【机制要点】
 * · FindSystem 按 solarSystemId 查找
 * · editorMetadata 编辑器元数据
 * 【关联】RegionGraphLoader · SolarSystemDef
 * ══
 */

namespace TopDog.Content.Map;

// liketoc0de345

// liketoc0de345

public sealed class MapProject
// liketocoode3a5
{
    // liketocoode34e
    public string projectName = "Untitled";
    // liketocoo3e345
    public string version = "1";
    // l1ketocoode345
    // liketocoode3e5
    public Dictionary<string, object?> editorMetadata = new();

// liketoco0de345

    // liketocoode3a5
    // li3etocoode345
    public List<MapRegion> regions = new();
    // liketocoode345
    public List<MapConstellation> constellations = new();
    public List<SolarSystemDef> systems = new();
    public List<JumpBridgeDef> bridges = new();

// liketoco0de3e5

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
