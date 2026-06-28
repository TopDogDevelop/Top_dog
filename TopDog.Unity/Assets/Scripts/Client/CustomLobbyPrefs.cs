using System.Text;
using TopDog.Lobby;
using UnityEngine;

namespace TopDog.Client;

/// <summary>本机自定义战役大厅上次开局选项（PlayerPrefs）。</summary>
public static class CustomLobbyPrefs
{
    private const string Prefix = "topdog.custom_lobby.";

    public static void Save(CustomLobbyState lobby, LobbyPlayer? local)
    {
        if (local == null)
        {
            return;
        }

        PlayerPrefs.SetString(Prefix + "map_path", lobby.mapPath ?? "");
        PlayerPrefs.SetInt(Prefix + "procedural_map", lobby.proceduralMap ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "procedural_system_count", lobby.proceduralSystemCount);
        PlayerPrefs.SetFloat(Prefix + "procedural_bridge_density", lobby.proceduralBridgeDensity);
        PlayerPrefs.SetString(Prefix + "member_template", local.memberTemplateId ?? "");
        PlayerPrefs.SetString(Prefix + "asset_template", local.assetTemplateId ?? "");
        PlayerPrefs.SetString(Prefix + "spawn_system", local.spawnSolarSystemId ?? "");

        var sb = new StringBuilder();
        foreach (var p in lobby.players)
        {
            sb.Append((int)p.kind).Append('|')
                .Append(p.spawnSolarSystemId ?? "").Append('|')
                .Append(p.memberTemplateId ?? "").Append('|')
                .Append(p.assetTemplateId ?? "").Append('|')
                .Append(p.displayName ?? "").Append(';');
        }

        PlayerPrefs.SetString(Prefix + "roster", sb.ToString());
        var aiCount = 0;
        foreach (var p in lobby.players)
        {
            if (p.kind == LobbyPlayerKind.AI)
            {
                aiCount++;
            }
        }

        PlayerPrefs.SetInt(Prefix + "ai_count", aiCount);
        PlayerPrefs.Save();
    }

    public static void Apply(CustomLobbyState lobby, LobbyPlayer local)
    {
        if (!PlayerPrefs.HasKey(Prefix + "map_path"))
        {
            return;
        }

        var procedural = PlayerPrefs.GetInt(Prefix + "procedural_map", 0) == 1;
        var mapPath = PlayerPrefs.GetString(Prefix + "map_path", "");
        lobby.proceduralMap = procedural;
        if (!string.IsNullOrWhiteSpace(mapPath))
        {
            lobby.mapPath = mapPath;
        }

        var sysCount = PlayerPrefs.GetInt(Prefix + "procedural_system_count", lobby.proceduralSystemCount);
        if (sysCount > 0)
        {
            lobby.proceduralSystemCount = sysCount;
        }

        lobby.proceduralBridgeDensity = PlayerPrefs.GetFloat(
            Prefix + "procedural_bridge_density",
            lobby.proceduralBridgeDensity);

        var member = PlayerPrefs.GetString(Prefix + "member_template", "");
        if (!string.IsNullOrWhiteSpace(member))
        {
            local.memberTemplateId = member;
        }

        var asset = PlayerPrefs.GetString(Prefix + "asset_template", "");
        if (!string.IsNullOrWhiteSpace(asset))
        {
            local.assetTemplateId = asset;
        }

        var spawn = PlayerPrefs.GetString(Prefix + "spawn_system", "");
        if (!string.IsNullOrWhiteSpace(spawn))
        {
            local.spawnSolarSystemId = spawn;
        }
    }

    /// <summary>恢复上次保存的人机数量与各玩家出生/模版（在 SeedLocalPlayer 之后调用）。</summary>
    public static void RestoreRoster(CustomLobbyState lobby)
    {
        var roster = PlayerPrefs.GetString(Prefix + "roster", "");
        if (string.IsNullOrWhiteSpace(roster))
        {
            return;
        }

        var local = lobby.FindLocal();
        if (local == null)
        {
            return;
        }

        var slots = roster.Split(';', System.StringSplitOptions.RemoveEmptyEntries);
        if (slots.Length == 0)
        {
            return;
        }

        ApplySlot(local, slots[0]);
        for (var i = 1; i < slots.Length && lobby.players.Count < lobby.maxPlayers; i++)
        {
            if (!TryParseSlot(slots[i], out var kind, out var spawn, out var member, out var asset, out var name))
            {
                continue;
            }

            if (kind != LobbyPlayerKind.AI)
            {
                continue;
            }

            lobby.players.Add(new LobbyPlayer
            {
                kind = LobbyPlayerKind.AI,
                displayName = string.IsNullOrWhiteSpace(name) ? "人机" : name,
                spawnSolarSystemId = string.IsNullOrWhiteSpace(spawn) ? null : spawn,
                memberTemplateId = string.IsNullOrWhiteSpace(member) ? local.memberTemplateId : member,
                assetTemplateId = string.IsNullOrWhiteSpace(asset) ? local.assetTemplateId : asset,
            });
        }
    }

    private static void ApplySlot(
        LobbyPlayer player,
        string slot)
    {
        if (!TryParseSlot(slot, out _, out var spawn, out var member, out var asset, out var name))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(spawn))
        {
            player.spawnSolarSystemId = spawn;
        }

        if (!string.IsNullOrWhiteSpace(member))
        {
            player.memberTemplateId = member;
        }

        if (!string.IsNullOrWhiteSpace(asset))
        {
            player.assetTemplateId = asset;
        }

        if (!player.local && !string.IsNullOrWhiteSpace(name))
        {
            player.displayName = name;
        }
    }

    private static bool TryParseSlot(
        string slot,
        out LobbyPlayerKind kind,
        out string spawn,
        out string member,
        out string asset,
        out string name)
    {
        kind = LobbyPlayerKind.HUMAN;
        spawn = "";
        member = "";
        asset = "";
        name = "";
        var parts = slot.Split('|');
        if (parts.Length < 5)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var kindInt)
            || !System.Enum.IsDefined(typeof(LobbyPlayerKind), kindInt))
        {
            return false;
        }

        kind = (LobbyPlayerKind)kindInt;
        spawn = parts[1];
        member = parts[2];
        asset = parts[3];
        name = parts[4];
        return true;
    }
}
