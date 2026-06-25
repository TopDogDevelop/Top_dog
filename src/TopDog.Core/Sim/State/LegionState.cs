namespace TopDog.Sim.State;

/// <summary>One skirmish legion (human, AI, or future LAN guest).</summary>
public sealed class LegionState
{
    public string legionId = "";
    public string displayName = "";
    public string? playerId;
    public Lobby.LobbyPlayerKind kind;
    public bool isLocal;
    public bool isAiControlled;
    public string? spawnSolarSystemId;
    public string? memberTemplateId;
    public string? assetTemplateId;
    public Dictionary<string, int> legionStock = new();
}
