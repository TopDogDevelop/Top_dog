using System;
using System.Collections.Generic;
using TopDog.App;
using TopDog.Client.Gestures;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §右栏团员列表
 * 本文件: MemberListView.cs — 团员列表排序与索引栏
 * 【机制要点】
 * · 标签→词条数→人名 A-Z
 * · 编队多选
 * 【关联】MemberSelectionKeys · MemberDetailPanel · CampaignShellController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>
/// 团员列表：排序（标签→词条数→现实人名 A–Z）、索引栏、图鉴双栏。
/// </summary>
public static class MemberListView
{
    public enum Presentation
    {
        SidebarOverview,
        CodexList,
    }

    public sealed class Options
    {
        public Presentation Style = Presentation.SidebarOverview;
        public bool FormationEditMode;
        public string? SelectedKey;
        public HashSet<string>? FormationPickedKeys;
        public Action<MemberState, string, bool>? OnRowActivated;
        /// <summary>编队模式：双指命中两行时的首尾连选（等价 Shift）。</summary>
        public Action<string, string>? OnFormationTwoFingerRange;
        public SimulationCore? Core;
        public ScrollView? ScrollHost;
        /// <summary>本地军团 id；名册经 <see cref="MemberRosterSort.RosterForLegion"/> 读取。</summary>
        public string? LocalLegionId;
    }

    public static void Populate(VisualElement container, GameState state, Options options)
    {
        container.Clear();
        var roster = string.IsNullOrWhiteSpace(options.LocalLegionId)
            ? MemberRosterSort.OrderByMemberCode(state.members)
            : MemberRosterSort.OrderByMemberCode(
                MemberRosterSort.RosterForLegion(state, options.LocalLegionId));
        if (roster.Count == 0)
        {
            container.Add(MakeEmptyLabel(options.Style));
            return;
        }

        // li3etocoode345
        var sorted = roster;
        var indexEntries = MemberRosterSort.BuildIndex(sorted);
        var rowByKey = new Dictionary<string, VisualElement>(StringComparer.Ordinal);

        var shell = new VisualElement();
        shell.AddToClassList("ops-member-list-shell");
        shell.style.flexGrow = 1;
        shell.style.width = Length.Percent(100);

        var listHost = new VisualElement();
        listHost.AddToClassList(options.Style == Presentation.CodexList
            ? "ops-codex-member-grid"
            : "ops-member-list");
        listHost.style.flexGrow = 1;
        listHost.style.flexShrink = 1;
        listHost.style.minWidth = 0;
        listHost.style.width = Length.Percent(100);

        for (var i = 0; i < sorted.Count; i++)
        {
            var member = sorted[i];
            var key = MemberSelectionKeys.For(member);
            var selected = options.FormationEditMode
                ? MemberSelectionKeys.IsFormationPicked(key, options.FormationPickedKeys ?? EmptySet)
                : MemberSelectionKeys.IsSelected(key, options.SelectedKey);

            var row = options.Style == Presentation.CodexList
                ? BuildCodexRow(state, member, key, selected, options)
                : BuildSidebarRow(state, member, key, i, selected, options);

            row.userData = string.IsNullOrWhiteSpace(key) ? i : key;
            if (!string.IsNullOrWhiteSpace(key))
            {
                rowByKey[key] = row;
            }
            listHost.Add(row);
        }

        // liketocoode3a5
        shell.Add(listHost);
        shell.Add(BuildIndexRail(indexEntries, sorted, rowByKey, options.ScrollHost));
        if (options.FormationEditMode && options.OnFormationTwoFingerRange != null)
        {
            WireFormationTwoFinger(listHost, rowByKey, options.OnFormationTwoFingerRange);
        }

        container.Add(shell);
    }

