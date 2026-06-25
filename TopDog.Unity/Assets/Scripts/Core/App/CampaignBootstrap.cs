using TopDog.App.Brick;
using TopDog.Content.Assets;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Starting;
using TopDog.Content.Traits;
using TopDog.Lobby;
using TopDog.Sim.Building;
using TopDog.Sim.Exchange;
using TopDog.Sim.Legion;
using TopDog.Sim.Operations;
using TopDog.Sim.Order;
using TopDog.Sim.Phase;
using TopDog.Sim.Realtime;
using TopDog.Sim.Ship;
using TopDog.Sim.State;
using TopDog.Sim.Trigger;
using TopDog.Sim.Tutorial;
using TopDog.Sim.Map;
using TopDog.Sim.Economy;
using TopDog.Sim.Member;

namespace TopDog.App;

public static class CampaignBootstrap
{
    public enum Profile
    {
        SHELL,
        TUTORIAL_OPS,
        SHIPS_AND_MAP,
        CUSTOM_CAMPAIGN,
    }

    public static SimulationCore Create(Profile profile, WorldlineType worldline)
    {
        var state = new GameState();
        state.worldline.type = worldline;
        state.worldline.tutorialMode = profile == Profile.TUTORIAL_OPS;
        return BuildCore(state, profile);
    }

    public static SimulationCore CreateFromLobby(CustomLobbyState lobby)
    {
        var state = new GameState();
        LobbyBootstrap.ApplyToState(state, lobby);
        return BuildCore(state, Profile.CUSTOM_CAMPAIGN);
    }

    private static SimulationCore BuildCore(GameState state, Profile profile)
    {
        var graph = new BrickGraph();
        graph.Add(new MapBootstrapBrick());
        graph.Add(new PhaseDriverBrick());
        graph.Add(new TriggerEngineBrick());

        TutorialOpsBrick? tutorial = null;
        if (profile == Profile.TUTORIAL_OPS)
        {
            graph.Add(new OperationClockBrick());
            graph.Add(new RecruitBrick());
            tutorial = new TutorialOpsBrick();
            graph.Add(tutorial);
        }
        else if (profile == Profile.CUSTOM_CAMPAIGN)
        {
            graph.Add(new OperationClockBrick());
            graph.Add(new ExchangeSystemBrick());
            graph.Add(new FleetTransitBrick());
            graph.Add(new BattlefieldSystemBrick());
        }
        else if (profile >= Profile.SHIPS_AND_MAP)
        {
            graph.Add(new FleetTransitBrick());
        }

        graph.Add(new OrderExecutorBrick());

        var ships = ShipRegistry.LoadDefault();
        var traits = TraitCatalog.LoadDefault();
        var modules = ModuleRegistry.LoadDefault();
        WireOrderExecutor(graph, tutorial);
        SeedMembers(state, profile);
        IdentityMigrationService.EnsureFromMembers(state);

        if (profile == Profile.CUSTOM_CAMPAIGN)
        {
            try
            {
                StartingTemplateLoader.ApplyToState(state);
            }
            catch (Exception e)
            {
                PushBootstrapAlert(state, "开局模版加载失败: " + e.Message);
            }
            DefaultAssetBootstrap.ApplyIfEmpty(state);
            BuildingService.SeedCampaignFortresses(state, new Random(state.storyRound + 1));
            LegionRegistry.SyncLocalStockToLegacy(state);
            if (!state.flags.ContainsKey("ai.rosterSize"))
            {
                state.flags["ai.rosterSize"] = "1";
            }
            StarCoinService.SyncAllMemberFunds(state);
            MatchIdentityRegistry.SyncAll(state);
            if (state.members.Count == 0)
            {
                PushBootstrapAlert(state,
                    "开局模版「" + state.worldline.startingTemplateId + "」无预设团员，请用招新扩充或选择预设团员模版");
            }
        }

        MarketRefreshService.EnsureInitial(state, modules, ships);
        if (profile == Profile.CUSTOM_CAMPAIGN)
        {
            state.flags["exchange.enabled"] = "1";
        }
        if (state.members.Count > 0 || state.legions.Count > 0)
        {
            LegionPlayerRegistry.EnsureFromLegions(state);
            LegionPlayerRegistry.EnsureAggregateFromBuckets(state);
            if (state.members.Count > 0)
            {
                LegionPlayerRegistry.PartitionMembers(state);
            }
            var localId = LegionRegistry.Local(state)?.legionId;
            if (!string.IsNullOrWhiteSpace(localId))
            {
                LegionPlayerRegistry.EnsureRosterForLegion(state, localId);
            }
        }
        LegionBrickClusterFactory.RegisterAll(graph, state);
        return new SimulationCore(state, graph, ships, traits, modules);
    }

    private static void SeedMembers(GameState state, Profile profile)
    {
        if (profile is Profile.SHELL or Profile.CUSTOM_CAMPAIGN)
        {
            return;
        }

        var lin = new MemberState
        {
            memberId = "1000000101",
            identityCode = "10000001",
            accountSuffix = "01",
            name = "林准将",
            accountName = "教程账号",
            rarity = "A",
            trueRarity = "A",
            appraised = true,
            legionBelonging = 4,
            energy = 3,
        };
        var wang = new MemberState
        {
            memberId = "1000000201",
            identityCode = "10000002",
            accountSuffix = "01",
            name = "王上校",
            accountName = "教程账号",
            rarity = "B",
            trueRarity = "B",
            appraised = true,
        };
        state.members.Add(lin);
        state.members.Add(wang);

        var spawn = state.currentSolarSystemId;
        if (spawn != null)
        {
            lin.currentSolarSystemId = spawn;
            wang.currentSolarSystemId = spawn;
        }

        if (profile == Profile.SHIPS_AND_MAP)
        {
            lin.equippedHullId = "hull_bc_spear";
            wang.assignedTask = "待命";
        }
    }

    private static void WireOrderExecutor(BrickGraph graph, TutorialOpsBrick? tutorial)
    {
        OrderExecutorBrick? order = null;
        FleetTransitBrick? transit = null;
        foreach (var b in graph.Bricks)
        {
            if (b is OrderExecutorBrick o)
            {
                order = o;
            }
            else if (b is FleetTransitBrick t)
            {
                transit = t;
            }
            else if (b is TutorialOpsBrick tb)
            {
                tutorial = tb;
            }
        }
        order?.Bind(transit, tutorial);
    }

    private static void PushBootstrapAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
