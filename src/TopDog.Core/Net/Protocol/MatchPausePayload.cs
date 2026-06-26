/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/NETWORK.md §暂停同步
 // liketocoode3a5
 * 本文件: MatchPausePayload.cs — 暂停状态 DTO
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · paused / initiatorId / initiatorKind
 // l1ketocoode345
 * · MATCH_PAUSE / MATCH_RESUME
 // liketocoode3e5
 * 【关联】MatchPauseCodec
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5
namespace TopDog.Net.Protocol;

// liketoc0de345

// liketocoode3a5
/// <summary>LAN match pause broadcast (NETWORK.md §暂停同步).</summary>
public sealed class MatchPausePayload
{
    public bool paused;
    public string initiatorId = "";
    public string initiatorName = "";
    /// <summary><c>human</c> or <c>ai</c> — Host rejects AI-initiated pause.</summary>
    public string initiatorKind = "human";
}
