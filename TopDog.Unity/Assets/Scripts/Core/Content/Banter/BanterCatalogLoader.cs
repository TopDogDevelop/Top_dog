using TopDog.Foundation.Io;

namespace TopDog.Content.Banter;

public static class BanterCatalogLoader
{
    public const int CompanionLogCap = 200;

    public static BanterCatalog LoadDefault()
    {
        var dir = AppRoot.BanterDir();
        if (!Directory.Exists(dir))
        {
            return BanterCatalog.Empty;
        }

        var catalog = new BanterCatalog();
        LoadPalette(Path.Combine(dir, "color_palette.csv"), catalog);
        LoadEmotes(Path.Combine(dir, "emote_catalog.csv"), catalog);
        LoadMemberConfig(Path.Combine(dir, "member_banter_config.csv"), catalog);
        LoadReactive(Path.Combine(dir, "reactive_common.csv"), catalog.ReactiveCommon, personal: false);
        LoadReactive(Path.Combine(dir, "reactive_personal.csv"), catalog.ReactivePersonal, personal: true);
        LoadIdle(Path.Combine(dir, "idle_common.csv"), catalog.IdleCommon, personal: false);
        LoadIdle(Path.Combine(dir, "idle_personal.csv"), catalog.IdlePersonal, personal: true);
        BuildIdleGroups(catalog);
        return catalog;
    }

    private static void BuildIdleGroups(BanterCatalog catalog)
    {
        catalog.IdleGroups.Clear();
        foreach (var line in catalog.IdleCommon.Concat(catalog.IdlePersonal))
        {
            if (!catalog.IdleGroups.TryGetValue(line.GroupId, out var list))
            {
                list = new List<IdleBanterLine>();
                catalog.IdleGroups[line.GroupId] = list;
            }

            list.Add(line);
        }

        foreach (var kv in catalog.IdleGroups)
        {
            kv.Value.Sort((a, b) => a.Seq.CompareTo(b.Seq));
        }
    }

    private static void LoadPalette(string path, BanterCatalog catalog)
    {
        foreach (var row in ReadApprovedRows(path))
        {
            if (!int.TryParse(Get(row, "colorId"), out var id))
            {
                continue;
            }

            var hex = Get(row, "hex");
            if (!string.IsNullOrWhiteSpace(hex))
            {
                catalog.Colors[id] = hex.Trim();
            }
        }
    }

    private static void LoadEmotes(string path, BanterCatalog catalog)
    {
        foreach (var row in ReadApprovedRows(path))
        {
            if (!int.TryParse(Get(row, "emoteId"), out var id))
            {
                continue;
            }

            var sprite = Get(row, "spriteRef");
            if (!string.IsNullOrWhiteSpace(sprite))
            {
                catalog.EmoteSpriteRefs[id] = sprite.Trim();
            }
        }
    }

    private static void LoadMemberConfig(string path, BanterCatalog catalog)
    {
        foreach (var row in ReadApprovedRows(path, requireApproved: false))
        {
            var memberId = Get(row, "memberId");
            if (string.IsNullOrWhiteSpace(memberId))
            {
                continue;
            }

            catalog.MemberConfig[memberId.Trim()] = new MemberBanterConfigRow
            {
                MemberId = memberId.Trim(),
                ReactiveUseCommon = ParseBool(Get(row, "reactiveUseCommon"), true),
                IdleBanterUseCommon = ParseBool(Get(row, "idleBanterUseCommon"), true),
            };
        }
    }

    private static void LoadReactive(string path, List<ReactiveBanterLine> into, bool personal)
    {
        foreach (var row in ReadApprovedRows(path))
        {
            var eventKey = Get(row, "eventKey");
            var text = Get(row, "text");
            if (string.IsNullOrWhiteSpace(eventKey) || string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var memberId = Get(row, "memberId");
            if (personal && string.IsNullOrWhiteSpace(memberId))
            {
                continue;
            }

            into.Add(new ReactiveBanterLine
            {
                LineId = Get(row, "lineId") ?? "",
                MemberId = string.IsNullOrWhiteSpace(memberId) ? "*" : memberId.Trim(),
                EventKey = eventKey.Trim(),
                Text = text,
                Weight = ParseInt(Get(row, "weight"), 1),
            });
        }
    }

    private static void LoadIdle(string path, List<IdleBanterLine> into, bool personal)
    {
        foreach (var row in ReadApprovedRows(path))
        {
            var groupId = Get(row, "groupId");
            var text = Get(row, "text");
            if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var memberId = Get(row, "memberId");
            if (personal && string.IsNullOrWhiteSpace(memberId))
            {
                continue;
            }

            into.Add(new IdleBanterLine
            {
                GroupId = groupId.Trim(),
                Seq = ParseInt(Get(row, "seq"), 1),
                MemberId = string.IsNullOrWhiteSpace(memberId) ? "*" : memberId.Trim(),
                Text = text,
                SplitMsgId = NullIfBlank(Get(row, "splitMsgId")),
            });
        }
    }

    private static IEnumerable<Dictionary<string, string>> ReadApprovedRows(
        string path,
        bool requireApproved = true)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        var lines = File.ReadAllLines(path);
        var keyRow = FindHeaderRow(lines, "reviewStatus");
        if (keyRow < 0)
        {
            keyRow = FindHeaderRow(lines, "lineId");
        }

        if (keyRow < 0)
        {
            keyRow = FindHeaderRow(lines, "groupId");
        }

        if (keyRow < 0 || keyRow + 1 >= lines.Length)
        {
            yield break;
        }

        var idx = IndexColumns(SplitCsv(lines[keyRow]));
        for (var r = keyRow + 1; r < lines.Length; r++)
        {
            var line = lines[r].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var cols = SplitCsv(line);
            var row = RowDict(cols, idx);
            if (requireApproved)
            {
                var status = Get(row, "reviewStatus");
                if (!string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            yield return row;
        }
    }

    private static Dictionary<string, string> RowDict(string[] cols, Dictionary<string, int> idx)
    {
        var row = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in idx)
        {
            row[kv.Key] = kv.Value < cols.Length ? cols[kv.Value] : "";
        }

        return row;
    }

    private static int FindHeaderRow(string[] lines, string keyCol)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var cols = SplitCsv(lines[i]);
            foreach (var c in cols)
            {
                if (string.Equals(NormalizeColumn(c), keyCol, StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static Dictionary<string, int> IndexColumns(string[] cols)
    {
        var m = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < cols.Length; i++)
        {
            m[NormalizeColumn(cols[i])] = i;
        }

        return m;
    }

    private static string NormalizeColumn(string? col) => col?.Trim() ?? "";

    private static string? Get(Dictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var v) ? v : null;

    private static string[] SplitCsv(string line) => line.Split(',', StringSplitOptions.None);

    private static int ParseInt(string? s, int defaultValue) =>
        int.TryParse(s?.Trim(), out var v) ? v : defaultValue;

    private static bool ParseBool(string? s, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return defaultValue;
        }

        var t = s.Trim();
        if (bool.TryParse(t, out var b))
        {
            return b;
        }

        return t is "1" or "yes" or "Y" or "是";
    }

    private static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
