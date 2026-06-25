using TopDog.App;
using TopDog.Foundation.Io;
using TopDog.Lobby;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using System.Linq;

namespace TopDog.Tests;

[TestFixture]
public sealed class LobbyRandomBootstrapTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
    }

    [Test]
    public void PickRandomMemberTemplateId_IncludesPresetAndPureRandom()
    {
        var lobbyTemplates = ContentCatalog.ListLobbyMemberTemplates();
        var rng = new Random(42);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < 200; i++)
        {
            seen.Add(LobbyRandomBootstrap.PickRandomMemberTemplateId(lobbyTemplates, rng));
        }
        Assert.That(seen, Does.Contain(LobbyCatalogConstants.RandomMemberTemplateId));
        Assert.That(seen, Does.Contain("template_1"));
        Assert.That(seen, Does.Contain("template_vip_invest"));
    }

    [Test]
    public void RandomMemberStart_SpawnsThirtyProceduralMembersOnly()
    {
        var map = ContentCatalog.LoadMap(AppRoot.ContentMapDir());
        var lobby = new CustomLobbyState
        {
            mapPath = AppRoot.ContentMapDir(),
            mapDisplayName = "tutorial",
        };
        lobby.players.Add(new LobbyPlayer
        {
            local = true,
            host = true,
            kind = LobbyPlayerKind.HUMAN,
            displayName = "host",
            memberTemplateId = LobbyCatalogConstants.RandomMemberTemplateId,
            assetTemplateId = LobbyCatalogConstants.DefaultTestAssetId,
            spawnSolarSystemId = map.Project.systems[0].solarSystemId,
        });

        var core = CampaignBootstrap.CreateFromLobby(lobby);
        Assert.That(core.State.members.Count, Is.EqualTo(RecruitService.LobbyRandomStartMemberCount));
        var localLegion = LegionRegistry.Local(core.State)?.legionId;
        Assert.That(localLegion, Is.Not.Null.And.Not.Empty);
        Assert.That(MemberRosterSort.RosterForLegion(core.State, localLegion).Count, Is.EqualTo(RecruitService.LobbyRandomStartMemberCount));
        Assert.That(core.State.members.All(m => "procedural".Equals(m.source, StringComparison.Ordinal)), Is.True);
        Assert.That(core.State.members.Any(m => m.name == "奥法凯"), Is.False);
        Assert.That(core.State.flags.GetValueOrDefault("lobby.randomMembers"), Is.EqualTo("1"));
        Assert.That(core.State.flags.GetValueOrDefault("lobby.randomMemberCount"), Is.EqualTo("30"));
    }

    [Test]
    public void RandomMemberSlot_SpawnsProceduralAiRoster()
    {
        var map = ContentCatalog.LoadMap(AppRoot.ContentMapDir());
        var lobby = new CustomLobbyState
        {
            mapPath = AppRoot.ContentMapDir(),
            mapDisplayName = "tutorial",
        };
        lobby.players.Add(new LobbyPlayer
        {
            local = true,
            host = true,
            kind = LobbyPlayerKind.HUMAN,
            displayName = "host",
            memberTemplateId = "template_1",
            assetTemplateId = "assets_1",
            spawnSolarSystemId = map.Project.systems[0].solarSystemId,
        });
        lobby.players.Add(new LobbyPlayer
        {
            kind = LobbyPlayerKind.AI,
            displayName = "ai-random",
            memberTemplateId = LobbyCatalogConstants.RandomMemberTemplateId,
            spawnSolarSystemId = map.Project.systems[0].solarSystemId,
        });

        var core = CampaignBootstrap.CreateFromLobby(lobby);
        Assert.That(core.State.members.Count, Is.EqualTo(80));
        Assert.That(core.State.flags.GetValueOrDefault("lobby.randomMembers"), Is.EqualTo("1"));
        Assert.That(core.State.members.Any(m => m.isAi && m.source != "preset"), Is.True);
        Assert.That(core.State.members.First(m => m.name == "奥法凯").isPlayer, Is.True);
    }
}
