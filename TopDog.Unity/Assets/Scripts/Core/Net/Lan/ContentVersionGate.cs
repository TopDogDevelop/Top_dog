using System;
using System.Globalization;
using System.Text.RegularExpressions;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VERSION.md · docs/RELEASE_AND_HOTUPDATE.md §2 · docs/ONLINE_UPDATE.md
 * 本文件: ContentVersionGate.cs — 三位版号 YYYYMM.D.N（无字母；联机同版 + 热更排序）
 * 【机制要点】
 * · 格式例 202607.14.1 = 2026-07-14 当天第 1 版；N=1～999；不含 v/字母
 * · Matches：字符串全等（联机）
 * · Compare：按 (年,月,日,N) 排序；热更仅远端更大才下载
 * · 商店 versionCode 等内部递增不纳入本门禁
 * 【关联】LanProtocol · OnlineUpdateClient · MainMenuController（软件版同形）
 * ══
 */

namespace TopDog.Net.Lan;

/// <summary>Shared version string for hot-update, LAN matchmaking, and shell display alignment.</summary>
public static class ContentVersionGate
{
    /// <summary>Shell / content baseline in <c>YYYYMM.D.N</c> form (see docs/VERSION.md).</summary>
    public const string Baseline = "202607.14.6";

    private static readonly Regex VersionPattern = new(
        @"^(?<ym>\d{6})\.(?<d>\d{1,2})\.(?<n>\d{1,3})$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Current { get; private set; } = Baseline;

    public static void Set(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return;
        }

        Current = version.Trim();
    }

    public static bool Matches(string? other) =>
        string.Equals(Current, other?.Trim(), StringComparison.Ordinal);

    /// <summary>
    /// Parses <c>YYYYMM.D.N</c> (no letters). N is 1–3 digits (1–999).
    /// </summary>
    public static bool TryParse(string? version, out int year, out int month, out int day, out int revision)
    {
        year = month = day = revision = 0;
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var m = VersionPattern.Match(version.Trim());
        if (!m.Success)
        {
            return false;
        }

        if (!int.TryParse(m.Groups["ym"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var ym)
            || !int.TryParse(m.Groups["d"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out day)
            || !int.TryParse(m.Groups["n"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out revision))
        {
            return false;
        }

        year = ym / 100;
        month = ym % 100;
        if (year is < 1000 or > 9999
            || month is < 1 or > 12
            || day is < 1 or > 31
            || revision is < 1 or > 999)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Compares two version strings. When both parse, orders by (y,m,d,n).
    /// Otherwise falls back to ordinal string compare of trimmed values.
    /// </summary>
    public static int Compare(string? a, string? b)
    {
        var left = a?.Trim() ?? "";
        var right = b?.Trim() ?? "";
        if (TryParse(left, out var ay, out var am, out var ad, out var an)
            && TryParse(right, out var by, out var bm, out var bd, out var bn))
        {
            var c = ay.CompareTo(by);
            if (c != 0)
            {
                return c;
            }

            c = am.CompareTo(bm);
            if (c != 0)
            {
                return c;
            }

            c = ad.CompareTo(bd);
            if (c != 0)
            {
                return c;
            }

            return an.CompareTo(bn);
        }

        return string.CompareOrdinal(left, right);
    }
}
