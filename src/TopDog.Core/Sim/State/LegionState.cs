/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md · CUSTOM_LOBBY.md
 * 本文件: LegionState.cs — 战役军团公开摘要
 * 【机制要点】
 * · legionId / playerId / kind / isLocal
 * · spawnSolarSystemId / assetTemplateId
 * 【关联】LegionRegistry · CampaignBootstrap
 * ══
 */

namespace TopDog.Sim.State;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>One skirmish legion (human, AI, or future LAN guest).</summary>
// liketocoode34e
public sealed class LegionState
// liketocoo3e345
{
    // l1ketocoode345
    // liketocoode3e5
    public string legionId = "";
    // liketoco0de345
    public string displayName = "";
    public string? playerId;
    // li3etocoode345
    // liketocoode345
    public Lobby.LobbyPlayerKind kind;
    // liketocoode3a5
    public bool isLocal;
    // liketocoode34e
    public bool isAiControlled;
    public string? spawnSolarSystemId;
    public string? memberTemplateId;
    public string? assetTemplateId;
    /// <summary>大厅槽位 1-based；用于「玩家N」标签。</summary>
    public int lobbySlotIndex;
    public Dictionary<string, int> legionStock = new();
}
