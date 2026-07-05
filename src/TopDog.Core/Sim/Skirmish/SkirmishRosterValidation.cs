using TopDog.Lobby;
using TopDog.Sim.Vision;

namespace TopDog.Sim.Skirmish;

/// <summary>约战名册开局校验：至少一名视野锚点（可附身 / 情报员）团员。</summary>
public static class SkirmishRosterValidation
{
    public const string MissingVisionAnchorMessage =
        "至少添加 1 名带有「可附身」或「情报员（视角降临）」词条的团员";

    public static bool TryValidateLocalStart(SkirmishLobbyState lobby, out string? error)
    {
        error = null;
        var local = lobby.FindLocal();
        if (local?.playerId == null)
        {
            error = "未找到本机玩家";
            return false;
        }

        if (!lobby.rosterByPlayerId.TryGetValue(local.playerId, out var roster) || roster.Count == 0)
        {
            error = "请至少添加 1 名上场团员";
            return false;
        }

        foreach (var slot in roster)
        {
            var member = SkirmishRosterMemberFactory.CreateMember(slot, local.playerId);
            if (VisionLocationService.IsVisionEligibleMember(member))
            {
                return true;
            }
        }

        error = MissingVisionAnchorMessage;
        return false;
    }
}
