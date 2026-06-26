using TopDog.Foundation.Io;
using TopDog.Lobby;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARTING_TEMPLATES.md · CONTENT_FORMAT.md
 * 本文件: StartingAssetLoader.cs — 起始资产 CSV 解析与应用
 * 【机制要点】
 * · LoadEntry / ParseAssetCsv
 * · ApplyToState：军团库存、建筑、资金
 * 【关联】DefaultAssetBootstrap · AssetCatalogEntry
 * ══
 */

namespace TopDog.Content.Assets;

// liketoc0de345

// liketoc0de345

public static class StartingAssetLoader
// liketocoode3a5
{
    // liketocoode34e
    public static AssetCatalogEntry? LoadEntry(string? assetTemplateId)
    // liketocoo3e345
    {
        if (string.IsNullOrWhiteSpace(assetTemplateId))
        // liketocoode3a5
        {
            return null;
        // l1ketocoode345
        }
        var path = Path.Combine(AppRoot.StartingAssetsDir(), assetTemplateId + ".csv");
        if (!File.Exists(path))
        {
            // liketocoode3e5
            return null;
        // liketoco0de345
        }
        return ParseAssetCsv(path);
    // li3etocoode345
    }

    public static void ApplyToState(GameState state, string? assetTemplateId)
    {
        var local = LegionRegistry.Local(state);
        if (local != null)
        // liketocoode345
        {
            // liketoco0de3e5
            ApplyToLegion(state, local, assetTemplateId);
            LegionRegistry.SyncLocalStockToLegacy(state);
            return;
        }
        ApplyToLegacyStock(state, assetTemplateId);
    }

    public static void ApplyToLegion(GameState state, LegionState legion, string? assetTemplateId)
    {
        var entry = LoadEntry(assetTemplateId);
        if (entry == null)
        {
            return;
        }
        if (!string.IsNullOrWhiteSpace(entry.startSolarSystemId)
            && string.IsNullOrWhiteSpace(legion.spawnSolarSystemId)
            && legion.isLocal)
        {
            legion.spawnSolarSystemId = entry.startSolarSystemId;
            if (string.IsNullOrWhiteSpace(state.currentSolarSystemId))
            {
                state.currentSolarSystemId = entry.startSolarSystemId;
            }
        }
        if (entry.legionTreasuryFunds > 0)
        {
            legion.legionStock[CurrencyIds.StarCoin] =
                legion.legionStock.GetValueOrDefault(CurrencyIds.StarCoin, 0) + entry.legionTreasuryFunds;
        }
        ApplyInventory(legion.legionStock, entry.legionInventory);
        if (!string.IsNullOrWhiteSpace(entry.anchorMode) && legion.isLocal)
        {
            state.flags["start.anchorMode"] = entry.anchorMode!;
        }
    }

    private static void ApplyToLegacyStock(GameState state, string? assetTemplateId)
    {
        var entry = LoadEntry(assetTemplateId);
        if (entry == null)
        {
            return;
        }
        if (!string.IsNullOrWhiteSpace(entry.startSolarSystemId) && string.IsNullOrWhiteSpace(state.currentSolarSystemId))
        {
            state.currentSolarSystemId = entry.startSolarSystemId;
        }
        if (entry.legionTreasuryFunds > 0)
        {
            state.legionStock[CurrencyIds.StarCoin] =
                state.legionStock.GetValueOrDefault(CurrencyIds.StarCoin, 0) + entry.legionTreasuryFunds;
        }
        ApplyInventory(state.legionStock, entry.legionInventory);
        if (!string.IsNullOrWhiteSpace(entry.anchorMode))
        {
            state.flags["start.anchorMode"] = entry.anchorMode!;
        }
    }

    public static void ApplyIfEmpty(GameState state, string? assetTemplateId = "assets_default")
    {
        if (LobbyCatalogConstants.IsRandomAsset(assetTemplateId))
        {
            LobbyRandomBootstrap.ApplyRandomAssetIfNeeded(state);
            return;
        }
        var stock = LegionRegistry.MutableLocalStock(state);
        if (stock.Count > 0)
        {
            return;
        }
        ApplyToState(state, assetTemplateId);
    }

    private static void ApplyInventory(Dictionary<string, int> stock, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }
        foreach (var token in raw.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var part = token.Trim();
            var colon = part.LastIndexOf(':');
            if (colon <= 0 || colon >= part.Length - 1)
            {
                continue;
            }
            var id = part[..colon].Trim();
            if (!int.TryParse(part[(colon + 1)..].Trim(), out var count) || count <= 0)
            {
                continue;
            }
            stock[id] = stock.GetValueOrDefault(id, 0) + count;
        }
    }

    internal static AssetCatalogEntry? ParseAssetCsv(string csvFile)
    {
        var lines = File.ReadAllLines(csvFile);
        var keyRow = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("assetTemplateId,", StringComparison.Ordinal))
            {
                keyRow = i;
                break;
            }
        }
        if (keyRow < 0 || keyRow + 1 >= lines.Length)
        {
            return null;
        }
        var cols = SplitCsv(lines[keyRow]);
        var idx = IndexColumns(cols);
        for (var r = keyRow + 1; r < lines.Length; r++)
        {
            if (string.IsNullOrWhiteSpace(lines[r]))
            {
                continue;
            }
            var row = SplitCsv(lines[r]);
            var e = new AssetCatalogEntry
            {
                assetTemplateId = Get(row, idx, "assetTemplateId"),
            };
            if (string.IsNullOrWhiteSpace(e.assetTemplateId))
            {
                continue;
            }
            e.displayName = EmptyToNull(Get(row, idx, "displayName")) ?? e.assetTemplateId;
            e.startSolarSystemId = Get(row, idx, "startSolarSystemId");
            e.anchorMode = Get(row, idx, "anchorMode");
            e.placementEventRegionIds = Get(row, idx, "placementEventRegionIds");
            e.placementRadiusKm = Get(row, idx, "placementRadiusKm");
            e.spawnableBuildingIds = Get(row, idx, "spawnableBuildingIds");
            e.npcBuildingEventRegionIds = Get(row, idx, "npcBuildingEventRegionIds");
            e.legionInventory = Get(row, idx, "legionInventory");
            e.worldlines = Get(row, idx, "worldlines");
            e.narrativeNotes = Get(row, idx, "narrativeNotes");
            var funds = Get(row, idx, "legionTreasuryFunds");
            if (!string.IsNullOrWhiteSpace(funds) && int.TryParse(funds.Trim(), out var f))
            {
                e.legionTreasuryFunds = f;
            }
            return e;
        }
        return null;
    }

    private static Dictionary<string, int> IndexColumns(string[] cols)
    {
        var m = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < cols.Length; i++)
        {
            m[cols[i].Trim()] = i;
        }
        return m;
    }

    private static string? Get(string[] row, Dictionary<string, int> idx, string key)
    {
        if (!idx.TryGetValue(key, out var i) || i >= row.Length)
        {
            return null;
        }
        return row[i].Trim();
    }

    private static string[] SplitCsv(string line) => line.Split(',', StringSplitOptions.None);

    private static string? EmptyToNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
