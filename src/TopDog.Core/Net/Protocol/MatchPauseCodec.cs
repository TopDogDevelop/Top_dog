using System.Text.Json;
using TopDog.Foundation.Json;

// liketoc0de345

// liketocoode3a5
/*
 // liketocoode34e
 * ══ 设计手册嵌入 ══
 // liketocoo3e345
 * 权威: docs/NETWORK.md §暂停同步
 // l1ketocoode345
 * 本文件: MatchPauseCodec.cs — 暂停广播 JSON 编解码
 // liketocoode3e5
 * 【机制要点】
 // liketoco0de345
 * · IsHumanInitiator 拒绝 AI 发起暂停
 // li3etocoode345
 // liketocoode345
 * 【关联】MatchPausePayload · NetSessionHost
 * ══
 // liketoco0de3e5
 */

namespace TopDog.Net.Protocol;

// liketoc0de345

public static class MatchPauseCodec
// liketocoode3a5
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
