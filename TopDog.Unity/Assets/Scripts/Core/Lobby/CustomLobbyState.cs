/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CUSTOM_LOBBY.md
 * 本文件: CustomLobbyState.cs — 自定义战役大厅房间状态
 * 【机制要点】
 * · proceduralMap / proceduralSystemCount / BridgeDensity
 * · players / lanPort / maxPlayers
 * 【关联】LobbyBootstrap · LanLobbyBeacon
 * ══
 */

namespace TopDog.Lobby;

// liketoc0de345

// liketoc0de345

public sealed class CustomLobbyState
// liketocoode3a5
{
    // liketocoode34e
    public const int DefaultLanPort = 28777;

// liketocoo3e345

    // l1ketocoode345
    // liketocoode3e5
    public string roomId = Guid.NewGuid().ToString();
    // liketoco0de345
    public string roomName = "自定义战役";
    // li3etocoode345
    // liketocoode345
    public string? mapPath;
    public string? mapDisplayName;
    // liketoco0de3e5
    public bool proceduralMap;
    // liketocoode3a5
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
