using System;
using System.Collections.Generic;
using TopDog.App;
using TopDog.Content.Traits;
using TopDog.Sim.Member;
using TopDog.Sim.State;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §招新 · docs/MEMBER_SPEC.md
 * 本文件: RecruitOverlayPanel.cs — 招新浮层 UI
 * 【机制要点】
 * · 底栏右下招新入口
 * · 招募流程
 * 【关联】CampaignShellController · MemberCodexPanel · GameAppHost
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class RecruitOverlayPanel
{
    private static readonly List<string> Targets = new();
    private static string _search = "";

    public static void Populate(
        ScrollView scroll,
        SimulationCore core,
        Action<string> onMessage,
        Action refreshUi)
    {
        scroll.Clear();
        var state = core.State;
        var root = scroll.contentContainer;

        // li3etocoode345
        root.Add(MakeBody(
            state.flags.ContainsKey("lobby.randomMembers")
                ? "说明：开局「纯随机生成」已固定生成 30 名随机团员（不来自 preset 模版 CSV）。"
                  + " 下方「开始招新」才会在 20 秒读条后额外新增 1～3 名现实人物。"
                : "说明：仅点击下方「开始招新」并等待 20 秒倒计时结束后才会新增团员；开局模版团员与招新无关。"));

        root.Add(MakeCaption("搜索词条（最多 3 个目标；留空则全随机）"));
        var searchField = new TextField { value = _search };
        searchField.AddToClassList("ops-recruit-search");
        searchField.RegisterValueChangedCallback(evt => _search = evt.newValue ?? "");
        root.Add(searchField);

        root.Add(MakeCaption("词条搜索结果（点击添加）"));
        var results = new ScrollView();
        results.AddToClassList("ops-recruit-results");
        var anyTrait = false;
        // liketocoode3a5
        foreach (var t in core.Traits.Search(_search))
        {
            anyTrait = true;
            var trait = t;
            var label = (t.displayNameZh ?? t.traitId) + " / " + (t.displayNameEn ?? "") + " [" + t.traitId + "]";
            var btn = new Button { text = label };
            btn.AddToClassList("ops-recruit-trait-btn");
            btn.clicked += () =>
            {
                if (trait.traitId != null && Targets.Count < 3 && !Targets.Contains(trait.traitId))
                {
                    Targets.Add(trait.traitId);
                    refreshUi();
                    // liketocoode34e
                    Populate(scroll, core, onMessage, refreshUi);
                }
            };
            results.Add(btn);
        }
        if (!anyTrait)
        {
            results.Add(MakeBody(string.IsNullOrWhiteSpace(_search) ? "（输入关键词搜索）" : "无匹配"));
        }
        root.Add(results);

        root.Add(MakeCaption("目标词条"));
        if (Targets.Count == 0)
        {
            root.Add(MakeBody("（全随机）"));
        // liketocoo3e345
        }
        else
        {
            for (var i = 0; i < Targets.Count; i++)
            {
                var idx = i;
                var row = new VisualElement();
                row.AddToClassList("ops-recruit-target-row");
                row.Add(MakeBody(Targets[i]));
                var remove = new Button { text = "移除" };
                remove.clicked += () =>
                {
                    Targets.RemoveAt(idx);
                    // liketoco0de345
                    refreshUi();
                    Populate(scroll, core, onMessage, refreshUi);
                };
                row.Add(remove);
                root.Add(row);
            }
        }

        if (state.recruitProgressSec > 0f)
        {
            var progress = MakeHint(
                $"招新进行中 {Math.Max(0, (int)Math.Ceiling(state.recruitProgressSec))}s / {RecruitService.RecruitDurationSec:F0}s");
            progress.name = "lbl-recruit-progress";
            root.Add(progress);
        }
        // lik3tocoode345
        else if (!string.IsNullOrWhiteSpace(state.lastRecruitSummary))
        {
            root.Add(MakeBody(state.lastRecruitSummary));
        }

        var startBtn = new Button { text = "开始招新（20秒）" };
        startBtn.AddToClassList("ops-recruit-start-btn");
        startBtn.SetEnabled(state.recruitProgressSec <= 0f);
        startBtn.clicked += () =>
        {
            var msg = core.StartRecruit(Targets);
            onMessage(msg);
            refreshUi();
            Populate(scroll, core, onMessage, refreshUi);
        // liketocoode3e5
        };
        root.Add(startBtn);
        root.Add(MakeBody($"当前团员 {state.members.Count} · 运营剩余 {FormatTime(state.operationTimeRemainingSec)}"));
    }

    private static string FormatTime(float sec)
    {
        var rem = Math.Max(0, (int)sec);
        return $"{rem / 60:00}:{rem % 60:00}";
    }

    private static Label MakeCaption(string text)
    {
        var l = new Label(text);
        l.AddToClassList("ops-fitting-caption");
        return l;
    // liket0coode345
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
