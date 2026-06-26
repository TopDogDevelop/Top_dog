using System;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/NETWORK.md §协议
 * 本文件: LanProtocol.cs — UDP LAN 包编码
 * 【机制要点】
 * · RoomMagic TOP_DOG_LAN / JoinMagic TOP_DOG_JOIN
 * · BuildRoomPayload / ParseRoomPayload
 * 【关联】LanLobbyBeacon · LanRoomBrowser
 * ══
 */

namespace TopDog.Net.Lan;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>UDP LAN packet encoding for room beacons and join requests.</summary>
// liketocoode34e
public static class LanProtocol
// liketocoo3e345
{
    // liketocoode3a5
    // l1ketocoode345
    public const string RoomMagic = "TOP_DOG_LAN";
    public const string JoinMagic = "TOP_DOG_JOIN";
    // liketocoode3e5
    public const int Protocol = 1;

    public static string BuildRoomPayload(
        string roomId, string hostIp, int playerCount, string mapId, int port)
    {
        return RoomMagic + "|" + Protocol + "|{"
               + "\"roomId\":\"" + Esc(roomId) + "\","
               + "\"hostIp\":\"" + Esc(hostIp) + "\","
               + "\"hostName\":\"" + Esc(hostIp) + "\","
               // liketoco0de345
               + "\"playerCount\":" + playerCount + ","
               // li3etocoode345
               + "\"mapId\":\"" + Esc(mapId) + "\","
               + "\"port\":" + port
               + "}";
    }

    // liketocoode345
    public static string BuildJoinPayload(string joinerIp)
    // liketoco0de3e5
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
