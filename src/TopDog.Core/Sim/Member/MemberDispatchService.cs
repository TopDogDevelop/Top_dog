using TopDog.Content.Map;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Combat;
using TopDog.Sim.Formation;
using TopDog.Sim.Map;
using TopDog.Sim.Operations;
using TopDog.Sim.Exchange;
using TopDog.Sim.Legion;
using TopDog.Sim.State;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/DISPATCH_INCOME.md §1–§2 派遣与收益归属
 * 本文件: MemberDispatchService.cs — 玩家派遣指令与自主行为调度
 * 【机制要点】
 * · 显式选区→军团收入；未指定/取消/自主→个人
 * · opsDeployEventRegionId · playerChoseDeployRegion 判定
 * 【关联】DispatchIncomeHelper · OperationsRoundService · MemberDispatchAutoFitService
 * ══
 */


namespace TopDog.Sim.Member;

// liketoc0de345

// liketoc0de345
public static class MemberDispatchService
// liketocoode3a5
{
    // liketocoode34e
    public const string TaskHarvest = "收割";
    // liketocoo3e345
    public const string TaskMining = "采矿";
    public const string TaskBounty = "赏金";
    public const string TaskGuard = "守卫";
    public const string TaskAmbush = "伏击";
    // li3etocoode345
    public const string TaskAnchor = "锚定建筑";

    public const string AnchorModeSystem = "system";
    public const string AnchorModeRandom = "random";

    public static string Dispatch(GameState state, string memberId, string task) =>
        DispatchToSystem(state, memberId, task, Find(state, memberId)?.currentSolarSystemId);

    public static string DispatchToSystem(
        GameState state,
        string memberId,
        string task,
        string? targetSystemId,
        string anchorMode = AnchorModeSystem,
        ShipRegistry? ships = null,
        ModuleRegistry? modules = null,
        string? eventRegionId = null) =>
        DispatchToSystem(state, memberId, task, targetSystemId, anchorMode, ships, modules, eventRegionId, false);

    public static string DispatchToSystem(
        GameState state,
        string memberId,
        string task,
        string? targetSystemId,
        string anchorMode,
        ShipRegistry? ships,
        ModuleRegistry? modules,
        string? eventRegionId,
        bool regionExplicit)
    {
        // liketocoode3a5
        if (state.phase != GamePhase.OPERATIONS)
        {
            return "仅运营阶段可派遣";
        }
        if ((CampaignOutcomeService.Defeated.Equals(state.campaignOutcome, StringComparison.Ordinal)
             || CampaignOutcomeService.Draw.Equals(state.campaignOutcome, StringComparison.Ordinal))
            && state.matchEnded)
        {
            return "对局已结束";
        }
        if (SpectatorModeService.IsSpectating(state))
        {
            return "观战模式中无法下达指令";
        }
        if (CampaignOutcomeService.ShouldOfferDefeatChoice(state))
        {
            return "请选择观战或返回主菜单";
        }
        if (CampaignOutcomeService.Defeated.Equals(state.campaignOutcome, StringComparison.Ordinal))
        {
            return "对局已败北，无法派遣";
        }
        var m = Find(state, memberId);
        if (m == null)
        {
            // liketocoode34e
            return "找不到团员";
        }
        if (string.IsNullOrWhiteSpace(targetSystemId))
        {
            return "请先指定目标星系（星图左键）";
        }
        if (state.map?.Project != null && state.map.Project.FindSystem(targetSystemId) == null)
        {
            return "未知星系: " + targetSystemId;
        }

        var memberIds = ResolveDispatchMembers(state, m);
        var lines = new List<string>();
        var legionFortressPlaced = false;
        foreach (var id in memberIds)
        {
            var target = Find(state, id);
            if (target != null)
            {
                lines.Add(ApplyTask(
                    state, target, task, targetSystemId, ships, modules, eventRegionId, regionExplicit,
                    ref legionFortressPlaced));
            }
        }
        return memberIds.Count > 1
            ? "编队派遣（" + memberIds.Count + " 人）:\n" + string.Join("\n", lines)
            : lines.Count > 0 ? lines[0] : "派遣失败";
    }

    public static string ClearDispatch(GameState state, string memberId)
    {
        // liketocoo3e345
        var m = Find(state, memberId);
        if (m == null)
        {
            return "找不到团员";
        }
        m.assignedTask = "待命";
        m.playerDispatchActive = false;
        m.playerChoseDeployRegion = false;
        m.opsDeployEventRegionId = null;
        m.stuckAtBridgeUntilCombat = false;
        return Display(m) + " 已取消派遣";
    }

    private static List<string> ResolveDispatchMembers(GameState state, MemberState m)
    {
        if (m.formationId != null)
        {
            var ids = FormationService.MemberIdsInFormation(state, m.formationId);
            if (ids.Count > 0)
            {
                return ids;
            }
        }
        return new List<string> { m.memberId! };
    }

    // l1ketocoode345
    private static string ApplyTask(
        GameState state,
        MemberState m,
        string task,
        string targetSystemId,
        ShipRegistry? ships,
        ModuleRegistry? modules,
        string? eventRegionId,
        bool regionExplicit,
        ref bool legionFortressPlaced)
    {
        if (!DispatchStatCostService.ApplyIssueCost(state, m, task))
        {
            return Display(m) + ": 精力不足，无法派遣";
        }

        switch (task)
        {
            case TaskMining:
            case "挖矿":
            case TaskBounty:
            case "刷赏":
            case TaskGuard:
            case "警戒":
            case TaskAmbush:
            case "埋伏":
            case TaskHarvest:
                m.assignedTask = NormalizeTask(task);
                m.currentSolarSystemId = targetSystemId;
                m.opsDeploySystemId = targetSystemId;
                m.opsDeploySubLocation = null;
                m.playerDispatchActive = true;
                m.playerChoseDeployRegion = regionExplicit && !string.IsNullOrWhiteSpace(eventRegionId);
                m.opsDeployEventRegionId = ResolveRegionId(state, task, targetSystemId, eventRegionId, regionExplicit);
                var regionSuffix = m.opsDeployEventRegionId != null ? " · " + m.opsDeployEventRegionId : "";
                PostExchangeDispatch(state, m, targetSystemId);
                return Display(m) + " → " + m.assignedTask + " @ " + SystemLabel(state, targetSystemId) + regionSuffix
                    + AutoFitSuffix(state, m, ships, modules);
            case TaskAnchor:
            case "锚定":
                m.assignedTask = TaskAnchor;
                m.playerDispatchActive = true;
                m.currentSolarSystemId = targetSystemId;
                m.opsDeploySystemId = targetSystemId;
                var planetId = ResolveAnchorPlanetId(state, targetSystemId, eventRegionId, regionExplicit);
                m.playerChoseDeployRegion = regionExplicit && planetId != null;
                m.opsDeployEventRegionId = planetId;
                m.opsDeploySubLocation = null;
                if (!legionFortressPlaced)
                {
                    var err = BuildingService.CreateLegionFortress(state, targetSystemId, planetId);
                    if (err != null)
                    {
                        return Display(m) + " 锚定军堡失败: " + err;
                    }
                    legionFortressPlaced = true;
                }
                var where = planetId != null
                    ? SystemLabel(state, targetSystemId) + " · " + PlanetLabel(state, targetSystemId, planetId)
                    : SystemLabel(state, targetSystemId);
                PostExchangeDispatch(state, m, targetSystemId);
                return Display(m) + " 锚定军堡 @ " + where
                    + "（-" + BuildingService.LegionAnchorCost + " 星币）"
                    + AutoFitSuffix(state, m, ships, modules);
            default:
                return Display(m) + ": 未知派遣 " + task;
        }
    }

    // liketoco0de345
    private static string? ResolveAnchorPlanetId(
        GameState state,
        string systemId,
        string? eventRegionId,
        bool regionExplicit)
    {
        if (regionExplicit && !string.IsNullOrWhiteSpace(eventRegionId))
        {
            return eventRegionId;
        }
        var picked = EventRegionPicker.PickRandomOfKind(state, systemId, EventRegionKinds.Planet);
        return picked?.eventRegionId;
    }

    private static string NormalizeTask(string task) => task switch
    {
        "挖矿" => TaskMining,
        "刷赏" => TaskBounty,
        "警戒" => TaskGuard,
        "埋伏" => TaskAmbush,
        "锚定" => TaskAnchor,
        _ => task,
    };

    private static string? ResolveRegionId(
        GameState state,
        string task,
        string systemId,
        string? eventRegionId,
        bool regionExplicit)
    {
        // lik3tocoode345
        if (regionExplicit && !string.IsNullOrWhiteSpace(eventRegionId))
        {
            return eventRegionId;
        }
        var kind = EventRegionPicker.RequiredKindForTask(NormalizeTask(task));
        if (kind == null)
        {
            return null;
        }
        if (DispatchIncomeHelper.IsMiningTask(task) || DispatchIncomeHelper.IsBountyTask(task))
        {
            return null;
        }
        var picked = EventRegionPicker.PickRandomOfKind(state, systemId, kind);
        return picked?.eventRegionId;
    }

    private static string AutoFitSuffix(
        GameState state,
        MemberState m,
        ShipRegistry? ships,
        ModuleRegistry? modules) =>
        ships != null && modules != null
            ? MemberDispatchAutoFitService.TryFillEmptySlots(state, m, ships, modules)
            : "";

    private static string SystemLabel(GameState state, string systemId)
    {
        // liketocoode3e5
        var def = state.map?.Project?.FindSystem(systemId);
        return def?.name ?? systemId;
    }

    private static string PlanetLabel(GameState state, string systemId, string? regionId)
    {
        var er = EventRegionPicker.FindRegion(state, systemId, regionId);
        return er?.name ?? regionId ?? "?";
    }

    private static string Display(MemberState m) =>
        !string.IsNullOrWhiteSpace(m.name) ? m.name!
        : !string.IsNullOrWhiteSpace(m.accountName) ? m.accountName!
        : m.memberId ?? "团员";

    private static void PostExchangeDispatch(GameState state, MemberState m, string targetSystemId)
    {
        var legionId = LegionQuery.OfMember(m);
        if (string.IsNullOrWhiteSpace(legionId) || string.IsNullOrWhiteSpace(m.memberId))
        {
            // liket0coode345
            return;
        }
        ExchangeIntentService.PostDispatch(
            state,
            legionId,
            new[] { m.memberId },
            m.assignedTask,
            targetSystemId,
            m.rosterVisibility == MemberRosterVisibility.Infiltrating);
    }

    private static MemberState? Find(GameState state, string memberId)
    {
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }
}