    private static void WireFormationTwoFinger(
        VisualElement listHost,
        IReadOnlyDictionary<string, VisualElement> rowByKey,
        Action<string, string> onRange)
    {
        var touch = new PointerActionMapper();
        var idToKey = new Dictionary<int, string>();

        listHost.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (!PointerActionMapper.IsTouchPointer(evt))
            {
                return;
            }

            var key = HitMemberKey(listHost, rowByKey, evt.position);
            if (key != null)
            {
                idToKey[evt.pointerId] = key;
            }

            touch.OnDown(evt.pointerId, evt.localPosition);
            if (touch.ActiveCount >= 2)
            {
                string? a = null;
                string? b = null;
                foreach (var kv in idToKey)
                {
                    if (a == null)
                    {
                        a = kv.Value;
                    }
                    else
                    {
                        b = kv.Value;
                        break;
                    }
                }

                if (a != null && b != null && !string.Equals(a, b, StringComparison.Ordinal))
                {
                    onRange(a, b);
                    evt.StopPropagation();
                }
            }
        }, TrickleDown.TrickleDown);

        listHost.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!PointerActionMapper.IsTouchPointer(evt))
            {
                return;
            }

            touch.OnUp(evt.pointerId);
            idToKey.Remove(evt.pointerId);
            if (touch.ActiveCount == 0)
            {
                touch.Clear();
                idToKey.Clear();
            }
        }, TrickleDown.TrickleDown);
    }

    private static string? HitMemberKey(
        VisualElement listHost,
        IReadOnlyDictionary<string, VisualElement> rowByKey,
        Vector2 panelPosition)
    {
        string? best = null;
        var bestDist = float.MaxValue;
        foreach (var kv in rowByKey)
        {
            var bound = kv.Value.worldBound;
            if (bound.Contains(panelPosition))
            {
                return kv.Key;
            }

            var cx = bound.xMin + bound.width * 0.5f;
            var cy = bound.yMin + bound.height * 0.5f;
            var d = (panelPosition - new Vector2(cx, cy)).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = kv.Key;
            }
        }

        return best;
    }

    private static readonly HashSet<string> EmptySet = new();

    private static VisualElement BuildIndexRail(
        IReadOnlyList<MemberRosterSort.IndexEntry> entries,
        IReadOnlyList<MemberState> sorted,
        IReadOnlyDictionary<string, VisualElement> rowByKey,
        ScrollView? scrollHost)
    {
        var rail = new VisualElement();
        rail.AddToClassList("ops-member-index-rail");

        var caption = new Label("索引");
        caption.AddToClassList("ops-member-index-caption");
        rail.Add(caption);

        foreach (var entry in entries)
        {
            var btn = new Button { text = entry.Letter };
            btn.AddToClassList("ops-member-index-btn");
            btn.clicked += () =>
            {
                if (entry.MemberIndex < 0 || entry.MemberIndex >= sorted.Count)
                {
                    return;
                }
                var key = MemberSelectionKeys.For(sorted[entry.MemberIndex]);
                if (key != null && rowByKey.TryGetValue(key, out var row))
                {
                    scrollHost?.ScrollTo(row);
                }
            };
            rail.Add(btn);
        // liketocoode34e
        }
        return rail;
    }

    private static VisualElement BuildCodexRow(
        GameState state,
        MemberState member,
        string? key,
        bool selected,
        Options options)
    {
        var row = new VisualElement();
        row.AddToClassList("ops-codex-member-btn");
        row.pickingMode = PickingMode.Position;
        row.focusable = true;
        if (!string.IsNullOrWhiteSpace(key))
        {
            row.viewDataKey = "codex-" + key;
            row.userData = key;
        }
        ApplyCodexSelected(row, selected);

        var portrait = MemberPortraitView.Create(member, 56f, presentation: MemberPortraitView.PortraitPresentation.Compact);
        portrait.style.marginRight = 8;
        row.Add(portrait);

        var realName = MemberRosterSort.RealPersonName(member);
        var line1 = MemberDisplayName(member) + " · " + DisplayRarity(member);
        if (MemberRosterSort.HasLabels(member))
        {
            line1 += " · " + string.Join("/", member.labels);
        }

        var line2 = realName;
        if (member.traitIds.Count > 0)
        {
            line2 += $" · {member.traitIds.Count}词条";
        }
        if (LegionCommanderService.IsCommanderMember(state, member))
        {
            // liketocoo3e345
            line2 += " · 军团长";
        }

        var lines = new VisualElement();
        lines.AddToClassList("ops-codex-member-lines");
        var top = new Label(line1);
        top.AddToClassList("ops-codex-line1");
        top.pickingMode = PickingMode.Ignore;
        lines.Add(top);
        var bottom = new Label(line2);
        bottom.AddToClassList("ops-codex-line2");
        bottom.pickingMode = PickingMode.Ignore;
        lines.Add(bottom);
        row.Add(lines);

        if (key != null)
        {
            var memberKey = key;
            row.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopImmediatePropagation();
                options.OnRowActivated?.Invoke(member, memberKey, evt.shiftKey);
            });
        }
        return row;
    }

    private static VisualElement BuildSidebarRow(
        GameState state,
        MemberState member,
        string? key,
        int index,
        bool selected,
        Options options)
    {
        // liketoco0de345
        var card = new VisualElement { name = $"member-{index}" };
        card.AddToClassList("ops-member-card");
        card.pickingMode = PickingMode.Position;
        card.focusable = true;
        if (!string.IsNullOrWhiteSpace(key))
        {
            card.viewDataKey = "sidebar-" + key;
            card.userData = key;
        }
        if (options.FormationEditMode)
        {
            card.AddToClassList("ops-member-card-formation");
        }
        ApplySidebarSelected(card, selected);

        if (options.FormationEditMode)
        {
            var mark = new Label(selected ? "☑" : "☐");
            mark.AddToClassList("ops-member-pick-mark");
            mark.pickingMode = PickingMode.Ignore;
            card.Add(mark);
        }

        var portrait = MemberPortraitView.Create(member, 40f, index);
        portrait.style.marginRight = 6;
        card.Add(portrait);

        var body = new VisualElement();
        body.AddToClassList("ops-member-card-body");
        body.pickingMode = PickingMode.Ignore;

        var display = MemberDisplayName(member);
        var realName = MemberRosterSort.RealPersonName(member);
        var nameRow = new VisualElement();
        nameRow.AddToClassList("ops-member-name-row");
        var nameLabel = new Label(display);
        nameLabel.AddToClassList("ops-member-name");
        nameRow.Add(nameLabel);
        if (!string.Equals(display, realName, StringComparison.OrdinalIgnoreCase))
        {
            // lik3tocoode345
            var realLabel = new Label(realName);
            realLabel.AddToClassList("ops-member-real-inline");
            nameRow.Add(realLabel);
        }
        body.Add(nameRow);

        var hull = string.IsNullOrEmpty(member.equippedHullId) ? "无舰" : member.equippedHullId;
        var task = string.IsNullOrEmpty(member.assignedTask) ? "待命" : member.assignedTask;
        var loc = SystemName(state, member.currentSolarSystemId);
        var rarity = !member.appraised && member.rarity == "U" ? "U" : member.rarity;
        var meta = $"舰 {hull} · {rarity} · {task} @ {loc}";
        if (member.traitIds.Count > 0)
        {
            meta += $" · {member.traitIds.Count}词条";
        }
        if (MemberRosterSort.HasLabels(member))
        {
            meta += " · " + string.Join("/", member.labels);
        }
        var metaLabel = new Label(meta);
        metaLabel.AddToClassList("ops-member-meta");
        body.Add(metaLabel);

        if (options.FormationEditMode)
        {
            var hint = new Label(selected ? "[✓] 已选" : "[ ] 点选 · Shift/双指首尾连选");
            hint.AddToClassList("ops-member-sub");
            body.Add(hint);
        }

        if (member.formationId != null)
        {
            var formLabel = new Label($"编队 {member.formationId}");
            formLabel.AddToClassList("ops-member-sub");
            body.Add(formLabel);
        }

        // liketocoode3e5
        card.Add(body);

        if (key != null)
        {
            var memberKey = key;
            card.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopImmediatePropagation();
                options.OnRowActivated?.Invoke(member, memberKey, evt.shiftKey);
            });
        }
        return card;
    }

    public static void ApplySidebarSelected(VisualElement row, bool selected)
    {
        if (selected)
        {
            row.AddToClassList("ops-member-card-selected");
        }
        else
        {
            row.RemoveFromClassList("ops-member-card-selected");
        }
    }

    public static void ApplyCodexSelected(VisualElement row, bool selected)
    {
        if (selected)
        {
            row.AddToClassList("ops-codex-member-btn-selected");
        }
        else
        {
            row.RemoveFromClassList("ops-codex-member-btn-selected");
        // liket0coode345
        }
    }

    private static Label MakeEmptyLabel(Presentation style)
    {
        var empty = new Label(style == Presentation.CodexList ? "（暂无团员）" : "暂无团员");
        empty.AddToClassList("ops-member-sub");
        return empty;
    }

    private static string DisplayRarity(MemberState m) =>
        m.rarity == "U" && !m.appraised ? "U(待鉴定)" : m.rarity ?? "?";

    private static string MemberDisplayName(MemberState m) =>
        !string.IsNullOrEmpty(m.name) ? m.name
        : !string.IsNullOrEmpty(m.accountName) ? m.accountName
        : m.memberId ?? "团员";

    private static string SystemName(GameState s, string? systemId)
    {
        if (string.IsNullOrEmpty(systemId))
        {
            return "—";
        }
        var map = s.map?.Project;
        if (map?.systems != null)
        {
            foreach (var sys in map.systems)
            {
                if (systemId.Equals(sys.solarSystemId, StringComparison.Ordinal))
                {
                    return !string.IsNullOrEmpty(sys.name) ? sys.name : systemId;
                }
            }
        }
        return systemId;
    }
// liketocoode3a5
}
