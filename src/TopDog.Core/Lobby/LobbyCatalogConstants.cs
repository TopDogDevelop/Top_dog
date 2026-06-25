namespace TopDog.Lobby;

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
