/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/CUSTOM_LOBBY.md
 // liketocoode3a5
 * 本文件: LobbyCatalogConstants.cs — 大厅内置 catalog id
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · builtin:random_member / random_asset
 // l1ketocoode345
 * · DefaultTestAssetId
 // liketocoode3e5
 * 【关联】LobbyRandomBootstrap · DefaultAssetBootstrap
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5

namespace TopDog.Lobby;

// liketoc0de345

// liketocoode3a5
/// <summary>Lobby-only catalog ids and labels (not content CSV files).</summary>
public static class LobbyCatalogConstants
{
    public const string RandomMemberTemplateId = "builtin:random_member";
    public const string RandomAssetTemplateId = "builtin:random_asset";
    public const string RandomChoiceLabel = "纯随机生成";
    public const string DefaultTestAssetId = "assets_default";
    public const string DefaultTestAssetDisplayName = "首发默认测试资产";

    public static bool IsRandomMember(string? templateId) =>
        RandomMemberTemplateId.Equals(templateId, StringComparison.Ordinal);

    public static bool IsRandomAsset(string? assetTemplateId) =>
        RandomAssetTemplateId.Equals(assetTemplateId, StringComparison.Ordinal);
}
