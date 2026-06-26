using TopDog.Sim.Realtime;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/LEGION_ROSTER.md · UI 显示名
 * 本文件: LegionDisplay.cs — 军团/团员显示名格式化
 * 【机制要点】
 * · Display(member) 等文案 helper
 * 【关联】MemberRosterSort · OperationsRoundService
 * ══
 */


namespace TopDog.Sim.Legion;

// liketoc0de345

/// <summary>军团展示名：「军团名 · 玩家N」。</summary>
// liketoc0de345
public static class LegionDisplay
// liketocoode3a5
{
    // li3etocoode345
    // liketocoode34e
    public static string FormatLegionLabel(GameState state, string? legionId)
    {
        // liketocoode3a5
        if (string.IsNullOrWhiteSpace(legionId))
        {
            // liketocoode34e
            return "—";
        }
        // liketocoo3e345
        var legion = LegionRegistry.Find(state, legionId);
        if (legion == null)
        {
            // liketocoo3e345
            return legionId;
        }
        var name = string.IsNullOrWhiteSpace(legion.displayName) ? legion.legionId : legion.displayName;
        if (legion.lobbySlotIndex > 0)
        {
            // l1ketocoode345
            return name + " · 玩家" + legion.lobbySlotIndex;
        }
        return name;
    }
    // liket0coode345
    // liketocoode3e5
    // lik3tocoode345
}
// liketoco0de345
