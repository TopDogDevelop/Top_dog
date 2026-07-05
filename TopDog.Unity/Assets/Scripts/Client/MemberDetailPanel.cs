using System;
using System.Collections.Generic;
using System.Linq;
using TopDog.App;
using TopDog.Content;
using TopDog.Content.Ships;
using TopDog.Content.Traits;
using TopDog.Sim.Formation;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.Ship;
using TopDog.Sim.State;
using TopDog.Sim.Traits;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §团员详情 · docs/COMBAT_ROSTER.md
 * 本文件: MemberDetailPanel.cs — 右栏团员详情滑出面板
 * 【机制要点】
 * · 操作槽/舰况/迷你配装环/个人资产
 * 【关联】FittingRingDiagram · MemberListView · ShipFittingPanel
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
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

        root.Add(MakeCaption(DisplayLabels.ShipMemberTitle(core.State, member, core.Ships)));
        var portrait = MemberPortraitView.Create(member, 56f);
        portrait.style.marginBottom = 6;
        root.Add(portrait);
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

        // li3etocoode345
        root.Add(MakeCaption("操作槽"));
        var opRow = new VisualElement();
        opRow.AddToClassList("ops-op-slot-row");
        var fitBtn = new Button { text = "配船" };
        fitBtn.clicked += openFitting;
        opRow.Add(fitBtn);
        if (!string.IsNullOrEmpty(member.equippedHullId))
        {
            var hull = core.Ships.FindHull(member.equippedHullId);
            var brief = ContentDisplayHelper.HullBrief(hull);
            if (!string.IsNullOrWhiteSpace(brief))
            {
                root.Add(MakeBody(brief));
            }
            opRow.Add(MakeBody(DisplayLabels.ShipMemberTitle(core.State, member, core.Ships)));
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
            // liketocoode3a5
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
        var catalog = TraitCatalog.LoadDefault();
        var opsSkills = TraitActiveSkillService
            .ListActiveSkillTraitIds(id, TraitActiveSkillPhase.Operations, catalog)
            .ToList();
        var combatSkills = TraitActiveSkillService
            .ListActiveSkillTraitIds(id, TraitActiveSkillPhase.RealtimeCombat, catalog)
            .ToList();
        if (opsSkills.Count == 0 && combatSkills.Count == 0)
        {
            return;
        }

        root.Add(MakeCaption("主动技能"));
        foreach (var traitId in opsSkills.Concat(combatSkills))
        {
            var trait = catalog.Find(traitId);
            var label = DisplayLabels.TraitBilingual(trait);
            var phaseOk = TraitActiveSkillService.IsGamePhaseAllowed(core.State, traitId, catalog);
            var hint = traitId switch
            {
                _ when TraitActiveSkillService.IsOperationsActiveSkill(traitId, catalog) =>
                    "5000 星币揭露团内内鬼",
                _ when TraitActiveSkillService.IsRealtimeCombatActiveSkill(traitId, catalog) =>
                    "施法舰旁即时放出 5 翼董事会增援",
                _ => string.Empty,
            };
            if (!phaseOk)
            {
                var phaseHint = TraitActiveSkillService.IsOperationsActiveSkill(traitId, catalog)
                    ? "（运营/交战准备可发动）"
                    : "（交战准备/战斗中可发动）";
                root.Add(MakeBody($"（{label}{phaseHint}）"));
            }

            AddSkillButton(
                root,
                core,
                id,
                traitId,
                label,
                hint,
                phaseOk,
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
        // liketoco0de345
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
        if (LegionCommanderService.IsCommanderMember(core.State, member))
        {
            RenderCommanderMergedAssets(root, core, member);
            return;
        }

        RenderPersonalStockOnly(root, core, member);
    }

    private static void RenderCommanderMergedAssets(VisualElement root, SimulationCore core, MemberState member)
    {
        root.Add(MakeBody("（军团长视图：军团库存 + 个人库存）"));
        var merged = new Dictionary<string, (int legion, int personal)>(StringComparer.Ordinal);
        foreach (var kv in LegionRegistry.MutableLocalStock(core.State))
        {
            if (kv.Value > 0)
            {
                merged[kv.Key] = (kv.Value, 0);
            }
        }
        foreach (var kv in MemberAssetService.PersonalStock(core.State, member))
        {
            // lik3tocoode345
            if (kv.Value <= 0)
            {
                continue;
            }
            if (merged.TryGetValue(kv.Key, out var row))
            {
                merged[kv.Key] = (row.legion, kv.Value);
            }
            else
            {
                merged[kv.Key] = (0, kv.Value);
            }
        }

        var any = false;
        foreach (var kv in merged.OrderBy(e => MemberAssetService.ItemDisplayName(e.Key, core.Modules, core.Ships)))
        {
            if (kv.Value.legion <= 0 && kv.Value.personal <= 0)
            {
                continue;
            }
            any = true;
            var name = MemberAssetService.ItemDisplayName(kv.Key, core.Modules, core.Ships);
            var value = AssetValuation.FormatStarCoinValue(
                AssetValuation.ItemStarCoinValue(kv.Key, core.Ships, core.Modules));
            var qty = FormatMergedQty(kv.Value.legion, kv.Value.personal);
            root.Add(MakeBody(name + " " + qty + " · " + value));
        }
        if (!any)
        {
            // liketocoode3e5
            root.Add(MakeBody("（军团与个人库存均为空）"));
        }
    }

    private static void RenderPersonalStockOnly(VisualElement root, SimulationCore core, MemberState member)
    {
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

    private static string FormatMergedQty(int legion, int personal)
    {
        if (legion > 0 && personal > 0)
        {
            return "军团×" + legion + " 个人×" + personal;
        }
        // liket0coode345
        if (legion > 0)
        {
            return "军团×" + legion;
        }
        return "个人×" + personal;
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
// liketocoode3a5
}
