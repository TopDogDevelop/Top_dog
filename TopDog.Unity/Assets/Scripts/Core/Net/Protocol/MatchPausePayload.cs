namespace TopDog.Net.Protocol;

/// <summary>LAN match pause broadcast (NETWORK.md §暂停同步).</summary>
public sealed class MatchPausePayload
{
    public bool paused;
    public string initiatorId = "";
    public string initiatorName = "";
    /// <summary><c>human</c> or <c>ai</c> — Host rejects AI-initiated pause.</summary>
    public string initiatorKind = "human";
}
