using System.Text;
using System.Text.RegularExpressions;

namespace TopDog.Content.Banter;

public enum BanterMarkupRunKind
{
    Text,
    Emote,
}

public sealed class BanterMarkupRun
{
    public BanterMarkupRunKind Kind;
    public string Text = "";
    public int EmoteId;
}

public sealed class BanterParsedMarkup
{
    public int? ColorId;
    public List<BanterMarkupRun> Runs = new();
}

/// <summary>解析 CSV text 内 #color /emote 行内标记。</summary>
public static class BanterInlineMarkupParser
{
    private static readonly Regex LeadingColor = new(@"^#(\d+)", RegexOptions.Compiled);
    private static readonly Regex Token = new(@"^/(\d+)", RegexOptions.Compiled);

    public static BanterParsedMarkup Parse(string? raw)
    {
        var result = new BanterParsedMarkup();
        if (string.IsNullOrEmpty(raw))
        {
            return result;
        }

        var rest = raw;
        var colorMatch = LeadingColor.Match(rest);
        if (colorMatch.Success)
        {
            if (int.TryParse(colorMatch.Groups[1].Value, out var colorId))
            {
                result.ColorId = colorId;
            }

            rest = rest.Substring(colorMatch.Length);
        }

        var textBuf = new StringBuilder();
        for (var i = 0; i < rest.Length;)
        {
            var slice = rest.Substring(i);
            var emoteMatch = Token.Match(slice);
            if (emoteMatch.Success && emoteMatch.Index == 0 && emoteMatch.Value.StartsWith('/'))
            {
                FlushText(result, textBuf);
                if (int.TryParse(emoteMatch.Groups[1].Value, out var emoteId))
                {
                    result.Runs.Add(new BanterMarkupRun { Kind = BanterMarkupRunKind.Emote, EmoteId = emoteId });
                }

                i += emoteMatch.Length;
                continue;
            }

            textBuf.Append(rest[i]);
            i++;
        }

        FlushText(result, textBuf);
        return result;
    }

    public static string StripMarkupForValidation(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return "";
        }

        var parsed = Parse(raw);
        var sb = new StringBuilder();
        foreach (var run in parsed.Runs)
        {
            if (run.Kind == BanterMarkupRunKind.Text)
            {
                sb.Append(run.Text);
            }
        }

        return sb.ToString();
    }

    private static void FlushText(BanterParsedMarkup result, StringBuilder textBuf)
    {
        if (textBuf.Length == 0)
        {
            return;
        }

        result.Runs.Add(new BanterMarkupRun
        {
            Kind = BanterMarkupRunKind.Text,
            Text = textBuf.ToString(),
        });
        textBuf.Clear();
    }
}
