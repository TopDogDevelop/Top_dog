using TopDog.Lobby;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

namespace TopDog.Content.Assets;

public static class DefaultAssetBootstrap
{
    public static void ApplyIfEmpty(GameState state)
    {
        if (state.legions.Count > 0)
        {
            foreach (var legion in state.legions)
            {
                var assetId = legion.assetTemplateId;
                if (string.IsNullOrWhiteSpace(assetId))
                {
                    assetId = LobbyCatalogConstants.DefaultTestAssetId;
                }
                if (LobbyCatalogConstants.IsRandomAsset(assetId))
                {
                    continue;
                }
                if (legion.legionStock.Count > 0)
                {
                    continue;
                }
                StartingAssetLoader.ApplyToLegion(state, legion, assetId);
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
