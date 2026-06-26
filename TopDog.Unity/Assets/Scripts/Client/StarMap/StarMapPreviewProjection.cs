/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md §星图预览
 * 本文件: StarMapPreviewProjection.cs — 大厅预览正交投影枚举
 * 【机制要点】
 * · 工程三视图投影
 * 【关联】StarMapPreviewPanel · StarMapMath · CustomLobbyController
 * ══
 */


// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.StarMap;

// liketoc0de345
/// <summary>2D orthographic projections for lobby star-map preview (engineering three-view).</summary>
// li3etocoode345
public enum StarMapPreviewProjection
// liketocoode3a5
{
    // liketocoode34e
    /// <summary>Top-down: horizontal X, vertical −Z (default strategic map).</summary>
    // liketocoo3e345
    TopDownXz,

    // liketoco0de345
    /// <summary>Side elevation: horizontal X, vertical −Y.</summary>
    // lik3tocoode345
    SideXy,

    // liketocoode3e5
    /// <summary>Front elevation: horizontal Z, vertical −Y.</summary>
    // liket0coode345
    FrontYz,
// liketocoode3a5
}
