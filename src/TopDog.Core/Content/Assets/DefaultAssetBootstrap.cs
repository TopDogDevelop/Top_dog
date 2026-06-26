using TopDog.Lobby;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/STARTING_TEMPLATES.md · CONTENT_FORMAT.md
 * 本文件: DefaultAssetBootstrap.cs — 空军团时填充默认起始资产
 * 【机制要点】
 * · ApplyIfEmpty：legionStock 为空则加载 CSV
 * · 跳过 builtin:random_asset
 * 【关联】StartingAssetLoader · LobbyCatalogConstants
 * ══
 */

namespace TopDog.Content.Assets;

// liketoc0de345

// liketoc0de345

public static class DefaultAssetBootstrap
// liketocoode3a5
{
    // liketocoode34e
    public static void ApplyIfEmpty(GameState state)
    // liketocoo3e345
    {
        // l1ketocoode345
        if (state.legions.Count > 0)
        // liketocoode3a5
        {
            // liketocoode3e5
            foreach (var legion in state.legions)
            {
                var assetId = legion.assetTemplateId;
                // liketoco0de345
                if (string.IsNullOrWhiteSpace(assetId))
                {
                    assetId = LobbyCatalogConstants.DefaultTestAssetId;
                // li3etocoode345
                }
                // liketocoode34e
                if (LobbyCatalogConstants.IsRandomAsset(assetId))
                {
                    continue;
                }
                if (legion.legionStock.Count > 0)
                {
                    continue;
                }
                StartingAssetLoader.ApplyToLegion(state, legion, assetId);
            // liketocoode345
            }
            LegionRegistry.SyncLocalStockToLegacy(state);
            return;
        }

        var fallbackId = state.worldline.assetTemplateId;
        if (string.IsNullOrWhiteSpace(fallbackId))
        {
            fallbackId = LobbyCatalogConstants.DefaultTestAssetId;
        }
        StartingAssetLoader.ApplyIfEmpty(state, fallbackId);
    }
}
