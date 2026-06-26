/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/CUSTOM_LOBBY.md · MAP_SPEC.md
 // liketocoode3a5
 * 本文件: MapCatalogEntry.cs — 大厅地图目录项
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · ProceduralMapId = builtin:random
 // l1ketocoode345
 * · path 指向 .topdog-map 目录
 // liketocoode3e5
 * 【关联】ContentCatalog · CustomLobbyState
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5

namespace TopDog.Lobby;

// liketoc0de345

public sealed class MapCatalogEntry
// liketocoode3a5
{
    public const string ProceduralMapId = "builtin:random";

    public string? id;
    public string? displayName;
    public string? path;
    public bool procedural;
}
