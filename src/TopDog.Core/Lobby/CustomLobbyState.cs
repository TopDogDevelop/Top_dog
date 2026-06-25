namespace TopDog.Lobby;

public sealed class CustomLobbyState
{
    public const int DefaultLanPort = 28777;

    public string roomId = Guid.NewGuid().ToString();
    public string roomName = "自定义战役";
    public string? mapPath;
    public string? mapDisplayName;
    public bool proceduralMap;
    public int proceduralSystemCount = 20;
    public float proceduralBridgeDensity = 1f;
    public int proceduralSeed;
    public List<LobbyPlayer> players = new();
    public string? selectedPlayerId;
    public int maxPlayers = 8;
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
        return null;
    }

    public LobbyPlayer? FindSelected()
    {
        if (selectedPlayerId == null)
        {
            return players.Count > 0 ? players[0] : null;
        }
        foreach (var p in players)
        {
            if (selectedPlayerId.Equals(p.playerId, StringComparison.Ordinal))
            {
                return p;
            }
        }
        return players.Count > 0 ? players[0] : null;
    }

    public int NextAiNumber()
    {
        var n = 1;
        while (true)
        {
            var prefix = "人机 " + n;
            var taken = false;
            foreach (var p in players)
            {
                if (p.kind == LobbyPlayerKind.AI && prefix.Equals(p.displayName, StringComparison.Ordinal))
                {
                    taken = true;
                    break;
                }
            }
            if (!taken)
            {
                return n;
            }
            n++;
        }
    }
}
