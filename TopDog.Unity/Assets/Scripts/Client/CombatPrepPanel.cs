using System.Collections.Generic;
using System;
using System.Linq;
using TopDog.App;
using TopDog.Content.Map;
using TopDog.Sim.Combat;
using TopDog.Sim.State;
using UnityEngine.UIElements;

namespace TopDog.Client;

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
            {
                root.Add(MakeBody("已编译交战队列；运营倒计时结束后将进入交战准备："));
                RenderEventListPreview(root, state);
            }
            else
            {
                root.Add(MakeBody("当前为运营阶段。倒计时结束后若存在巡逻/交战事件，将自动进入交战准备。"));
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
        root.Add(MakeCaption($"自动交战 {ordinal}/{total} · {SubtypeLabel(entry?.combatSubtype)}"));

        if (entry == null)
        {
            root.Add(MakeBody("交战列表为空"));
            return;
        }

        root.Add(MakeBody(entry.label ?? entry.entryId ?? "?"));
        root.Add(MakeHint("战场: " + BattlefieldLabel(state, entry)));
        RenderPowerComparison(root, entry);

        root.Add(MakeCaption("【我方】"));
        RenderRoster(root, entry.friendlyRosterLines, true);
        root.Add(MakeCaption("【敌方】"));
        RenderRoster(root, entry.enemyRoster, false);

        if (state.phase == GamePhase.COMBAT_PREP && state.combatPrepStep == CombatPrepStep.CHOOSE_STANCE)
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
            root.Add(MakeBody($"  {ord}/{tot} · {SubtypeLabel(e.combatSubtype)} · {e.label ?? e.entryId}"));
        }
    }

    private static void RenderEventList(
        VisualElement root,
        GameState state,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi,
        ScrollView scroll)
    {
        root.Add(MakeBody("逐场处理 · 可选自动交战、参与战斗或舰队撤退"));
        if (state.combatQueue.Count == 0)
        {
            root.Add(MakeBody("（交战列表为空）"));
            return;
        }
        for (var i = 0; i < state.combatQueue.Count; i++)
        {
            var e = state.combatQueue[i];
            var current = i == state.combatQueueIndex;
            var ord = e.queueOrdinal > 0 ? e.queueOrdinal : i + 1;
            var tot = e.queueTotal > 0 ? e.queueTotal : state.combatQueue.Count;
            var mark = current ? "▶ " : "  ";
            var line = $"{mark}{ord}/{tot} · {SubtypeLabel(e.combatSubtype)} · {e.label ?? e.entryId}";
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
        }
        var autoBtn = new Button { text = "自动交战" };
        autoBtn.clicked += () =>
        {
            onMessage(core.CombatChooseAuto());
            refreshUi();
            Populate(scroll, core, onMessage, refreshUi);
        };
        root.Add(autoBtn);
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
        var rtBtn = new Button { text = "实时指挥（R4）" };
        rtBtn.clicked += () =>
        {
            onMessage(core.CombatChooseRealtime());
            GameSceneRouter.Instance?.Load(TopDogSceneKind.CombatRealtime);
            refreshUi();
        };
        root.Add(rtBtn);
    }

    private static void RenderRoster(VisualElement root, List<CombatRosterLine> lines, bool friendly)
    {
        if (lines.Count == 0)
        {
            root.Add(MakeBody("  (无)"));
            return;
        }
        foreach (var line in lines)
        {
            var tag = RosterTag(line);
            var arrival = line.arrivalSec >= 0 ? $" T+{line.arrivalSec}s" : "";
            var text = $"  {line.displayName}{tag}\n    {line.hullId} · {line.tonnageClass} · 估值 {line.combatPower:F0} 星币{arrival}";
            root.Add(MakeBody(text));
        }
    }

    private static void RenderPowerComparison(VisualElement root, CombatQueueEntry entry)
    {
        var friendly = entry.friendlyRosterLines.Sum(l => l.combatPower);
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

    private static string RosterTag(CombatRosterLine line)
    {
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

    private static string SubtypeLabel(CombatSubtype? subtype) => subtype switch
    {
        CombatSubtype.COUNTER_HARVEST => "反收割",
        CombatSubtype.BUILDING_ASSAULT => "建筑争夺",
        CombatSubtype.HARVEST => "巡逻/收割",
        _ => "交战",
    };

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
}
