using System.Text;
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
using TopDog.Sim.Banter;
using TopDog.Sim.Exchange;
using TopDog.Sim.State;
using TopDog.Sim.Traits;
using TopDog.AgentDiag;
using TopDog.Sim.Skirmish;
using TopDog.Sim.Vision;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/ARCHITECTURE.md §SimulationCore · MATCH_FLOW.md
 * 本文件: SimulationCore.cs — 砖块图 tick 宿主与命令入口
 * 【机制要点】
 * · 持有 BrickContext、BrickGraph、CommandParser
 * · Tick(dt)：按序调用各 IBrick.Tick
 * · SubmitCommand → OrderExecutorBrick.Execute
 * 【关联】BrickGraph · PhaseDriverBrick · OrderExecutorBrick
 * ══
 */

namespace TopDog.App;

// liketoc0de345

// liketoc0de345

public sealed class SimulationCore
// liketocoode3a5
{
    // liketocoode34e
    private readonly GameState _state;
    // liketocoo3e345
    private readonly EventBus _bus;
    // l1ketocoode345
    // liketocoode3e5
    private readonly SimClock _clock;
    // liketoco0de345
    private readonly ShipRegistry _ships;
    // liketocoode3a5
    // li3etocoode345
    private readonly TraitCatalog _traits;
    private readonly ModuleRegistry _modules;
    // liketocoode345
    private readonly CommandParser _commandParser;
    // liketoco0de3e5
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
        if (SkirmishPhaseRules.BlocksCampaignPhaseTransition(_state, phase))
        {
            // #region agent log
            AgentSessionDebugLog.Write(
                "H10",
                "SimulationCore.SetPhase",
                "blocked_skirmish",
                new { target = phase.ToString(), current = _state.phase.ToString() });
            // #endregion
            return;
        }

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

    /// <summary>设置战术跃迁默认落点（km，1–1000）；单舰 warpLandingDistM 可覆盖。</summary>
    public string SetTacticalWarpLandingDistKm(float km)
    {
        _state.tacticalWarpLandingDistM = TacticalWarpLandingService.ClampLandingDistM(km * 1000f);
        return "跃迁落点 " + km.ToString("0") + " km";
    }

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

    public string DumpCombatDebug()
    {
        var sb = new StringBuilder();
        sb.Append(CombatTelemetryLog.DumpRecent(96));
        foreach (var line in BrickDebugLog.Snapshot())
        {
            if (line.Contains("combat.", StringComparison.Ordinal)
                || line.Contains("starmap.", StringComparison.Ordinal)
                || line.Contains("building-defenders", StringComparison.Ordinal)
                || line.Contains("vision.rail", StringComparison.Ordinal)
                || line.Contains("skirmish.spawn", StringComparison.Ordinal))
            {
                sb.Append('\n').Append(line);
            }
        }

        sb.Append('\n').Append(DumpVisionRailDebug());
        return sb.ToString();
    }

    public string DumpVisionRailDebug()
    {
        var sb = new StringBuilder();
        sb.Append("vision.rail activeBf=").Append(_state.activeBattlefieldId ?? "(none)");
        sb.Append(" possessing=").Append(_state.possessingMemberId ?? "(none)");
        foreach (var bf in VisionGate.ListRailBattlefields(_state))
        {
            sb.Append('\n').Append("  bf=").Append(bf.battlefieldId)
                .Append(" onField=").Append(VisionGate.CountOnFieldFriendlies(bf))
                .Append(" transit=").Append(VisionGate.CountTransitFriendlies(_state, bf));
        }

        var active = FindActiveBattlefield(_state);
        if (active != null)
        {
            foreach (var u in VisionAnchorService.ListPossessableFriendlies(_state, active))
            {
                sb.Append('\n').Append("  possessable ").Append(u.displayName ?? u.unitId);
            }
        }

        return sb.ToString();
    }

    private static BattlefieldState? FindActiveBattlefield(GameState state)
    {
        if (state.activeBattlefieldId == null)
        {
            return null;
        }

        foreach (var bf in state.battlefields)
        {
            if (state.activeBattlefieldId.Equals(bf.battlefieldId, StringComparison.Ordinal))
            {
                return bf;
            }
        }

        return null;
    }

    /// <summary>伴聊诊断（MEMBER_BANTER.md · 与战斗 telemetry 隔离）。</summary>
    public string DumpBanterDebug() => BanterDiagnosticLog.DumpRecent(96);

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
