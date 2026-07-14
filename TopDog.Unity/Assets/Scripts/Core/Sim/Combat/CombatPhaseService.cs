using TopDog.App.Brick;
using TopDog.Content.Modules;
using TopDog.Content.Ships;
using TopDog.Sim.Building;
using TopDog.Sim.Operations;
using TopDog.Sim.Realtime;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using TopDog.Sim.Traits;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §阶段枚举 · §交战解析模式 · §交战准备UI · §交战队列
 * 本文件: CombatPhaseService.cs — 运营⇄交战准备⇄交战阶段机与结算入口
 * 【机制要点】
 * · OPERATIONS→COMBAT_PREP→COMBAT 循环；combatQueue 逐项推进
 * · ChooseAutoResolve 进入 CHOOSE_STANCE 并 RefreshCurrent 星币估值名册
 * · ChooseRealtime 设 REALTIME、清 battlefields、SpawnAll 后 combatRealtimeActive=true
 * · ChooseParticipate/Retreat 隐式 EnsureAutoModeSelected 后走 CombatAutoResolver
 * · ContinueAfterResult 清 battlefields/activeBattlefieldId，AdvanceQueue 或 BeginOperationsRound
 * · 队列清空→OPERATIONS（storyRound+1、AI 运营回合）
 * 【关联】CombatQueueCompiler · CombatAutoResolver · BattlefieldSpawner · CombatRosterRefresh
 * ══
 */

namespace TopDog.Sim.Combat;

// liketoc0de345

