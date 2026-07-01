using TopDog.Lobby;

namespace TopDog.App;

/// <summary>开发/冒烟用约战快速开局（完整大厅 UI 见 SkirmishLobbyController）。</summary>
public static class SkirmishQuickStart
{
    public static SkirmishLobbyState CreateDefaultVsAi(int scale = 10)
    {
        var lobby = new SkirmishLobbyState { scale = scale, seed = 42 };
        var human = new LobbyPlayer
        {
            local = true,
            host = true,
            displayName = "玩家",
            memberTemplateId = "template_1",
        };
        var ai = new LobbyPlayer
        {
            kind = LobbyPlayerKind.AI,
            displayName = "人机",
            memberTemplateId = "template_1",
        };
        lobby.players.Add(human);
        lobby.players.Add(ai);

        lobby.rosterByPlayerId[human.playerId] = new List<SkirmishRosterSlot>
        {
            new()
            {
                memberId = "sk_m1",
                displayName = "团员 1",
                hullId = "hull_frigate_pineapple",
            },
        };
        lobby.rosterByPlayerId[ai.playerId] = new List<SkirmishRosterSlot>
        {
            new()
            {
                memberId = "sk_ai1",
                displayName = "AI 1",
                hullId = "hull_frigate_pineapple",
            },
        };
        return lobby;
    }
}
