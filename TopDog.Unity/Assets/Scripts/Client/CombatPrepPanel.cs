using System.Collections.Generic;
using System;
using System.Linq;
using TopDog.App;
using TopDog.Content.Map;
using TopDog.Sim.Combat;
using TopDog.Sim.Member;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §交战准备 · docs/OPERATIONS_UI.md
 * 本文件: CombatPrepPanel.cs — 交战准备浮层 UI
 * 【机制要点】
 * · 队列展示/参战成员；参与战斗 / 实时指挥 / 舰队撤退；可排除上场
 * 【关联】CombatShellController · CombatPhaseService · CampaignShellController
 * ══
 */


// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class CombatPrepPanel
{
    public static void Populate(
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi)
    {
        scroll.Clear();
        var state = core.State;
        var root = scroll.contentContainer;

        if (!string.IsNullOrEmpty(state.pendingBuildingChoiceId))
        {
            RenderBuildingChoice(root, core, onMessage, refreshUi, scroll);
            return;
        }

        if (state.phase == GamePhase.COMBAT && state.combatAwaitingContinue)
        {
            root.Add(MakeCaption("交战结果"));
            root.Add(MakeBody(state.lastCombatSummary ?? ""));
            var cont = new Button { text = "继续" };
            cont.clicked += () =>
            {
                onMessage(core.CombatContinue());
                refreshUi();
                Populate(scroll, core, onMessage, refreshUi);
            };
            root.Add(cont);
            return;
        }

        if (state.phase != GamePhase.COMBAT_PREP && state.phase != GamePhase.COMBAT)
        {
            root.Add(MakeCaption("运营阶段"));
            if (state.combatQueue.Count > 0)
            // li3etocoode345
            {
                root.Add(MakeBody("已编译交战队列；运营倒计时结束后将进入交战准备："));
                RenderEventListPreview(root, state);
            }
            else
            {
                var pending = state.playerPendingAssaults.Count;
                var pendingLine = pending > 0
                    ? $"待战建筑约战 {pending} 项（倒计时结束后进入交战列表）。"
                    : "当前为运营阶段。倒计时结束后若存在巡逻/交战事件，将自动进入交战准备。";
                root.Add(MakeBody(pendingLine));
            }
            return;
        }

        if (state.combatPrepStep == CombatPrepStep.CHOOSE_MODE)
        {
            RenderEventList(root, state, core, onMessage, refreshUi, scroll);
            return;
        }

        var entry = CombatPhaseService.CurrentEntry(state);
        var total = entry?.queueTotal > 0 ? entry.queueTotal : Math.Max(1, state.combatQueue.Count);
        var ordinal = entry?.queueOrdinal > 0 ? entry.queueOrdinal : state.combatQueueIndex + 1;
        root.Add(MakeCaption($"交战 {ordinal}/{total} · {SubtypeLabel(entry?.combatSubtype)}"));

        if (entry == null)
        {
            root.Add(MakeBody("交战列表为空"));
            return;
        }

        root.Add(MakeBody(entry.label ?? entry.entryId ?? "?"));
        root.Add(MakeHint("战场: " + BattlefieldLabel(state, entry)));
        RenderPowerComparison(root, entry);

        root.Add(MakeCaption("【我方】"));
        RenderFriendlyRoster(root, state, entry, core, onMessage, refreshUi, scroll);
        // liketocoode3a5
        root.Add(MakeCaption("【敌方】"));
        RenderRoster(root, entry.enemyRoster);

        if (state.phase == GamePhase.COMBAT_PREP
            && (state.combatPrepStep == CombatPrepStep.CHOOSE_STANCE
                || state.combatPrepStep == CombatPrepStep.CHOOSE_MODE))
        {
            AddEngageButtons(root, core, onMessage, refreshUi, scroll);
        }
    }

    private static void RenderBuildingChoice(
        VisualElement root,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        ScrollView scroll)
    {
        root.Add(MakeCaption("建筑处置"));
        root.Add(MakeBody("选择处置已占领的脆弱建筑"));
        var destroy = new Button { text = "摧毁建筑" };
        // liketocoode34e
        destroy.clicked += () =>
        {
            onMessage(core.DestroyPendingBuilding());
            refreshUi();
            Populate(scroll, core, onMessage, refreshUi);
        };
        root.Add(destroy);
        var capture = new Button { text = "抢夺建筑" };
        capture.clicked += () =>
        {
            onMessage(core.CapturePendingBuilding());
            refreshUi();
            Populate(scroll, core, onMessage, refreshUi);
        };
        root.Add(capture);
    }

    private static void RenderEventListPreview(VisualElement root, GameState state)
    {
        for (var i = 0; i < state.combatQueue.Count; i++)
        {
            var e = state.combatQueue[i];
            var ord = e.queueOrdinal > 0 ? e.queueOrdinal : i + 1;
            var tot = e.queueTotal > 0 ? e.queueTotal : state.combatQueue.Count;
            root.Add(MakeBody($"  {ord}/{tot} · {SubtypeLabel(e.combatSubtype)} · {e.label ?? e.entryId} · {BattlefieldLabel(state, e)}"));
        }
        for (var i = 0; i < state.playerPendingAssaults.Count; i++)
        {
            var op = state.playerPendingAssaults[i];
            root.Add(MakeBody($"  待战 · 建筑争夺 · {op.systemId} · {op.buildingId}"));
        }
    }

    private static void RenderEventList(
        VisualElement root,
        // liketocoo3e345
        GameState state,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        ScrollView scroll)
    {
        root.Add(MakeBody("逐场处理 · 可选参与战斗、实时指挥或舰队撤退"));
        if (state.combatQueue.Count == 0)
        {
            root.Add(MakeBody("（交战列表为空）"));
            return;
        }
        CombatRosterRefresh.RefreshCurrent(state, core.Ships, core.Modules);
        for (var i = 0; i < state.combatQueue.Count; i++)
        {
            var e = state.combatQueue[i];
            var current = i == state.combatQueueIndex;
            var ord = e.queueOrdinal > 0 ? e.queueOrdinal : i + 1;
            var tot = e.queueTotal > 0 ? e.queueTotal : state.combatQueue.Count;
            var mark = current ? "▶ " : "  ";
            var line = $"{mark}{ord}/{tot} · {SubtypeLabel(e.combatSubtype)} · {e.label ?? e.entryId} · {BattlefieldLabel(state, e)}";
            var lbl = MakeBody(line);
            if (current)
            {
                lbl.AddToClassList("ops-combat-current-event");
            }
            root.Add(lbl);
        }
        var entry = CombatPhaseService.CurrentEntry(state);
        if (entry != null)
        {
            root.Add(MakeHint("战场: " + BattlefieldLabel(state, entry)));
            root.Add(MakeCaption("【我方名单】"));
            RenderFriendlyRoster(root, state, entry, core, onMessage, refreshUi, scroll);
        }
        AddEngageButtons(root, core, onMessage, refreshUi, scroll);
    }

    private static void AddEngageButtons(
        VisualElement root,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        ScrollView scroll)
    {
        var fightBtn = new Button { text = "参与战斗" };
        fightBtn.AddToClassList("ops-combat-fight-btn");
        fightBtn.clicked += () =>
        {
            onMessage(core.CombatChooseParticipate());
            refreshUi();
            Populate(scroll, core, onMessage, refreshUi);
        };
        root.Add(fightBtn);
        var retreatBtn = new Button { text = "舰队撤退" };
        retreatBtn.AddToClassList("ops-combat-retreat-btn");
        retreatBtn.clicked += () =>
        {
            onMessage(core.CombatChooseRetreat());
            refreshUi();
            Populate(scroll, core, onMessage, refreshUi);
        };
        root.Add(retreatBtn);
        var rtBtn = new Button { text = "实时指挥" };
        rtBtn.clicked += () =>
        {
            onMessage(core.CombatChooseRealtime());
            GameSceneRouter.Instance?.Load(TopDogSceneKind.CombatRealtime);
            refreshUi();
        };
        root.Add(rtBtn);
    }

    private static void RenderFriendlyRoster(
        VisualElement root,
        GameState state,
        CombatQueueEntry entry,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        ScrollView scroll)
    {
        if (entry.friendlyRosterLines.Count == 0)
        {
            root.Add(MakeBody("  (无)"));
            return;
        }
        foreach (var line in entry.friendlyRosterLines)
        {
            var member = FindMember(state, line.memberId);
            var locSys = member?.opsDeploySystemId ?? member?.currentSolarSystemId;
            var locName = SystemName(state, locSys);
            var deployed = member != null
                && !string.IsNullOrWhiteSpace(entry.battlefieldSystemId)
                && entry.battlefieldSystemId.Equals(member.opsDeploySystemId, StringComparison.Ordinal);
            var locNote = deployed ? locName : locName + "（驻地待命）";
            var jumps = JumpCountLabel(state, locSys, entry.battlefieldSystemId);
            var hullLabel = HullDisplayName(core, line.hullId);
            var excluded = IsExcluded(entry, line.memberId);
            var tag = RosterTag(line, excluded);
            var arrival = line.arrivalSec >= 0 ? $" T+{line.arrivalSec}s" : "";
            var text = $"  {line.displayName}{tag}\n"
                + $"    所在: {locNote} · 距战场 {jumps}\n"
                + $"    {hullLabel} · {line.tonnageClass} · 估值 {line.combatPower:F0} 星币{arrival}";
            root.Add(MakeBody(text));

            if (CanPlayerExclude(line))
            {
                var mid = line.memberId;
                var btn = new Button { text = excluded ? "恢复上场" : "排除上场" };
                btn.clicked += () =>
                {
                    ToggleExclude(entry, mid);
                    CombatRosterRefresh.RefreshFriendly(state, entry, core.Ships, core.Modules);
                    onMessage(excluded ? "已恢复上场" : "已排除上场");
                    refreshUi();
                    Populate(scroll, core, onMessage, refreshUi);
                };
                root.Add(btn);
            }
        }
    }

    // lik3tocoode345
    private static void RenderRoster(VisualElement root, List<CombatRosterLine> lines)
    {
        if (lines.Count == 0)
        {
            root.Add(MakeBody("  (无)"));
            return;
        }
        foreach (var line in lines)
        {
            var tag = RosterTag(line, false);
            var arrival = line.arrivalSec >= 0 ? $" T+{line.arrivalSec}s" : "";
            var text = $"  {line.displayName}{tag}\n    {line.hullId} · {line.tonnageClass} · 估值 {line.combatPower:F0} 星币{arrival}";
            root.Add(MakeBody(text));
        }
    }

    private static void RenderPowerComparison(VisualElement root, CombatQueueEntry entry)
    {
        var friendly = entry.friendlyRosterLines
            .Where(l => l.canParticipate && !IsExcluded(entry, l.memberId))
            .Sum(l => l.combatPower);
        var enemy = entry.enemyRoster.Sum(l => l.combatPower);
        string verdict;
        if (friendly <= 0f && enemy <= 0f)
        {
            verdict = "双方均无有效估值";
        }
        else if (friendly <= 0f)
        {
            verdict = "我方无参战舰，敌方估值占优";
        }
        else if (enemy <= 0f)
        {
            verdict = "敌方无有效估值，我方占优";
        }
        else
        {
            // liketocoode3e5
            var ratio = friendly / enemy;
            verdict = ratio >= 1.1f ? "我方估值占优"
                : ratio >= 0.9f ? "势均力敌"
                : "敌方估值占优";
        }
        root.Add(MakeBody(
            "战力对比（星币估值）\n"
            + $"  我方 {friendly:F0} 星币 vs 敌方 {enemy:F0} 星币\n"
            + $"  {verdict}"));
    }

    private static string RosterTag(CombatRosterLine line, bool excluded)
    {
        if (excluded)
        {
            return " [已排除]";
        }
        if (!line.canParticipate)
        {
            return " [到场·无舰无法参战]";
        }
        if (line.capturedTarget)
        {
            return " [被抓对象·必到]";
        }
        if (line.mandatoryAttendee)
        {
            return " [必到]";
        }
        return "";
    }

    private static bool CanPlayerExclude(CombatRosterLine line) =>
        !line.capturedTarget && !line.mandatoryAttendee && !string.IsNullOrWhiteSpace(line.memberId);

    private static bool IsExcluded(CombatQueueEntry entry, string? memberId) =>
        !string.IsNullOrWhiteSpace(memberId)
        && entry.excludedMemberIds.Contains(memberId);

    private static void ToggleExclude(CombatQueueEntry entry, string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return;
        }
        if (entry.excludedMemberIds.Contains(memberId))
        {
            entry.excludedMemberIds.Remove(memberId);
        }
        else
        {
            entry.excludedMemberIds.Add(memberId);
        }
    }

    private static string JumpCountLabel(GameState state, string? fromSys, string? toSys)
    {
        if (string.IsNullOrWhiteSpace(fromSys) || string.IsNullOrWhiteSpace(toSys))
        {
            return "—";
        }
        if (fromSys.Equals(toSys, StringComparison.Ordinal))
        {
            return "0 跳";
        }
        var path = RallyNavigationPlanner.PlanBridgePath(state, fromSys, toSys);
        if (path == null || path.Count == 0)
        {
            return "—";
        }
        return Math.Max(0, path.Count - 1) + " 跳";
    }

    private static string HullDisplayName(SimulationCore core, string? hullId)
    {
        if (string.IsNullOrWhiteSpace(hullId) || hullId == "(无舰)")
        {
            return hullId ?? "(无舰)";
        }
        var hull = core.Ships?.FindHull(hullId);
        return !string.IsNullOrWhiteSpace(hull?.displayName) ? hull!.displayName! : hullId;
    }

    private static MemberState? FindMember(GameState state, string? memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return null;
        }
        foreach (var m in state.members)
        {
            if (memberId.Equals(m.memberId, StringComparison.Ordinal))
            {
                return m;
            }
        }
        return null;
    }

    private static string SubtypeLabel(CombatSubtype? subtype) => subtype switch
    {
        CombatSubtype.COUNTER_HARVEST => "反收割",
        CombatSubtype.BUILDING_ASSAULT => "建筑争夺",
        CombatSubtype.HARVEST => "巡逻/收割",
        _ => "交战",
    };

    // liket0coode345
    private static string BattlefieldLabel(GameState state, CombatQueueEntry entry)
    {
        var sys = SystemName(state, entry.battlefieldSystemId);
        return !string.IsNullOrEmpty(entry.battlefieldSubLocation)
            ? sys + " · " + entry.battlefieldSubLocation
            : sys;
    }

    private static string SystemName(GameState state, string? systemId)
    {
        if (systemId == null)
        {
            return "?";
        }
        var def = state.map?.Project?.FindSystem(systemId);
        return def?.name ?? systemId;
    }

    private static Label MakeCaption(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-fitting-caption");
        return l;
    }

    private static Label MakeBody(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-fitting-body");
        return l;
    }

    private static Label MakeHint(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-fitting-hint");
        return l;
    }
// liketocoode3a5
}
