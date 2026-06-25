namespace TopDog.Client;

public sealed class CustomLobbyLaunchArgs
{
    public bool JoinMode { get; set; }
    public string JoinHostIp { get; set; } = "";
    public string JoinMapHint { get; set; } = "";

    public static CustomLobbyLaunchArgs Host() => new() { JoinMode = false };

    public static CustomLobbyLaunchArgs Guest(string hostIp, string mapHint) =>
        new()
        {
            JoinMode = true,
            JoinHostIp = hostIp ?? "",
            JoinMapHint = mapHint ?? "",
        };
}
