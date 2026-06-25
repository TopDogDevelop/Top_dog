using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.App;
using TopDog.Content;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Formation;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;
using TopDog.Sim.State;
using TopDog.Sim.Traits;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Member detail rail: stats, mini fitting diagram, personal assets.</summary>
public static class MemberDetailPanel
{
    public static void Populate(
        ScrollView scroll,
        SimulationCore core,
        MemberState? member,
        Action<string> onMessage,
        Action openFitting,
        Action<string, string>? onUseSuppressionSkill = null)
    {
        scroll.Clear();
        var root = scroll.contentContainer;
        if (member == null)
        {
            root.Add(MakeBody("请从右侧选择团员"));
            return;
        }

        root.Add(MakeCaption(MemberName(member)));
        root.Add(MakeBody($"稀有度 {member.rarity} · 底图 {member.cardBackdrop ?? "—"}"));
        root.Add(MakeBody($"任务 {member.assignedTask ?? "待命"} · 编队 {FormationLabel(core.State, member.formationId)}"));
        if (member.traitIds.Count > 0)
        {
            var catalog = TraitCatalog.LoadDefault();
            var labels = member.traitIds
                .Select(tid => DisplayLabels.TraitBilingual(catalog.Find(tid)))
                .ToList();
            root.Add(MakeBody("词条: " + string.Join(", ", labels)));
        }

        RenderSuppressionSkills(root, core, member, onMessage, onUseSuppressionSkill);

        root.Add(MakeCaption("操作槽"));
        var opRow = new VisualElement();
        opRow.AddToClassList("ops-op-slot-row");
        var fitBtn = new Button { text = "配船" };
        fitBtn.clicked += openFitting;
        opRow.Add(fitBtn);
        if (!string.IsNullOrEmpty(member.equippedHullId))
        {
            var hull = core.Ships.FindHull(member.equippedHullId);
            opRow.Add(MakeBody("舰: " + DisplayLabels.HullBilingual(hull)));
        }
        else
        {
            opRow.Add(MakeBody("（未配舰）"));
        }
        root.Add(opRow);

        RenderPersonalAssets(root, core, member);

        if (!string.IsNullOrEmpty(member.equippedHullId))
        {
            var hull = core.Ships.FindHull(member.equippedHullId);
            var fit = MemberFittingService.Fittings(core.State, member);
            var stats = ShipFitStats.Compute(hull, fit, core.Modules, core.State, member);
            root.Add(MakeCaption("舰况汇总"));
            root.Add(MakeBody(
                $"DPS {stats.dps:F0} · 盾回 {stats.shieldRegenPerSec:F0}/s · 航速 {stats.fittedSpeedMps:F0} m/s"));
            if (!string.IsNullOrEmpty(stats.activePropulsionLabel))
            {
                root.Add(MakeBody("[推进] " + stats.activePropulsionLabel));
            }

            if (hull != null)
            {
                root.Add(MakeCaption("配船示意（点「配船」编辑）"));
                root.Add(FittingRingDiagram.BuildMini(core, member, hull));
            }
        }

        if (!string.IsNullOrWhiteSpace(member.bio))
        {
            root.Add(MakeCaption("简介"));
            root.Add(MakeBody(member.bio));
        }
    }

    private static void RenderSuppressionSkills(
        VisualElement root,
        SimulationCore core,
        MemberState member,
        Action<string> onMessage,
        Action<string, string>? onUseSuppressionSkill)
    {
        var id = IdentityMigrationService.GetOrCreate(core.State, member);
        var hasSummon = TraitActiveSkillService.HasSkill(id, TraitActiveSkillService.BoardSummonTraitId);
        var hasPlanning = TraitActiveSkillService.HasSkill(id, TraitActiveSkillService.PlanningSupportTraitId);
        if (!hasSummon && !hasPlanning)
        {
            return;
        }

        root.Add(MakeCaption("主动技能"));
        var phase = core.State.phase;
        var summonPhase = phase is GamePhase.COMBAT_PREP or GamePhase.COMBAT;
        var planningPhase = phase is GamePhase.OPERATIONS or GamePhase.COMBAT_PREP;
        if (!summonPhase && hasSummon)
        {
            root.Add(MakeBody("（董事会召来：交战准备/战斗中可发动）"));
        }
        if (!planningPhase && hasPlanning)
        {
            root.Add(MakeBody("（策划支援：运营/交战准备可发动）"));
        }

        if (hasSummon)
        {
            var catalog = TraitCatalog.LoadDefault();
            var summonLabel = DisplayLabels.TraitBilingual(catalog.Find(TraitActiveSkillService.BoardSummonTraitId));
            AddSkillButton(
                root,
                core,
                id,
                TraitActiveSkillService.BoardSummonTraitId,
                summonLabel,
                "战斗中 3AU 跃迁进场 5 艘无畏",
                summonPhase,
                onMessage,
                onUseSuppressionSkill);
        }
        if (hasPlanning)
        {
            var catalog = TraitCatalog.LoadDefault();
            var planningLabel = DisplayLabels.TraitBilingual(catalog.Find(TraitActiveSkillService.PlanningSupportTraitId));
            AddSkillButton(
                root,
                core,
                id,
                TraitActiveSkillService.PlanningSupportTraitId,
                planningLabel,
                "5000 星币揭露团内内鬼",
                planningPhase,
                onMessage,
                onUseSuppressionSkill);
        }
    }

    private static void AddSkillButton(
        VisualElement root,
        SimulationCore core,
        IdentityState id,
        string traitId,
        string label,
        string hint,
        bool phaseOk,
        Action<string> onMessage,
        Action<string, string>? onUse)
    {
        var cd = TraitActiveSkillService.CooldownRoundsRemaining(core.State, id, traitId);
        var ready = phaseOk && cd == 0;
        var row = new VisualElement();
        row.AddToClassList("ops-op-slot-row");
        var btn = new Button { text = label };
        btn.SetEnabled(ready && onUse != null);
        if (ready && onUse != null)
        {
            btn.clicked += () => onUse(traitId, label);
        }
        row.Add(btn);
        var status = cd > 0
            ? hint + " · 冷却 " + cd + " 回合"
            : hint + (ready ? "" : " · 不可用");
        row.Add(MakeBody(status));
        root.Add(row);
    }

    private static void RenderPersonalAssets(VisualElement root, SimulationCore core, MemberState member)
    {
        root.Add(MakeCaption("个人资产"));
        var stock = MemberAssetService.PersonalStock(core.State, member);
        var any = false;
        foreach (var kv in stock)
        {
            if (kv.Value <= 0)
            {
                continue;
            }
            any = true;
            var name = MemberAssetService.ItemDisplayName(kv.Key, core.Modules, core.Ships);
            var value = AssetValuation.FormatStarCoinValue(
                AssetValuation.ItemStarCoinValue(kv.Key, core.Ships, core.Modules));
            root.Add(MakeBody(name + " ×" + kv.Value + " · " + value));
        }
        if (!any)
        {
            root.Add(MakeBody("（个人库存为空）"));
        }
    }

    private static string FormationLabel(GameState state, string? formationId)
    {
        if (formationId == null)
        {
            return "—";
        }
        return FormationService.DisplayName(state, formationId) ?? formationId;
    }

    private static string MemberName(MemberState m) =>
        !string.IsNullOrEmpty(m.name) ? m.name
        : !string.IsNullOrEmpty(m.accountName) ? m.accountName
        : m.memberId ?? "团员";

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
}
