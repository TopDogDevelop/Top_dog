using TopDog.Content.Assets;
using TopDog.Content.Balance;
using TopDog.Content.Mechanisms;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Foundation.Io;
using TopDog.Lobby;
using TopDog.Net.Lan;
using TopDog.Sim.State;
using TopDog.Sim.Trigger;

namespace TopDog.Tests;

public sealed class ContentPipelineTests
{
    /// <summary>FIRST_PACK_CONTENT §3–§8 + 采矿光束 S；assets_default 各 ×100。</summary>
    private static readonly string[] FirstPackEquipmentIds =
    [
        "mod_scanner_s", "mod_warp_scram_s", "mod_web_s", "mod_damage_control_s",
        "mod_energy_disrupt_s", "mod_energy_drain_s",
        "mod_shield_regen_m", "mod_armor_regen_m", "mod_propulsion_m", "mod_hybrid_gun_m",
        "mod_shield_regen_l", "mod_armor_regen_l", "mod_shield_resist_l", "mod_armor_resist_l",
        "mod_propulsion_l", "mod_hybrid_gun_l", "mod_strike_wing_a_l", "mod_chaos_missile_l",
        "mod_hybrid_gun_xl",
        "plug_range_10", "plug_speed_10", "plug_warp_speed_10",
        "plug_shield_resist_10", "plug_armor_resist_10", "plug_damage_control_wide",
        "mod_ore_mining_beam_s",
    ];

    private const int FirstPackEquipmentQty = 100;
    [SetUp]
    public void SetUp()
    {
        AppRoot.InvalidateCache();
        BalanceConfig.InvalidateCache();
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
    }

