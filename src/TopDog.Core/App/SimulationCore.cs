using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Foundation.Bus;
using TopDog.Foundation.Clock;
using TopDog.Sim.Combat;
using TopDog.Sim.Building;
using TopDog.Sim.Economy;
using TopDog.Sim.Formation;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Order;
using TopDog.Sim.Possession;
using TopDog.Sim.Realtime;
using TopDog.Sim.Exchange;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

namespace TopDog.App;

public sealed class SimulationCore
{
    private readonly GameState _state;
    private readonly EventBus _bus;
    private readonly SimClock _clock;
    private readonly ShipRegistry _ships;
    private readonly TraitCatalog _traits;
    private readonly ModuleRegistry _modules;
    private readonly CommandParser _commandParser;
    private readonly BrickGraph _graph;
    private readonly BrickContext _ctx;
    private readonly OrderExecutorBrick? _orderExecutor;

    public SimulationCore(GameState state, BrickGraph graph, ShipRegistry ships)
        : this(state, graph, ships, null, null)
    {
    }

    public SimulationCore(
        GameState state,
        BrickGraph graph,
        ShipRegistry ships,
        TraitCatalog? traits,
        ModuleRegistry? modules)
    {
        _state = state;
        _bus = new EventBus();
        _clock = new SimClock();
        _ships = ships;
        _traits = traits ?? TraitCatalog.Empty();
        _modules = modules ?? ModuleRegistry.Empty();
        _commandParser = new CommandParser();
        _graph = graph;
        _ctx = new BrickContext(state, _bus, _clock, _ships, _modules, _traits, _commandParser);
        _orderExecutor = FindOrderBrick(graph);
        graph.RegisterAll(_ctx);
    }

    public GameState State => _state;
    public EventBus Bus => _bus;
    public SimClock Clock => _clock;
    public BrickContext Context => _ctx;
    public ShipRegistry Ships => _ships;
    public ModuleRegistry Modules => _modules;
    public TraitCatalog Traits => _traits;

    public void Tick(float dtSec)
    {
        _clock.Advance(dtSec);
        _graph.TickAll(_ctx, dtSec);
    }

    public string SubmitCommand(string line, string? issuerLegionId = null)
    {
        var order = _commandParser.Parse(line);
        _state.commandIssuerLegionId = issuerLegionId ?? LegionRegistry.Local(_state)?.legionId;
        try
        {
            if (_orderExecutor != null)
            {
                return _orderExecutor.Execute(_ctx, order);
            }
            return "命令系统未就绪";
        }
        finally
        {
            _state.commandIssuerLegionId = null;
        }
    }

    public string CombatChooseAuto() => CombatPhaseService.ChooseAutoResolve(_ctx);
    public string CombatChooseRealtime()
    {
        if (RecruitService.UsesExchangeHub(_state))
        {
            var entry = CombatPhaseService.CurrentEntry(_state);
            if (entry?.entryId != null && ExchangeProcessor.FindEncounter(_state, entry.entryId) != null)
            {
                var localId = LegionRegistry.Local(_state)?.legionId;
                if (!string.IsNullOrWhiteSpace(localId))
                {
                    ExchangeIntentService.PostResolveVote(
                        _state, entry.entryId, localId, CombatResolveMode.REALTIME);
                    ExchangeProcessor.ProcessPending(_state);
                    if (_state.combatRealtimeActive)
                    {
                        return "实时指挥已开始 · 交换中心物化战场";
                    }
                    return "已提交实时交战投票";
                }
            }
        }
        return CombatPhaseService.ChooseRealtime(_ctx);
    }
    public string CombatChooseParticipate() => CombatPhaseService.ChooseParticipate(_ctx);
    public string CombatChooseRetreat() => CombatPhaseService.ChooseRetreat(_ctx);
    public string CombatContinue() => CombatPhaseService.ContinueAfterResult(_ctx);

    public void SetPhase(GamePhase phase)
    {
        if (_state.phase != phase)
        {
            _state.phase = phase;
            _graph.NotifyPhase(_ctx, phase);
            _bus.Publish(GameEvent.Of("phase.changed", phase.ToString()));
        }
    }

    public string PossessMember(string memberIdOrName) =>
        PossessionService.PossessByName(_state, memberIdOrName);

    public void ApplyPossessionInput(PossessionInputSample sample) =>
        PossessionInputService.QueueInput(_state, sample);

    public string ToggleAutoFire() => FleetOrderService.ToggleAutoFire(_state);

    public string StartRecruit(IReadOnlyList<string>? targetTraitIds) =>
        RecruitService.Start(_state, targetTraitIds);

    public string AppraiseMember(string memberId) =>
        AppraiseService.Appraise(_state, memberId);

    public string TransferLegionAsset(string itemId, string memberIdOrName, int quantity = 1)
    {
        var m = FindMember(memberIdOrName);
        if (m == null)
        {
            return "找不到团员";
        }
        if (quantity <= 0)
        {
            return "数量须大于 0";
        }
        if (MemberAssetService.LegionQty(_state, itemId) < quantity)
        {
            return "军团库存不足（现有 "
                + MemberAssetService.LegionQty(_state, itemId) + "）";
        }
        MemberAssetService.TransferLegionToPersonal(_state, m, itemId, quantity);
        return "已分配 " + quantity + " × "
            + MemberAssetService.ItemDisplayName(itemId, _modules, _ships)
            + " → " + (m.name ?? m.memberId);
    }

