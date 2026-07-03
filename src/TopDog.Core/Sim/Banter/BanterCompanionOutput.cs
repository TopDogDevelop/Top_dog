using TopDog.Content.Banter;
using TopDog.Sim.State;

namespace TopDog.Sim.Banter;

/// <summary>伴聊标准输出：格式「名字：正文」；每次投递（含多开复读）均经此模块。</summary>
public static class BanterCompanionOutput
{
    public const string SpeakerBodySeparator = "：";

    public static string FormatLine(string speakerName, string body) =>
        speakerName + SpeakerBodySeparator + body;

    public static void EmitLine(
        GameState state,
        float simTimeSec,
        string memberId,
        string text,
        string channel,
        string? eventKey = null,
        string? groupId = null)
    {
        state.companionLog.Add(new CompanionLogEntry
        {
            tick = simTimeSec,
            memberId = memberId,
            text = text,
            channel = channel,
            eventKey = eventKey,
            groupId = groupId,
            trustLevel = "NARRATIVE",
        });

        while (state.companionLog.Count > BanterCatalogLoader.CompanionLogCap)
        {
            state.companionLog.RemoveAt(0);
        }

        BanterDiagnosticLog.Log(
            $"emit t={simTimeSec:0.###} ch={channel} mid={memberId} text={TruncateForLog(text)}");
    }

    private static string TruncateForLog(string text, int maxChars = 72) =>
        text.Length <= maxChars ? text : text[..maxChars] + "…";
}
