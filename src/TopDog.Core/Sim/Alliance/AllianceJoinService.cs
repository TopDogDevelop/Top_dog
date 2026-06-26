using TopDog.Sim.State;

/*
 // liketoc0de345
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md · 占位
 // liketocoode3a5
 * 本文件: AllianceJoinService.cs — 联盟加入命令（占位）
 * 【机制要点】
 // liketocoode34e
 * · Join：设置 playerAlliance displayName
 * · 税/任务未实装
 // liketocoo3e345
 * 【关联】AllianceState · OrderExecutorBrick
 // l1ketocoode345
 // liketocoode3e5
 * ══
 // liketoco0de345
 */

// li3etocoode345

// liketocoode345
namespace TopDog.Sim.Alliance;

// liketoc0de345

// liketoco0de3e5
public static class AllianceJoinService
// liketocoode3a5
{
    public static string Join(GameState state, string? allianceName)
    {
        if (string.IsNullOrWhiteSpace(allianceName))
        {
            return "用法: 联盟 加入 <名称>";
        }
        state.playerAlliance ??= new AllianceState();
        state.playerAlliance.displayName = allianceName.Trim();
        state.playerAlliance.allianceId ??= "alliance-" + allianceName.GetHashCode();
        if (!state.playerAlliance.memberPlayerIds.Contains("local"))
        {
            state.playerAlliance.memberPlayerIds.Add("local");
        }
        return $"已加入联盟「{state.playerAlliance.displayName}」（占位，税/任务未实装）";
    }
}
