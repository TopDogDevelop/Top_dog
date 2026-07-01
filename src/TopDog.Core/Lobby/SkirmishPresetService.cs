using System.Text.Json;
using TopDog.Foundation.Json;

namespace TopDog.Lobby;

public static class SkirmishPresetService
{
    public const int PresetCount = 10;

    public static string Serialize(SkirmishLobbyState lobby) =>
        JsonSerializer.Serialize(lobby, TopDogJson.Options);

    public static SkirmishLobbyState? Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<SkirmishLobbyState>(json, TopDogJson.Options);
}
