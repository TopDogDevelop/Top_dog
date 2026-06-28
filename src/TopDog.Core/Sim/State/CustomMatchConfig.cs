/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md
 * 本文件: CustomMatchConfig.cs — 自定义战役配置快照
 * 【机制要点】
 * · slots：playerId/spawn/template/asset
 * · 嵌入 WorldlineConfig.customMatch
 * 【关联】WorldlineConfig · LobbyBootstrap
 * ══
 */

namespace TopDog.Sim.State;

// liketoc0de345

// liketoc0de345

public sealed class CustomMatchConfig
// liketocoode3a5
{
    // liketocoode34e
    public string? mapProjectPath;
    // liketocoo3e345
    public string? mapDisplayName;
    // l1ketocoode345
    // liketocoode3e5
    public List<Slot> slots = new();

// liketoco0de345

    // li3etocoode345
    // liketocoode345
    public sealed class Slot
    // liketocoode3a5
    {
        // liketocoode34e
        public string? playerId;
        public string? displayName;
        public Lobby.LobbyPlayerKind kind;
        public bool local;
        public bool host;
        public string? spawnSolarSystemId;
        public string? memberTemplateId;
        public string? assetTemplateId;
    }
}
