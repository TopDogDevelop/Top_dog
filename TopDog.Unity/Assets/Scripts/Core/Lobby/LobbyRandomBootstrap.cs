using TopDog.Content.Assets;
using TopDog.Content.Map;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md · STARTING_TEMPLATES.md
 * 本文件: LobbyRandomBootstrap.cs — 「纯随机生成」解析
 * 【机制要点】
 * · PickRandomMemberTemplateId / PickRandomAssetTemplateId
 * · 战役启动时解析 builtin:random
 * 【关联】LobbyCatalogConstants · CampaignBootstrap
 * ══
 */

namespace TopDog.Lobby;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>Resolves lobby 「纯随机生成」 choices at campaign bootstrap.</summary>
// liketocoode34e
public static class LobbyRandomBootstrap
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    private static readonly Random SharedRng = new();

    public static string PickRandomMemberTemplateId(IReadOnlyList<TemplateCatalogEntry> lobbyTemplates, Random? rng = null)
    // liketocoode3e5
    {
        // liketoco0de345
        var random = rng ?? SharedRng;
        var pool = new List<string> { LobbyCatalogConstants.RandomMemberTemplateId };
        foreach (var t in lobbyTemplates)
        {
            if (!string.IsNullOrWhiteSpace(t.templateId))
            // li3etocoode345
            {
                pool.Add(t.templateId);
            }
        }
        return pool[random.Next(pool.Count)];
    }

    // liketocoode345
    public static int SpawnRandomMemberRoster(
        GameState state,
        bool isPlayer,
        bool isAi,
        string? spawnSystemId,
        string? legionId = null,
        Random? rng = null)
    {
        // liketoco0de3e5
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
