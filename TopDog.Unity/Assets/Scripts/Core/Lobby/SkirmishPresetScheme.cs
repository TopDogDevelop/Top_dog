namespace TopDog.Lobby;

/// <summary>从配置保存槽提取的可应用方案（名册 + 规模 + 模式）。</summary>
public sealed class SkirmishPresetScheme
{
    public int Scale { get; init; }
    public SkirmishLobbyMode Mode { get; init; }
    public List<SkirmishPresetRosterLine> RosterLines { get; init; } = new();

    public static SkirmishPresetScheme? Extract(SkirmishLobbyState? lobby)
    {
        if (lobby == null)
        {
            return null;
        }

        var local = lobby.FindLocal();
        if (local == null)
        {
            return null;
        }

        var lines = new List<SkirmishPresetRosterLine>();
        if (lobby.rosterByPlayerId.TryGetValue(local.playerId, out var roster))
        {
            foreach (var slot in roster)
            {
                lines.Add(new SkirmishPresetRosterLine
                {
                    DisplayName = slot.displayName ?? slot.memberId ?? "?",
                    HullId = slot.hullId,
                    MemberTemplateRowId = slot.memberTemplateRowId,
                    MemberTemplateId = slot.memberTemplateId,
                    FittedModules = slot.fittedModules.ToDictionary(kv => kv.Key, kv => kv.Value ?? ""),
                });
            }
        }

        return new SkirmishPresetScheme
        {
            Scale = lobby.scale,
            Mode = lobby.mode,
            RosterLines = lines,
        };
    }

    public static string FormatSummary(SkirmishPresetScheme scheme)
    {
        var mode = scheme.Mode == SkirmishLobbyMode.VsHuman ? "匹配真人" : "匹配人机";
        var roster = scheme.RosterLines.Count == 0
            ? "（空名册）"
            : string.Join("、", scheme.RosterLines.ConvertAll(l => l.DisplayName));
        return $"规模 {scheme.Scale} · {mode} · {scheme.RosterLines.Count} 人：{roster}";
    }

    /// <summary>将方案应用到当前大厅（仅本机玩家名册与规模/模式）。</summary>
    public static void ApplyToLobby(SkirmishLobbyState lobby, SkirmishPresetScheme scheme)
    {
        var local = lobby.FindLocal();
        if (local == null)
        {
            return;
        }

        lobby.scale = scheme.Scale;
        lobby.mode = scheme.Mode;
        var roster = new List<SkirmishRosterSlot>();
        foreach (var line in scheme.RosterLines)
        {
            roster.Add(new SkirmishRosterSlot
            {
                memberId = "sk_" + Guid.NewGuid().ToString("N")[..8],
                displayName = line.DisplayName,
                hullId = line.HullId,
                memberTemplateId = line.MemberTemplateId,
                memberTemplateRowId = line.MemberTemplateRowId,
                fittedModules = line.FittedModules.ToDictionary(kv => kv.Key, kv => kv.Value ?? ""),
            });
        }

        lobby.rosterByPlayerId[local.playerId] = roster;
    }
}

public sealed class SkirmishPresetRosterLine
{
    public string DisplayName { get; init; } = "?";
    public string? HullId { get; init; }
    public string? MemberTemplateId { get; init; }
    public string? MemberTemplateRowId { get; init; }
    public Dictionary<string, string> FittedModules { get; init; } = new();
}
