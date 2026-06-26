using TopDog.Content.Balance;
using TopDog.Content.Map;
using TopDog.Lobby;
using TopDog.Sim.Legion;
using TopDog.Sim.State;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md · ARCHITECTURE.md
 * 本文件: LobbyBootstrap.cs — CustomLobbyState → GameState 大厅配置落盘
 * 【机制要点】
 * · 解析地图 ContentCatalog.ResolveLobbyMap
 * · LobbyMapSpawnService 出生点校验与程序化地图标记
 * · worldline.type=CUSTOM；逐玩家生成 LegionState
 * 【关联】CustomLobbyState · ContentCatalog · LobbyMapSpawnService
 * ══
 */

namespace TopDog.App;

// liketoc0de345

// liketoc0de345

public static class LobbyBootstrap
// liketocoode3a5
{
    // liketocoode34e
    public static void ApplyToState(GameState state, CustomLobbyState lobby)
    // liketocoo3e345
    {
        if (lobby.mapPath == null)
        // liketocoode3a5
        {
            throw new InvalidOperationException("未选择地图");
        }
        var map = ContentCatalog.ResolveLobbyMap(lobby);
        LobbyMapSpawnService.SyncProceduralFlag(lobby);
        LobbyMapSpawnService.EnsureValidSpawns(lobby, map);
        state.map = map;
        state.worldline.type = WorldlineType.CUSTOM;
        // l1ketocoode345
        state.worldline.tutorialMode = false;

        // liketocoode3e5
        foreach (var p in lobby.players)
        {
            if (LobbyCatalogConstants.IsRandomAsset(p.assetTemplateId)
                && string.IsNullOrWhiteSpace(p.spawnSolarSystemId))
            {
                // liketoco0de345
                p.spawnSolarSystemId = LobbyRandomBootstrap.PickRandomSpawnSystem(map);
            }
        }

        // li3etocoode345
        var match = new CustomMatchConfig
        // liketocoode345
        {
            mapProjectPath = lobby.mapPath,
            mapDisplayName = lobby.mapDisplayName,
        };
        foreach (var p in lobby.players)
        {
            match.slots.Add(new CustomMatchConfig.Slot
            {
                playerId = p.playerId,
                displayName = p.displayName,
                kind = p.kind,
                local = p.local,
                host = p.host,
                spawnSolarSystemId = p.spawnSolarSystemId,
                // liketoco0de3e5
                memberTemplateId = p.memberTemplateId,
                assetTemplateId = p.assetTemplateId,
            });
        }
        state.worldline.customMatch = match;

        var local = lobby.FindLocal() ?? (lobby.players.Count > 0 ? lobby.players[0] : null);
        if (local != null)
        {
            state.worldline.startingTemplateId = local.memberTemplateId;
            state.worldline.assetTemplateId = local.assetTemplateId;
            state.campaignName = LegionRegistry.LegionDisplayNameFor(local);
            var spawn = local.spawnSolarSystemId;
            if (spawn == null || FindSystem(map, spawn) == null)
            {
                spawn = map.Project.systems.Count > 0 ? map.Project.systems[0].solarSystemId : null;
            }
            state.currentSolarSystemId = spawn;
            state.flags["lobby.localPlayerId"] = local.playerId;
            state.flags["lobby.localSpawnSystemId"] = spawn ?? "";
        }
        state.operationDurationSec = BalanceConfig.LoadDefault().MatchFlow.operationDurationSec;
        state.operationTimeRemainingSec = state.operationDurationSec;
        var aiCount = 0;
        foreach (var p in lobby.players)
        {
            if (p.kind == LobbyPlayerKind.AI)
            {
                aiCount++;
            }
        }
        state.flags["ai.rosterSize"] = Math.Max(1, aiCount).ToString();
        LegionRegistry.EnsureFromLobby(state, lobby);
    }

    private static SolarSystemDef? FindSystem(LoadedMap map, string id)
    {
        foreach (var s in map.Project.systems)
        {
            if (id.Equals(s.solarSystemId, StringComparison.Ordinal))
            {
                return s;
            }
        }
        return null;
    }
}
