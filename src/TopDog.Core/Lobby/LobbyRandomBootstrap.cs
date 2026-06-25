using TopDog.Content.Assets;
using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

namespace TopDog.Lobby;

/// <summary>Resolves lobby 「纯随机生成」 choices at campaign bootstrap.</summary>
public static class LobbyRandomBootstrap
{
    private static readonly Random SharedRng = new();

    public static string PickRandomMemberTemplateId(IReadOnlyList<TemplateCatalogEntry> lobbyTemplates, Random? rng = null)
    {
        var random = rng ?? SharedRng;
        var pool = new List<string> { LobbyCatalogConstants.RandomMemberTemplateId };
        foreach (var t in lobbyTemplates)
        {
            if (!string.IsNullOrWhiteSpace(t.templateId))
            {
                pool.Add(t.templateId);
            }
        }
        return pool[random.Next(pool.Count)];
    }

    public static int SpawnRandomMemberRoster(
        GameState state,
        bool isPlayer,
        bool isAi,
        string? spawnSystemId,
        string? legionId = null,
        Random? rng = null)
    {
        state.flags["lobby.randomMembers"] = "1";
        state.flags["lobby.randomMemberCount"] = RecruitService.LobbyRandomStartMemberCount.ToString();
        var r = rng ?? SharedRng;
        var traits = TraitCatalog.LoadDefault();
        var ships = ShipRegistry.LoadDefault();
        return RecruitService.CreateRandomLobbyRoster(
            state, traits, r, ships, isPlayer, isAi, spawnSystemId, legionId);
    }

    public static void ApplyRandomAssetIfNeeded(GameState state)
    {
        if (!LobbyCatalogConstants.IsRandomAsset(state.worldline.assetTemplateId))
        {
            return;
        }

        state.flags["lobby.randomAssets"] = "1";
        var map = state.map?.Project;
        if (map != null && map.systems.Count > 0)
        {
            var pick = map.systems[SharedRng.Next(map.systems.Count)];
            if (!string.IsNullOrWhiteSpace(pick.solarSystemId))
            {
                state.currentSolarSystemId = pick.solarSystemId;
            }
        }

        var funds = SharedRng.Next(8000, 25001);
        var stock = LegionRegistry.MutableLocalStock(state);
        stock[CurrencyIds.StarCoin] = stock.GetValueOrDefault(CurrencyIds.StarCoin, 0) + funds;
        LegionRegistry.SyncLocalStockToLegacy(state);
        state.flags["start.anchorMode"] = "PLAYER_CHOICE";
    }

    public static string? PickRandomSpawnSystem(LoadedMap? map, IReadOnlyCollection<string>? exclude = null)
    {
        if (map == null)
        {
            return null;
        }
        var pool = new List<string>();
        foreach (var s in map.Project.systems)
        {
            if (s.solarSystemId == null)
            {
                continue;
            }
            if (exclude != null && exclude.Contains(s.solarSystemId))
            {
                continue;
            }
            pool.Add(s.solarSystemId);
        }
        if (pool.Count == 0)
        {
            return map.Project.systems.Count > 0
                ? map.Project.systems[0].solarSystemId
                : null;
        }
        return pool[SharedRng.Next(pool.Count)];
    }
}
