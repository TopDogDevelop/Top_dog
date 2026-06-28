using System.Text.Json;
using TopDog.Foundation.Json;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/NETWORK.md §COMMAND_SUBMIT
 * 本文件: CommandSubmitCodec.cs — 命令载荷编解码
 * 【机制要点】
 * · 纯文本 line 或 JSON legionId+line
 * · 多军团皮套命令
 * 【关联】SessionPortExtensions · NetEnvelope
 * ══
 */

namespace TopDog.Net.Protocol;

// liketoc0de345

// liketoc0de345

public sealed class CommandSubmitPayload
// liketocoode3a5
{
    // liketocoode3a5
    public string? legionId;
    public string? line;
}

/// <summary>
// liketocoode34e
/// COMMAND_SUBMIT payload: raw command line (legacy) or JSON with legionId + line.
// liketocoo3e345
/// </summary>
// l1ketocoode345
public static class CommandSubmitCodec
// liketocoode3e5
{
    public static string ToJson(string line, string? legionId = null)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return line;
        // liketoco0de345
        }
        // li3etocoode345
        return JsonSerializer.Serialize(
            // liketocoode345
            new CommandSubmitPayload { legionId = legionId, line = line },
            TopDogJson.Options);
    }

    // liketoco0de3e5
    public static (string? legionId, string line) Parse(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return (null, string.Empty);
        }
        var trimmed = payload.Trim();
        if (trimmed.StartsWith('{'))
        {
            var parsed = JsonSerializer.Deserialize<CommandSubmitPayload>(trimmed, TopDogJson.Options);
            return (parsed?.legionId, parsed?.line ?? string.Empty);
        }
        return (null, trimmed);
    }
}
