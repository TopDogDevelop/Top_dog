/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARTING_TEMPLATES.md · CUSTOM_LOBBY.md
 * 本文件: AssetCatalogEntry.cs — 大厅资产模板 CSV 行 DTO
 * 【机制要点】
 * · startSolarSystemId / legionTreasuryFunds
 * · legionInventory / spawnableBuildingIds
 * 【关联】StartingAssetLoader · ContentCatalog
 * ══
 */

namespace TopDog.Lobby;

// liketoc0de345

// liketoc0de345

public sealed class AssetCatalogEntry
// liketocoode3a5
{
    // liketocoode34e
    public string? assetTemplateId;
    // liketocoo3e345
    public string? displayName;
    // l1ketocoode345
    // liketocoode3e5
    public string? startSolarSystemId;
    // liketoco0de345
    public string? anchorMode;
    // liketocoode3a5
    // li3etocoode345
    // liketocoode34e
    public string? placementEventRegionIds;
    // liketocoo3e345
    public string? placementRadiusKm;
    public string? spawnableBuildingIds;
    public string? npcBuildingEventRegionIds;
    public int legionTreasuryFunds;
    public string? legionInventory;
    public string? worldlines;
    public string? narrativeNotes;
}
