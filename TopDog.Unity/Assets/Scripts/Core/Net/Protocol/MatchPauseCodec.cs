using System.Text.Json;
using TopDog.Foundation.Json;

namespace TopDog.Net.Protocol;

public static class MatchPauseCodec
{
    public static string ToJson(MatchPausePayload payload) =>
        JsonSerializer.Serialize(payload, TopDogJson.Options);

    public static MatchPausePayload? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }
        return JsonSerializer.Deserialize<MatchPausePayload>(json, TopDogJson.Options);
    }

    public static bool IsHumanInitiator(MatchPausePayload payload) =>
        !string.Equals(payload.initiatorKind, "ai", StringComparison.OrdinalIgnoreCase);
}
