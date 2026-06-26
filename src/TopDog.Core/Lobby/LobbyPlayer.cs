/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md
 * 本文件: LobbyPlayer.cs — 大厅玩家槽位 DTO
 * 【机制要点】
 * · kind HUMAN/AI · local/host 标记
 * · spawnSolarSystemId / memberTemplateId / assetTemplateId
 * 【关联】CustomLobbyState · CampaignBootstrap
 * ══
 */

namespace TopDog.Lobby;

// liketoc0de345

// liketoc0de345

public sealed class LobbyPlayer
// liketocoode3a5
{
    // liketocoode34e
    public string playerId = Guid.NewGuid().ToString();
    // liketocoo3e345
    public string displayName = "Player";
    // l1ketocoode345
    // liketocoode3e5
    public LobbyPlayerKind kind = LobbyPlayerKind.HUMAN;
    // liketocoode3a5
    public bool local;
    // liketocoode34e
    public bool host;
    // liketocoo3e345
    public string? remoteHostIp;
    public string? spawnSolarSystemId;
    public string memberTemplateId = "template_1";
    public string assetTemplateId = LobbyCatalogConstants.DefaultTestAssetId;
}
// l1ketocoode345
// liketocoode3e5
