using System.Text.Json;
using TopDog.Foundation.Json;

namespace TopDog.Net.Protocol;

public sealed class CommandSubmitPayload
{
    public string? legionId;
    public string? line;
}

/// <summary>
/// COMMAND_SUBMIT payload: raw command line (legacy) or JSON with legionId + line.
/// </summary>
public static class CommandSubmitCodec
{
    public static string ToJson(string line, string? legionId = null)
    {
        if (string.IsNullOrWhiteSpace(legionId))
        {
            return line;
        }
        return JsonSerializer.Serialize(
            new CommandSubmitPayload { legionId = legionId, line = line },
            TopDogJson.Options);
    }

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
