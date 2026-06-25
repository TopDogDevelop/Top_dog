using System;

namespace TopDog.Net.Lan;

/// <summary>UDP LAN packet encoding for room beacons and join requests.</summary>
public static class LanProtocol
{
    public const string RoomMagic = "TOP_DOG_LAN";
    public const string JoinMagic = "TOP_DOG_JOIN";
    public const int Protocol = 1;

    public static string BuildRoomPayload(
        string roomId, string hostIp, int playerCount, string mapId, int port)
    {
        return RoomMagic + "|" + Protocol + "|{"
               + "\"roomId\":\"" + Esc(roomId) + "\","
               + "\"hostIp\":\"" + Esc(hostIp) + "\","
               + "\"hostName\":\"" + Esc(hostIp) + "\","
               + "\"playerCount\":" + playerCount + ","
               + "\"mapId\":\"" + Esc(mapId) + "\","
               + "\"port\":" + port
               + "}";
    }

    public static string BuildJoinPayload(string joinerIp)
    {
        return JoinMagic + "|" + Protocol + "|{"
               + "\"joinerIp\":\"" + Esc(joinerIp) + "\","
               + "\"joinerName\":\"" + Esc(joinerIp) + "\""
               + "}";
    }

    public static PeerAnnouncement? ParseRoomBeacon(string msg, string fallbackIp)
    {
        if (!msg.StartsWith(RoomMagic + "|", StringComparison.Ordinal))
        {
            return null;
        }
        var parts = msg.Split('|', 3);
        if (parts.Length < 3 || parts[1] != Protocol.ToString())
        {
            return null;
        }
        var json = parts[2];
        var p = new PeerAnnouncement
        {
            hostIp = Extract(json, "hostIp") ?? fallbackIp,
            roomId = Extract(json, "roomId"),
            hostName = Extract(json, "hostName"),
            mapId = Extract(json, "mapId"),
        };
        var pc = Extract(json, "playerCount");
        if (pc != null && int.TryParse(pc, out var count))
        {
            p.playerCount = count;
        }
        return p;
    }

    public static string? ParseJoinerIp(string msg)
    {
        if (!msg.StartsWith(JoinMagic + "|", StringComparison.Ordinal))
        {
            return null;
        }
        var parts = msg.Split('|', 3);
        if (parts.Length < 3)
        {
            return null;
        }
        return Extract(parts[2], "joinerIp");
    }

    public static void ApplyJoinerToLobby(Lobby.CustomLobbyState lobby, string? joinerIp, string localIp)
    {
        if (string.IsNullOrWhiteSpace(joinerIp) || joinerIp == localIp)
        {
            return;
        }
        foreach (var p in lobby.players)
        {
            if (joinerIp == p.remoteHostIp || joinerIp == p.displayName)
            {
                return;
            }
        }
        if (lobby.players.Count >= lobby.maxPlayers)
        {
            return;
        }
        var player = new Lobby.LobbyPlayer
        {
            kind = Lobby.LobbyPlayerKind.HUMAN,
            displayName = joinerIp,
            remoteHostIp = joinerIp,
            local = false,
            host = false,
        };
        lobby.players.Add(player);
    }

    private static string? Extract(string json, string key)
    {
        var needle = "\"" + key + "\":";
        var i = json.IndexOf(needle, StringComparison.Ordinal);
        if (i < 0)
        {
            return null;
        }
        var start = i + needle.Length;
        if (start < json.Length && json[start] == '"')
        {
            var end = json.IndexOf('"', start + 1);
            if (end > start)
            {
                return json[(start + 1)..end];
            }
        }
        else
        {
            var end = json.IndexOf(',', start);
            if (end < 0)
            {
                end = json.IndexOf('}', start);
            }
            if (end > start)
            {
                return json[start..end].Trim();
            }
        }
        return null;
    }

    private static string Esc(string? s)
    {
        if (s == null)
        {
            return "";
        }
        return s.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