    [Test]
    public void ModuleRegistryLoadsJsonFromContent()
    {
        var reg = ModuleRegistry.LoadDefault();
        Assert.That(reg.Find("mod_propulsion_m"), Is.Not.Null);
        Assert.That(reg.All().Count, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void ModuleCatalogStubResolvesUnknownId()
    {
        var reg = ModuleRegistry.Empty();
        var stub = ModuleCatalog.Resolve(reg, "mod_hybrid_gun_xl");
        Assert.That(stub, Is.Not.Null);
        Assert.That(stub!.slotCategory, Is.EqualTo("ATTACK"));
    }

    [Test]
    public void TraitCatalogSearchFindsMultibox()
    {
        var cat = TraitCatalog.LoadDefault();
        var hits = cat.Search("多开");
        Assert.That(hits.Any(t => t.traitId == "trait_multibox"), Is.True);
    }

    [Test]
    public void TraitCatalogResolvesAlias()
    {
        var cat = TraitCatalog.LoadDefault();
        Assert.That(cat.ResolveTraitId("可附身"), Is.EqualTo("trait_direct_possess"));
        Assert.That(cat.ResolveTraitId("死忠"), Is.EqualTo("trait_direct_possess"));
        Assert.That(cat.ResolveTraitId("trait_multibox"), Is.EqualTo("trait_multibox"));
    }

    [Test]
    public void StartingAssetLoaderAppliesDefaultInventory()
    {
        var state = new GameState();
        StartingAssetLoader.ApplyToState(state, "assets_default");
        Assert.That(state.legionStock.GetValueOrDefault("hull_bc_spear"), Is.EqualTo(2));
        Assert.That(state.legionStock.GetValueOrDefault("mod_propulsion_m"), Is.EqualTo(FirstPackEquipmentQty));
        Assert.That(state.legionStock.GetValueOrDefault("hull_carrier_stellar_collector"), Is.EqualTo(1));
        Assert.That(state.legionStock.GetValueOrDefault("strike_wing_a"), Is.EqualTo(FirstPackEquipmentQty));
        Assert.That(state.legionStock.GetValueOrDefault("res_inorganic"), Is.EqualTo(10000));
        Assert.That(state.legionStock.ContainsKey("item_star_coin"), Is.True);
    }

    [Test]
    public void StartingAssetLoaderAppliesAllFirstPackEquipmentAt100()
    {
        var state = new GameState();
        StartingAssetLoader.ApplyToState(state, "assets_default");
        foreach (var id in FirstPackEquipmentIds)
        {
            Assert.That(state.legionStock.GetValueOrDefault(id), Is.EqualTo(FirstPackEquipmentQty), id);
        }
    }

    [Test]
    public void ListAssetTemplatesIncludesDefault()
    {
        var list = ContentCatalog.ListAssetTemplates();
        Assert.That(list.Any(a => a.assetTemplateId == "assets_default"), Is.True);
    }

    [Test]
    public void ListMapsIncludesBuiltinTutorial()
    {
        var list = ContentCatalog.ListMaps();
        Assert.That(list.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(list.Any(m => m.id == "builtin:tutorial"), Is.True);
    }

    [Test]
    public void DefaultSpawnForMapUsesAssetHintOrFirstSystem()
    {
        var map = ContentCatalog.LoadMap(AppRoot.ContentMapDir());
        var asset = ContentCatalog.ListAssetTemplates().First(a => a.assetTemplateId == "assets_default");
        var spawn = ContentCatalog.DefaultSpawnForMap(map, asset);
        Assert.That(spawn, Is.Not.Null);
        Assert.That(map.Project.systems.Any(s => s.solarSystemId == spawn), Is.True);
    }

    [Test]
    public void LanProtocolRoundTripRoomBeacon()
    {
        var payload = LanProtocol.BuildRoomPayload("room-1", "192.168.1.10", 2, "tutorial.topdog-map", 28777);
        var peer = LanProtocol.ParseRoomBeacon(payload, "192.168.1.10");
        Assert.That(peer, Is.Not.Null);
        Assert.That(peer!.roomId, Is.EqualTo("room-1"));
        Assert.That(peer.hostIp, Is.EqualTo("192.168.1.10"));
        Assert.That(peer.playerCount, Is.EqualTo(2));
        Assert.That(peer.mapId, Is.EqualTo("tutorial.topdog-map"));
    }

    [Test]
    public void LanProtocolJoinPayloadAndApplyJoiner()
    {
        var join = LanProtocol.BuildJoinPayload("192.168.1.20");
        Assert.That(LanProtocol.ParseJoinerIp(join), Is.EqualTo("192.168.1.20"));
        var lobby = new CustomLobbyState();
        lobby.players.Add(new LobbyPlayer { local = true, displayName = "192.168.1.10" });
        LanProtocol.ApplyJoinerToLobby(lobby, "192.168.1.20", "192.168.1.10");
        Assert.That(lobby.players.Count, Is.EqualTo(2));
        Assert.That(lobby.players[1].remoteHostIp, Is.EqualTo("192.168.1.20"));
    }

    [Test]
    public void MechanismCatalogLoadsNoop()
    {
        var cat = MechanismCatalog.LoadDefault();
        Assert.That(cat.Find("mech_test_noop"), Is.Not.Null);
    }

    [Test]
    public void BalanceConfigReadsMatchFlow()
    {
        var cfg = BalanceConfig.LoadDefault();
        Assert.That(cfg.MatchFlow.operationDurationSec, Is.EqualTo(180f));
        Assert.That(cfg.MatchFlow.emptyCombatNoticeSec, Is.EqualTo(15f));
    }

    [Test]
    public void ActionExecutorPhaseForceAndPresentation()
    {
        var state = new GameState { phase = GamePhase.COMBAT };
        ActionExecutor.Execute(state, new Content.Mechanisms.MechanismActionDef
        {
            type = "phase.force",
            phase = "OPERATIONS",
        });
        Assert.That(state.phase, Is.EqualTo(GamePhase.OPERATIONS));

        ActionExecutor.Execute(state, new Content.Mechanisms.MechanismActionDef
        {
            type = "presentation.enqueue",
            kind = "fullscreen_block",
            messageTemplate = "你遭到了来自 {attacker} 的断网攻击",
            attackerDisplayName = "测试员",
            recoverySec = 5f,
        });
        Assert.That(state.presentationQueue.Count, Is.EqualTo(1));
        Assert.That(state.presentationQueue[0].kind, Is.EqualTo("fullscreen_block"));
    }

    [Test]
    public void TriggerEngineDispatchDoesNotCrashOnNoopMechanism()
    {
        var brick = new TriggerEngineBrick();
        var state = new GameState();
        var ctx = new TopDog.App.Brick.BrickContext(
            state,
            new TopDog.Foundation.Bus.EventBus(),
            new TopDog.Foundation.Clock.SimClock(),
            ShipRegistry.LoadDefault(),
            ModuleRegistry.LoadDefault(),
            TopDog.Content.Traits.TraitCatalog.Empty(),
            new TopDog.Sim.Order.CommandParser());
        brick.OnRegister(ctx);
        Assert.DoesNotThrow(() => ctx.Bus.Publish(TopDog.Foundation.Bus.GameEvent.Of("combat.tick", "")));
    }
}
