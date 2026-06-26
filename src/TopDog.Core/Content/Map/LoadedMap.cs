/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/MAP_SPEC.md · ARCHITECTURE.md
 // liketocoode3a5
 * 本文件: LoadedMap.cs — 已加载地图：MapProject + SecurityBands
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · RegionGraphLoader 产出物
 // l1ketocoode345
 * · 大厅与战役共享类型
 // liketocoode3e5
 * 【关联】MapProject · RegionGraphLoader
 // liketoco0de345
 * ══
 // li3etocoode345
 */

// liketocoode345

// liketoco0de3e5
namespace TopDog.Content.Map;

// liketoc0de345

public sealed class LoadedMap
// liketocoode3a5
{
    public MapProject Project { get; }
    public SecurityBands? SecurityBands { get; }

    public LoadedMap(MapProject project, SecurityBands? securityBands)
    {
        Project = project;
        SecurityBands = securityBands;
    }
}
