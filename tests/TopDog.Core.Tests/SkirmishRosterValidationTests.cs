using TopDog.Lobby;
using TopDog.Sim.Skirmish;

namespace TopDog.Core.Tests;

public sealed class SkirmishRosterValidationTests
{
    [Test]
    public void TryValidateLocalStart_RejectsRosterWithoutVisionAnchorTrait()
    {
        var lobby = new SkirmishLobbyState { scale = 10 };
        var human = new LobbyPlayer { local = true, displayName = "P", memberTemplateId = "template_1" };
        lobby.players.Add(human);
        lobby.rosterByPlayerId[human.playerId] =
        [
            new SkirmishRosterSlot
            {
                memberId = "m1",
                displayName = "无锚点",
                hullId = "hull_frigate_pineapple",
            },
        ];

        Assert.That(SkirmishRosterValidation.TryValidateLocalStart(lobby, out var error), Is.False);
        Assert.That(error, Is.EqualTo(SkirmishRosterValidation.MissingVisionAnchorMessage));
    }

    [Test]
    public void TryValidateLocalStart_AcceptsRosterWithPossessTraitRow()
    {
        var lobby = new SkirmishLobbyState { scale = 10, seed = 42 };
        var human = new LobbyPlayer { local = true, displayName = "P", memberTemplateId = "template_1" };
        lobby.players.Add(human);
        lobby.rosterByPlayerId[human.playerId] =
        [
            new SkirmishRosterSlot
            {
                memberTemplateId = "template_1",
                memberTemplateRowId = "template_1:10000001:01",
                memberId = "sk_loyal",
                displayName = "奥法凯",
                hullId = "hull_frigate_pineapple",
            },
        ];

        Assert.That(SkirmishRosterValidation.TryValidateLocalStart(lobby, out _), Is.True);
    }
}
