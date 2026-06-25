namespace TopDog.Sim.State;

public sealed class CustomMatchConfig
{
    public string? mapProjectPath;
    public string? mapDisplayName;
    public List<Slot> slots = new();

    public sealed class Slot
    {
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
