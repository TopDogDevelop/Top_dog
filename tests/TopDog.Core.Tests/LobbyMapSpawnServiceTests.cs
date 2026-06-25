using TopDog.App;
using TopDog.Content.Map;
using TopDog.Foundation.Io;
using TopDog.Lobby;

namespace TopDog.Tests;

public sealed class LobbyMapSpawnServiceTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
    }

    [Test]
    public void ProceduralLobby_StartsWithValidSpawns()
    {
        var lobby = new CustomLobbyState
        {
            proceduralMap = true,
            mapPath = MapCatalogEntry.ProceduralMapId,
            proceduralSystemCount = 16,
            proceduralBridgeDensity = 1f,
            proceduralSeed = 4242,
        };
        lobby.players.Add(new LobbyPlayer
        {
            local = true,
            host = true,
            kind = LobbyPlayerKind.HUMAN,
            displayName = "host",
            memberTemplateId = "template_1",
            assetTemplateId = LobbyCatalogConstants.DefaultTestAssetId,
            spawnSolarSystemId = "sys_hub",
        });
        lobby.players.Add(new LobbyPlayer
        {
            kind = LobbyPlayerKind.AI,
            displayName = "ai",
            memberTemplateId = LobbyCatalogConstants.RandomMemberTemplateId,
            spawnSolarSystemId = "sys_mine",
        });

        var core = CampaignBootstrap.CreateFromLobby(lobby);

        Assert.That(core.State.map?.Project.systems.Count, Is.EqualTo(16));
        Assert.That(lobby.players[0].spawnSolarSystemId, Does.StartWith("sys_rand_"));
        Assert.That(lobby.players[1].spawnSolarSystemId, Does.StartWith("sys_rand_"));
        Assert.That(core.State.currentSolarSystemId, Does.StartWith("sys_rand_"));
    }

    [Test]
    public void RoundBridgeDensity_SnapsToStep()
    {
        Assert.That(ProceduralMapOptions.RoundBridgeDensity(0.27f), Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(ProceduralMapOptions.RoundBridgeDensity(1.07f), Is.EqualTo(1.05f).Within(0.001f));
    }
}
