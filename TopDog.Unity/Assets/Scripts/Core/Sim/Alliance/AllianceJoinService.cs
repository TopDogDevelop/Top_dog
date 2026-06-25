using TopDog.Sim.State;

namespace TopDog.Sim.Alliance;

public static class AllianceJoinService
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
