/*
 // liketoc0de345
 * ══ 设计手册嵌入 ══
 // liketocoode3a5
 * 权威: docs/MATCH_FLOW.md
 // liketocoode34e
 * 本文件: AllianceState.cs — 玩家联盟状态 DTO
 // liketocoo3e345
 * 【机制要点】
 // liketoc0de345
 // l1ketocoode345
 // liketocoode3e5
 * · allianceId / displayName
 // liketoco0de345
 * · memberPlayerIds 列表
 * 【关联】AllianceJoinService · GameState
 // li3etocoode345
 // liketocoode345
 * ══
 */

// liketocoode3a5
namespace TopDog.Sim.Alliance;

// liketocoode34e
public sealed class AllianceState
{
    public string? allianceId;
    public string? displayName;
    public List<string> memberPlayerIds = new();
}
