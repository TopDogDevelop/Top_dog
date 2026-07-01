namespace TopDog.Lobby;

public sealed class SkirmishRosterSlot
{
    public string memberTemplateId = "";
    public string memberTemplateRowId = "";
    public string memberId = "";
    public string displayName = "";
    public string hullId = "";
    public Dictionary<string, string?> fittedModules = new(StringComparer.Ordinal);
}

public sealed class SkirmishLobbyState
{
    public const int DefaultLanPort = 28777;
    public const int MatchUdpPort = 28778;
    public const string BuiltinMapId = "builtin:skirmish_single_system";

    public string roomId = Guid.NewGuid().ToString();
    public string roomName = "军团约战";
    public SkirmishLobbyMode mode = SkirmishLobbyMode.VsAi;
    public int scale = 10;
    public int seed;
    public List<LobbyPlayer> players = new();
    public Dictionary<string, List<SkirmishRosterSlot>> rosterByPlayerId = new(StringComparer.Ordinal);
    public string? selectedPlayerId;
    public int lanPort = DefaultLanPort;

    public LobbyPlayer? FindLocal()
    {
        foreach (var p in players)
        {
            if (p.local)
            {
                return p;
            }
        }

        return players.Count > 0 ? players[0] : null;
    }
}