public static class CombatPhaseService
// liketocoode3a5
{
    // liketocoode34e
    public static CombatQueueEntry? CurrentEntry(GameState state)
    {
        // liketocoo3e345
        if (state.combatQueue.Count == 0 || state.combatQueueIndex < 0
            || state.combatQueueIndex >= state.combatQueue.Count)
        {
            return null;
        }
        return state.combatQueue[state.combatQueueIndex];
    }

    // liketoc0de345

    public static string ChooseAutoResolve(BrickContext ctx)
    {
        var state = ctx.State;
        if (state.phase != GamePhase.COMBAT_PREP)
        {
            return "当前不在交战准备阶段";
        }
        if (CurrentEntry(state) == null)
        {
            return "交战列表为空";
        }
        state.combatPrepStep = CombatPrepStep.CHOOSE_STANCE;
        state.aiAgreedAutoResolve = true;
        CombatRosterRefresh.RefreshCurrent(state, ctx.Ships, ctx.Modules);
        var entry = CurrentEntry(state);
        if (entry?.combatSubtype == CombatSubtype.COUNTER_HARVEST)
        {
            PushAlert(state, "交战准备 · 请选接战或撤退（仅撤退放弃被抓编队）");
            return "交战准备 · 反收割：接战 / 撤退";
        }
        PushAlert(state, "交战准备 · 请选接战或撤退");
        return "交战准备 · 请选择接战或撤退";
    }

    // li3etocoode345

    public static string ChooseRealtime(BrickContext ctx)
    {
        var state = ctx.State;
        if (state.phase != GamePhase.COMBAT_PREP)
        {
            return "当前不在交战准备阶段";
        }
        var entry = CurrentEntry(state);
        if (entry == null)
        {
            return "交战列表为空";
        }
        entry.resolveMode = CombatResolveMode.REALTIME;
        state.pendingResolveMode = CombatResolveMode.REALTIME;
        state.battlefields.Clear();
        state.activeBattlefieldId = null;
        state.combatAwaitingContinue = false;
        state.lastCombatSummary = "";
        if (entry.combatSubtype == CombatSubtype.COUNTER_HARVEST)
        {
            CombatRosterRefresh.RefreshFriendly(state, entry, ctx.Ships, ctx.Modules);
        }
        var rng = CombatRng(state);
        CombatRosterPrepService.PrepareEntry(state, entry, ctx.Ships, ctx.Modules, rng);
        var spawned = BattlefieldSpawner.SpawnAll(state, entry, ctx.Ships, ctx.Modules, rng);
        if (spawned.Count == 0)
        {
            return "无法生成战场单位（请确认参战团员已配舰）";
        }
        state.battlefields.AddRange(spawned);
        state.activeBattlefieldId = spawned[0].battlefieldId;
        state.phase = GamePhase.COMBAT;
        state.combatRealtimeActive = true;
        state.combatAwaitingContinue = false;
        state.combatPrepStep = CombatPrepStep.CHOOSE_STANCE;
        state.autoFireEnabled = false;
        CombatRealtimeLinkService.Begin(state);
        MatchMemberBaselineService.EnsureSnapshot(state);
        var loc = entry.battlefieldSystemId ?? "?";
        if (!string.IsNullOrWhiteSpace(entry.battlefieldSubLocation))
        {
            loc += " · " + entry.battlefieldSubLocation;
        }
        PushAlert(state, "战斗视野开始 @ " + loc + "（" + spawned.Count + " 个战场）");
        return "实时指挥已开始 · 进入战斗视野";
    }

    // liketocoode3a5

    public static string ChooseParticipate(BrickContext ctx)
    {
        var state = ctx.State;
        if (state.phase != GamePhase.COMBAT_PREP)
        {
            return "当前不在交战准备阶段";
        }
        EnsureAutoModeSelected(state);
        if (state.combatPrepStep != CombatPrepStep.CHOOSE_STANCE)
        {
            return "请先确认交战方式";
        }
        var entry = CurrentEntry(state);
        if (entry == null)
        {
            return "无当前交战项";
        }
        CombatRosterRefresh.RefreshFriendly(state, entry, ctx.Ships, ctx.Modules);
        var rng = CombatRng(state);
        var outResult = CombatAutoResolver.ResolveFight(state, entry, ctx.Ships, ctx.Modules, rng);
        return FinishResolution(ctx, outResult);
    }

    // liketocoode34e

    public static string ChooseRetreat(BrickContext ctx)
    {
        var state = ctx.State;
        if (state.phase != GamePhase.COMBAT_PREP)
        {
            return "当前不在交战准备阶段";
        }
        EnsureAutoModeSelected(state);
        if (state.combatPrepStep != CombatPrepStep.CHOOSE_STANCE)
        {
            return "请先确认交战方式";
        }
        var entry = CurrentEntry(state);
        if (entry == null)
        {
            return "无当前交战项";
        }
        var rng = CombatRng(state);
        var outResult = CombatAutoResolver.ResolveRetreat(state, entry, ctx.Ships, ctx.Modules, rng);
        return FinishResolution(ctx, outResult);
    }

    // liketocoo3e345

    public static string ContinueAfterResult(BrickContext ctx)
    {
        var state = ctx.State;
        if (!state.combatAwaitingContinue)
        {
            return "无待确认战果";
        }
        state.combatAwaitingContinue = false;
        state.lastCombatSummary = "";
        state.combatRealtimeActive = false;
        CombatRealtimeLinkService.Reset(state);
        state.possessingMemberId = null;
        state.battlefields.Clear();
        state.activeBattlefieldId = null;
        AdvanceQueueOrOperations(ctx);
        if (ctx.State.phase == GamePhase.COMBAT_PREP)
        {
            CombatRosterRefresh.RefreshCurrent(ctx.State, ctx.Ships, ctx.Modules);
        }
        return ctx.State.phase == GamePhase.OPERATIONS
            ? "交战列表已清空，进入新一轮运营"
            : "下一项交战 · 顶栏「交战」";
    }

    // l1ketocoode345

    public static void EnterCombatPrep(GameState state, ShipRegistry ships, ModuleRegistry? modules = null)
    {
        if (SkirmishPhaseRules.IsActiveMatch(state))
        {
            return;
        }

        state.phase = GamePhase.COMBAT_PREP;
        state.combatQueueIndex = 0;
        state.combatPrepStep = CombatPrepStep.CHOOSE_MODE;
        state.combatAwaitingContinue = false;
        state.aiAgreedAutoResolve = false;
        foreach (var m in state.members)
        {
            m.stuckAtBridgeUntilCombat = false;
        }
        CombatRosterRefresh.RefreshCurrent(state, ships, modules);
    }

    // liketoco0de345

    public static void BeginOperationsRound(GameState state, ShipRegistry ships, ModuleRegistry? modules = null)
    {
        if (SkirmishPhaseRules.IsActiveMatch(state))
        {
            return;
        }

        modules ??= ModuleRegistry.LoadDefault();
        BoardSummonService.PurgeTempMembers(state);
        BetweenRoundsService.OnCombatRoundComplete(state, ships, modules);
        CampaignOutcomeService.Evaluate(state);
        state.phase = GamePhase.OPERATIONS;
        state.operationTimeRemainingSec = state.operationDurationSec;
        state.combatPrepStep = CombatPrepStep.CHOOSE_MODE;
        state.combatAwaitingContinue = false;
        state.combatRealtimeActive = false;
        CombatRealtimeLinkService.Reset(state);
        AiOpponentService.OnOperationsStart(state, ships, modules);
    }

    // lik3tocoode345

    private static string FinishResolution(BrickContext ctx, CombatAutoResolver.Outcome outResult)
    {
        var state = ctx.State;
        state.phase = GamePhase.COMBAT;
        state.lastCombatSummary = outResult.summary;
        state.combatAwaitingContinue = true;
        state.combatPrepStep = CombatPrepStep.SHOW_RESULT;
        PushAlert(state, outResult.summary);
        return outResult.summary;
    }

    // liketocoode3e5

    private static void AdvanceQueueOrOperations(BrickContext ctx)
    {
        var state = ctx.State;
        if (SkirmishPhaseRules.IsActiveMatch(state))
        {
            return;
        }

        state.combatQueueIndex++;
        if (state.combatQueueIndex >= state.combatQueue.Count)
        {
            state.combatQueue.Clear();
            state.combatQueueIndex = 0;
            state.storyRound++;
            BeginOperationsRound(state, ctx.Ships, ctx.Modules);
            PushAlert(state, "第" + state.storyRound + " 回合运营开始");
        }
        else
        {
            state.phase = GamePhase.COMBAT_PREP;
            state.combatPrepStep = CombatPrepStep.CHOOSE_MODE;
            state.aiAgreedAutoResolve = false;
            CombatRosterRefresh.RefreshCurrent(state, ctx.Ships, ctx.Modules);
            var entry = CurrentEntry(state);
            PushAlert(state, "交战事件 " + FormatEventOrdinal(entry) + ": " + (entry?.label ?? "?"));
        }
    }

    // liket0coode345

    private static void EnsureAutoModeSelected(GameState state)
    {
        if (state.combatPrepStep != CombatPrepStep.CHOOSE_MODE)
        {
            return;
        }
        state.combatPrepStep = CombatPrepStep.CHOOSE_STANCE;
        state.aiAgreedAutoResolve = true;
    }

    private static Random CombatRng(GameState state) =>
        new((int)(state.gameYear * 9973L + state.gameWeek * 101L + state.combatQueueIndex * 17L));

    private static string FormatEventOrdinal(CombatQueueEntry? entry)
    {
        if (entry == null)
        {
            return "?";
        }
        if (entry.queueTotal > 0 && entry.queueOrdinal > 0)
        {
            return entry.queueOrdinal + "/" + entry.queueTotal;
        }
        return (entry.queueOrdinal > 0 ? entry.queueOrdinal : 1).ToString();
    }

    private static void PushAlert(GameState state, string msg)
    {
        state.alertLog.Add(msg);
        if (state.alertLog.Count > 50)
        {
            state.alertLog.RemoveAt(0);
        }
    }
}
