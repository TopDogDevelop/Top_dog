using TopDog.App;
using TopDog.Content.Starting;
using TopDog.Foundation.Io;
using TopDog.Lobby;
using TopDog.Sim.Legion;
using TopDog.Sim.State;
using System.Linq;

namespace TopDog.Tests;

public sealed class CampaignBootstrapTemplateTests
{
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
    }

    [Test]
    public void Template1LoadsPresetMembersIntoCustomCampaign()
    {
        var map = ContentCatalog.LoadMap(AppRoot.ContentMapDir());
        var lobby = new CustomLobbyState
        {
            mapPath = AppRoot.ContentMapDir(),
            mapDisplayName = "tutorial",
        };

        var human = new LobbyPlayer
        {
            local = true,
            host = true,
            kind = LobbyPlayerKind.HUMAN,
            displayName = "test-host",
            memberTemplateId = "template_1",
            assetTemplateId = "assets_1",
            spawnSolarSystemId = map.Project.systems[0].solarSystemId,
        };
        lobby.players.Add(human);

        var ai = new LobbyPlayer
        {
            kind = LobbyPlayerKind.AI,
            displayName = "ai-1",
            memberTemplateId = "template_vip_invest",
            spawnSolarSystemId = map.Project.systems.Count > 1
                ? map.Project.systems[1].solarSystemId
                : map.Project.systems[0].solarSystemId,
        };
        lobby.players.Add(ai);

        var core = CampaignBootstrap.CreateFromLobby(lobby);
        Assert.That(core.State.members, Is.Not.Empty, "template_1 should import preset members");
        Assert.That(core.State.members.Count, Is.EqualTo(88));
        Assert.That(core.State.worldline.startingTemplateId, Is.EqualTo("template_1"));
        var leader = core.State.members.First(m => m.name == "奥法凯");
        Assert.That(leader.tonnageSpec["CARRIER"], Is.EqualTo(5));
        Assert.That(leader.tonnageSpec["DRONE"], Is.EqualTo(5));
        Assert.That(leader.isPlayer, Is.True);
        Assert.That(leader.isAi, Is.False);

        var sheep = core.State.members.First(m => m.name == "绵羊伸腿");
        Assert.That(sheep.isAi, Is.True);
        Assert.That(sheep.traitIds, Does.Contain("trait_board_favor"));

        var ashBei = core.State.members.First(m => m.name == "狈头军师" && m.isPlayer);
        var vipBei = core.State.members.First(m => m.name == "狈头军师" && m.isAi);
        Assert.That(ashBei.traitIds, Does.Contain("trait_fool_loyal"));
        Assert.That(vipBei.traitIds, Does.Not.Contain("trait_fool_loyal"));

        Assert.That(core.State.legions.Count, Is.EqualTo(2));
        Assert.That(core.State.members.All(m => !string.IsNullOrWhiteSpace(m.legionId)), Is.True);
        var legionForts = core.State.buildings
            .Where(b => b.buildingType == "LEGION_FORTRESS")
            .ToList();
        Assert.That(legionForts.Count, Is.EqualTo(2));
        Assert.That(legionForts.Select(b => b.legionId).Distinct().Count(), Is.EqualTo(2));
    }

    [Test]
    public void SameMemberTemplate_BothLegionsKeepDistinctRosters()
    {
        var map = ContentCatalog.LoadMap(AppRoot.ContentMapDir());
        var lobby = new CustomLobbyState
        {
            mapPath = AppRoot.ContentMapDir(),
            mapDisplayName = "tutorial",
        };
        var human = new LobbyPlayer
        {
            local = true,
            host = true,
            kind = LobbyPlayerKind.HUMAN,
            displayName = "test-host",
            memberTemplateId = "template_1",
            assetTemplateId = "assets_1",
            spawnSolarSystemId = map.Project.systems[0].solarSystemId,
        };
        lobby.players.Add(human);
        var ai = new LobbyPlayer
        {
            kind = LobbyPlayerKind.AI,
            displayName = "ai-1",
            memberTemplateId = "template_1",
            assetTemplateId = "assets_1",
            spawnSolarSystemId = map.Project.systems.Count > 1
                ? map.Project.systems[1].solarSystemId
                : map.Project.systems[0].solarSystemId,
        };
        lobby.players.Add(ai);

        var core = CampaignBootstrap.CreateFromLobby(lobby);
        var templateCount = StartingTemplateLoader.LoadMembers("template_1").Count;
        var humanLegionId = core.State.legions.First(l => l.isLocal).legionId;
        var aiLegionId = core.State.legions.First(l => l.isAiControlled).legionId;
        var humanRoster = LegionPlayerRegistry.VisibleRoster(core.State, humanLegionId);
        var aiRoster = LegionPlayerRegistry.VisibleRoster(core.State, aiLegionId);

        Assert.That(templateCount, Is.GreaterThan(0));
        Assert.That(humanRoster, Is.Not.Empty, "human legion roster should not be empty when sharing template with AI");
        Assert.That(aiRoster, Is.Not.Empty, "AI legion roster should not be empty when sharing template with human");
        Assert.That(humanRoster.Count, Is.EqualTo(templateCount));
        Assert.That(aiRoster.Count, Is.EqualTo(templateCount));
        Assert.That(core.State.members.Count, Is.EqualTo(templateCount * 2));
        Assert.That(humanRoster.Select(m => m.memberId).Distinct().Count(), Is.EqualTo(templateCount));
        Assert.That(aiRoster.Select(m => m.memberId).Distinct().Count(), Is.EqualTo(templateCount));
        var overlap = humanRoster.Select(m => m.memberId).Intersect(aiRoster.Select(m => m.memberId)).ToList();
        Assert.That(overlap, Is.Empty, "memberId must be unique per legion instance");
        var sharedIdentity = humanRoster[0].identityCode;
        Assert.That(aiRoster.Any(m => sharedIdentity.Equals(m.identityCode, StringComparison.Ordinal)), Is.True,
            "same template identity may repeat across legions");
    }

    [Test]
    public void LobbyListsOnlyPlayableLegionTemplates()
    {
        var lobbyTemplates = ContentCatalog.ListLobbyMemberTemplates();
        Assert.That(lobbyTemplates.Select(t => t.templateId), Is.EquivalentTo(new[]
        {
            "template_1",
            "template_vip_invest",
        }));
    }
}
