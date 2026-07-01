using System;
using TopDog.App;
using TopDog.Content;
using TopDog.Lobby;
using TopDog.Sim.Legion;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>约战名册：运营同款团员图鉴双栏（列表 + 摘要）。</summary>
public static class SkirmishRosterCodexPanel
{
    public static void Populate(
        ScrollView scroll,
        SkirmishLobbyPrepCore prep,
        string? selectedMemberId,
        Action<string?> onSelectMemberId,
        Action<string> onMessage,
        Action refreshUi,
        Action<string?> onRemoveMember)
    {
        scroll.Clear();
        var state = prep.Core.State;
        var root = scroll.contentContainer;
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexGrow = 1;

        if (state.members.Count == 0)
        {
            root.Add(MakeBody("（暂无上场团员 — 从模版添员）"));
            return;
        }

        var columns = new VisualElement();
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.flexGrow = 1;
        columns.style.minHeight = 0;

        var listCol = new ScrollView { name = "skirmish-codex-list" };
        listCol.AddToClassList("ops-codex-list");
        listCol.style.flexGrow = 1;
        listCol.style.minWidth = 0;

        var detailCol = new VisualElement { name = "skirmish-codex-detail" };
        detailCol.AddToClassList("ops-codex-detail");
        detailCol.style.flexGrow = 1;
        detailCol.style.minWidth = 0;

        var selectedKey = selectedMemberId;
        if (selectedKey == null || MemberSelectionKeys.FindMember(state, selectedKey) == null)
        {
            selectedKey = MemberSelectionKeys.For(state.members[0]);
            onSelectMemberId(selectedKey);
        }

        MemberListView.Populate(listCol.contentContainer, state, new MemberListView.Options
        {
            Style = MemberListView.Presentation.CodexList,
            SelectedKey = selectedKey,
            ScrollHost = listCol,
            LocalLegionId = prep.LocalLegionId,
            OnRowActivated = (_, key, _) =>
            {
                onSelectMemberId(key);
                refreshUi();
            },
        });

        var detail = MemberSelectionKeys.FindMember(state, selectedKey);
        if (detail != null)
        {
            RenderDetail(detailCol, state, detail, prep, onMessage, onRemoveMember, refreshUi);
        }

        columns.Add(listCol);
        columns.Add(detailCol);
        root.Add(columns);
    }

    private static void RenderDetail(
        VisualElement col,
        GameState state,
        MemberState member,
        SkirmishLobbyPrepCore prep,
        Action<string> onMessage,
        Action<string?> onRemoveMember,
        Action refreshUi)
    {
        col.Add(MakeCaption(MemberDisplayName(member)));
        if (!string.IsNullOrEmpty(member.accountName))
        {
            col.Add(MakeBody(member.accountName));
        }

        col.Add(MakeBody($"稀有度 {member.rarity ?? "?"} · {(member.source == "procedural" ? "随机生成" : "预设")}"));
        col.Add(MakeBody("现实人 · " + IdentityCodes.Of(member) + "-" + (member.accountSuffix ?? "??")));

        if (member.traitIds.Count > 0)
        {
            col.Add(MakeBody("词条: " + string.Join(", ", member.traitIds)));
        }

        if (!string.IsNullOrWhiteSpace(member.bio))
        {
            col.Add(MakeCaption("简介"));
            col.Add(MakeBody(member.bio));
        }

        var hullId = member.equippedHullId;
        if (!string.IsNullOrWhiteSpace(hullId))
        {
            var hull = prep.Core.Ships.FindHull(hullId);
            col.Add(MakeCaption("当前舰体"));
            col.Add(MakeBody(hull != null ? DisplayLabels.HullBilingual(hull) : hullId));
        }
        else
        {
            col.Add(MakeBody("未配舰 — 在右侧圆环配船区选择舰体"));
        }

        var removeBtn = new Button { text = "移出名册" };
        removeBtn.AddToClassList("ops-codex-appraise-btn");
        var memberId = member.memberId;
        removeBtn.clicked += () =>
        {
            onRemoveMember(memberId);
            onMessage("已移出 " + MemberDisplayName(member));
            refreshUi();
        };
        col.Add(removeBtn);
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
        l.style.whiteSpace = WhiteSpace.Normal;
        return l;
    }
}
