/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/STARTING_TEMPLATES.md · CUSTOM_LOBBY.md
 // liketocoode3a5
 * 本文件: TemplateCatalogEntry.cs — 开局团员模板目录项
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · templateId / defaultLegionName
 // l1ketocoode345
 * · lobbyVisible 大厅可见性
 // liketocoode3e5
 * 【关联】StartingTemplateLoader · ContentCatalog
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5

namespace TopDog.Lobby;

// liketoc0de345

public sealed class TemplateCatalogEntry
// liketocoode3a5
{
    public string? templateId;
    public string? displayName;
    public string? defaultLegionName;
    public string? assetTemplateId;
    public int? memberCount;
    public bool lobbyVisible = true;
}