    public string EquipMemberModule(string memberIdOrName, string slotKey, string moduleId)
    {
        var m = FindMember(memberIdOrName);
        if (m == null)
        {
            return "找不到团员";
        }
        var hull = m.equippedHullId != null ? _ships.FindHull(m.equippedHullId) : null;
        return MemberFittingService.EquipModule(
            _state, m, slotKey, moduleId, null, hull, _modules);
    }

    public string UnequipMemberModule(string memberIdOrName, string slotKey)
    {
        var m = FindMember(memberIdOrName);
        if (m == null)
        {
            return "找不到团员";
        }
        return MemberFittingService.UnequipModule(_state, m, slotKey, _modules);
    }

    public string CreateFormation(IReadOnlyList<string> memberIds) =>
        FormationService.Create(_state, memberIds);

    public string DissolveFormationForMember(string memberIdOrName)
    {
        var m = FindMember(memberIdOrName);
        return m?.memberId != null
            ? FormationService.DissolveForMember(_state, m.memberId)
            : "找不到团员";
    }

    public string DispatchMemberToSystem(
        string memberIdOrName,
        string task,
        string? systemId,
        string anchorMode = MemberDispatchService.AnchorModeSystem,
        string? eventRegionId = null,
        bool regionExplicit = false)
    {
        var m = FindMember(memberIdOrName);
        return m?.memberId != null
            ? MemberDispatchService.DispatchToSystem(
                _state, m.memberId, task, systemId, anchorMode, _ships, _modules, eventRegionId, regionExplicit)
            : "找不到团员";
    }

    public string DestroyPendingBuilding() =>
        BuildingService.DestroyBuilding(_state, _state.pendingBuildingChoiceId, _ships);

    public string CapturePendingBuilding() =>
        BuildingService.CaptureBuilding(_state, _state.pendingBuildingChoiceId, _ships);

    public string BuyFromMarket(string itemId, int quantity = 1)
    {
        var legionId = LegionRegistry.Local(_state)?.legionId;
        return LegionPlayerTradeService.BuyFromMarket(_state, legionId, itemId, quantity);
    }

    public string CraftHull(string hullId) =>
        CraftService.TryCraftHull(_state, hullId, _ships, _modules);

    public string SellToMarket(string itemId, int quantity = 1)
    {
        var legionId = LegionRegistry.Local(_state)?.legionId;
        return LegionPlayerTradeService.SellToMarket(_state, legionId, itemId, quantity);
    }

    public string BuyFromLegionListing(string listingId, int quantity = 1)
    {
        var legionId = LegionRegistry.Local(_state)?.legionId;
        return LegionPlayerTradeService.BuyFromLegionListing(_state, legionId, listingId, quantity);
    }

    public string BuyFromPlayerListing(string listingId, int quantity = 1) =>
        ExchangeTradeService.BuyFromPlayerListing(_state, listingId, quantity);

    public string ListOnLegionMarket(string itemId, int quantity = 1)
    {
        var legionId = LegionRegistry.Local(_state)?.legionId;
        return LegionPlayerTradeService.ListOnLegionMarket(
            _state, legionId, itemId, quantity, _modules, _ships);
    }

    public string ListOnPlayerMarket(string itemId, int quantity = 1) =>
        ExchangeTradeService.ListOnPlayerMarket(_state, itemId, quantity, _modules, _ships);

    public string AppointLegionCommander(string memberIdOrName) =>
        LegionCommanderService.Appoint(_state, memberIdOrName);

    public string DismissLegionCommander() =>
        LegionCommanderService.Dismiss(_state);

    public bool CanDismissLegionCommander() =>
        LegionCommanderService.CanDismiss(_state);

    public string UseSuppressionSkill(string traitId, string casterMemberIdOrName, string? targetMemberIdOrName = null)
    {
        var caster = FindMember(casterMemberIdOrName);
        if (caster == null)
        {
            return "找不到团员";
        }
        var target = targetMemberIdOrName != null ? FindMember(targetMemberIdOrName) : null;
        if (targetMemberIdOrName != null && target == null)
        {
            return "找不到目标团员";
        }
        return TraitActiveSkillService.TryUse(_state, caster, traitId, target);
    }

    private MemberState? FindMember(string? memberIdOrName)
    {
        if (string.IsNullOrWhiteSpace(memberIdOrName))
        {
            return null;
        }
        var n = memberIdOrName.Trim();
        foreach (var m in _state.members)
        {
            if (n.Equals(m.memberId, StringComparison.Ordinal)
                || n.Equals(m.name, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    private static OrderExecutorBrick? FindOrderBrick(BrickGraph graph)
    {
        foreach (var b in graph.Bricks)
        {
            if (b is OrderExecutorBrick order)
            {
                return order;
            }
        }
        return null;
    }
}
