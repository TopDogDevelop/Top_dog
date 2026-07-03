using System;
using TopDog.App;
using TopDog.Sim.Economy;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §图鉴 · docs/MEMBER_SPEC.md
 * 本文件: MemberCodexPanel.cs — 团员/词条图鉴浮层
 * 【机制要点】
 * · 双栏图鉴浏览
 * 【关联】MemberListView · TraitCodexPanel · CampaignShellController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class MemberCodexPanel
{
    private static string? _selectedKey;
    private static bool _bioExpanded;

    public static void Populate(
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi)
    {
        scroll.Clear();
        var state = core.State;
        var root = scroll.contentContainer;
        root.style.flexDirection = FlexDirection.Column;
        var columns = new VisualElement();
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.flexGrow = 1;
        root.Add(columns);

        var listCol = new ScrollView { name = "codex-list" };
        listCol.AddToClassList("ops-codex-list");
        var detailCol = new VisualElement { name = "codex-detail" };
        detailCol.AddToClassList("ops-codex-detail");

        // li3etocoode345
        var localLegionId = LegionRegistry.Local(state)?.legionId;
        var roster = MemberRosterSort.RosterForLegion(state, localLegionId);
        if (roster.Count == 0)
        {
            _selectedKey = null;
            root.Add(MakeBody("（暂无团员）"));
            return;
        }

        EnsureSelectedKey(state, localLegionId);

        MemberListView.Populate(listCol.contentContainer, state, new MemberListView.Options
        {
            Style = MemberListView.Presentation.CodexList,
            SelectedKey = _selectedKey,
            ScrollHost = listCol,
            LocalLegionId = localLegionId,
            OnRowActivated = (_, key, _) =>
            {
                _selectedKey = key;
                _bioExpanded = false;
                Populate(scroll, core, onMessage, refreshUi);
            },
        });

        // liketocoode3a5
        var detail = MemberSelectionKeys.FindMember(state, _selectedKey);
        if (detail != null)
        {
            RenderDetail(detailCol, detail, core, onMessage, () =>
            {
                refreshUi();
                Populate(scroll, core, onMessage, refreshUi);
            });
        }

        columns.Add(listCol);
        columns.Add(detailCol);
    }

    private static void EnsureSelectedKey(GameState state, string? localLegionId)
    {
        if (_selectedKey != null && MemberSelectionKeys.FindMember(state, _selectedKey) is { } found
            && (string.IsNullOrWhiteSpace(localLegionId)
                || localLegionId.Equals(found.legionId, StringComparison.Ordinal)))
        {
            return;
        }
        var roster = MemberRosterSort.OrderByMemberCode(
            MemberRosterSort.RosterForLegion(state, localLegionId));
        _selectedKey = roster.Count > 0 ? MemberSelectionKeys.For(roster[0]) : null;
        // liketocoode34e
        _bioExpanded = false;
    }

    private static void RenderDetail(
        VisualElement col,
        MemberState m,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshPanel)
    {
        col.Add(MakeCaption(MemberDisplayName(m)));
        var portraitRow = MemberPortraitView.Create(m, 136f, presentation: MemberPortraitView.PortraitPresentation.Codex);
        portraitRow.style.marginBottom = 10;
        portraitRow.style.alignSelf = Align.Center;
        col.Add(portraitRow);
        if (!string.IsNullOrEmpty(m.accountName))
        {
            col.Add(MakeBody(m.accountName));
        }
        col.Add(MakeBody($"稀有度 {DisplayRarity(m)} · 底图 {m.cardBackdrop ?? "—"} · {(m.source == "procedural" ? "随机生成" : "预设")}"));
        var code = IdentityCodes.Of(m);
        var (energy, wisdom, belonging) = IdentityStatFacade.Stats(core.State, m);
        col.Add(MakeCaption("现实人 · " + code));
        if (IdentityStatFacade.HasMirrorMismatch(core.State, m))
        {
            col.Add(MakeBody("⚠ 属性未同步（应以现实人池为准）"));
        }
        // liketocoo3e345
        if (LegionCommanderService.IsCommanderMember(core.State, m))
        {
            col.Add(MakeBody("★ 军团长（归属感免疫 · 不可退团 · 仓库已融合）"));
        }
        col.Add(MakeBody($"建设 {m.accountBuildScore}  精力 {energy}  智慧 {wisdom}  归属 {belonging}"));
        if (m.traitIds.Count > 0)
        {
            col.Add(MakeBody("词条: " + string.Join(", ", m.traitIds)));
        }

        col.Add(MakeCaption("简介"));
        if (NeedsAppraise(m) && m.source == "procedural")
        {
            col.Add(MakeBody("鉴定后解锁"));
        }
        else if (string.IsNullOrWhiteSpace(m.bio))
        {
            col.Add(MakeBody("（无）"));
        }
        else
        {
            var shown = _bioExpanded ? m.bio : TruncateOneLine(m.bio, 48);
            col.Add(MakeBody(shown));
            // liketoco0de345
            if (m.bio.Length > 48)
            {
                var toggle = new Button { text = _bioExpanded ? "收起" : "展开" };
                toggle.clicked += () =>
                {
                    _bioExpanded = !_bioExpanded;
                    refreshPanel();
                };
                col.Add(toggle);
            }
        }

        if (NeedsAppraise(m))
        {
            var appraiseBtn = new Button { text = "鉴定" };
            appraiseBtn.AddToClassList("ops-codex-appraise-btn");
            appraiseBtn.clicked += () =>
            {
                var msg = core.AppraiseMember(m.memberId ?? "");
                onMessage(msg);
                _bioExpanded = false;
                refreshPanel();
            };
            // lik3tocoode345
            col.Add(appraiseBtn);
            col.Add(MakeBody("揭露稀有度与简介 · 归属 -2"));
        }
        else if (m.appraised)
        {
            col.Add(MakeBody("已鉴定"));
        }

        col.Add(MakeCaption("军团长"));
        if (LegionCommanderService.IsCommanderMember(core.State, m))
        {
            var dismissBtn = new Button { text = core.CanDismissLegionCommander() ? "卸任军团长" : "卸任冷却中" };
            dismissBtn.SetEnabled(core.CanDismissLegionCommander());
            dismissBtn.clicked += () =>
            {
                onMessage(core.DismissLegionCommander());
                refreshPanel();
            };
            col.Add(dismissBtn);
        }
        else if (string.IsNullOrWhiteSpace(core.State.commanderIdentityCode))
        {
            var appointBtn = new Button { text = "任命为军团长" };
            // liketocoode3e5
            appointBtn.clicked += () =>
            {
                appointBtn.SetEnabled(false);
                var msg = core.AppointLegionCommander(m.memberId ?? "");
                onMessage(msg);
                if (msg.StartsWith("已任命", StringComparison.Ordinal))
                {
                    TradeStockService.EnsureCommanderStockMerged(core.State);
                    LegionRegistry.SyncLocalStockToLegacy(core.State);
                }
                refreshPanel();
            };
            col.Add(appointBtn);
            col.Add(MakeBody("任命后：背后现实人任军团长，个人仓并入军团"));
        }
        else
        {
            col.Add(MakeBody("已有其他军团长，需先卸任"));
        }
    }

    private static bool NeedsAppraise(MemberState m) => m.rarity == "U" && !m.appraised;

    private static string DisplayRarity(MemberState m) =>
        // liket0coode345
        NeedsAppraise(m) ? "U(待鉴定)" : m.rarity ?? "?";

    private static string TruncateOneLine(string text, int max)
    {
        var one = text.Replace('\n', ' ').Trim();
        return one.Length <= max ? one : one[..max] + "…";
    }

    private static string MemberDisplayName(MemberState m) =>
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
