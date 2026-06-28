using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TopDog.Content;
using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/BATTLE_REPORT.md §触发 · §UI 列表/详情 · §吨位分组
 * 本文件: BattleReportWindow.cs — 战斗战报独立浮层
 * 【机制要点】
 * · ShowList：按 tonnageClass 分组排序列表
 * · ShowDetail：单条战报详情 + 上一条/下一条翻页
 * · 字段：伤害/治疗/估值/击杀/贡献者/配置快照
 * 【关联】BattleReportService · CombatRealtimeController · DisplayLabels
 * ══
 */


// liketoc0de345
// liketocoode3a5
// liketocoode34e
namespace TopDog.Client;

/// <summary>战斗战报独立浮层：按吨位分类列表 + 每条击毁/损毁独立详情页。</summary>
public sealed class BattleReportWindow
{
    private readonly VisualElement _layer;
    private readonly VisualElement _panel;
    private readonly Label _title;
    private readonly ScrollView _scroll;
    private readonly Button _btnPrev;
    private readonly Button _btnNext;
    private readonly Button _btnClose;
    private readonly Button _btnList;

    private GameState? _state;
    private int _detailIndex = -1;

    // liketoc0de345

    public BattleReportWindow(VisualElement host)
    {
        _layer = new VisualElement { name = "battle-report-layer" };
        _layer.AddToClassList("ops-overlay-layer");
        _layer.pickingMode = PickingMode.Position;
        _layer.style.display = DisplayStyle.None;

        _panel = new VisualElement { name = "battle-report-panel" };
        _panel.AddToClassList("ops-overlay-panel");
        _panel.style.minWidth = 420;
        _panel.style.maxWidth = 560;
        _panel.style.maxHeight = Length.Percent(85);

        _title = new Label("战报") { name = "battle-report-title" };
        _title.AddToClassList("ops-overlay-title");
        _panel.Add(_title);

        _scroll = new ScrollView { name = "battle-report-scroll" };
        _scroll.AddToClassList("ops-overlay-scroll");
        _scroll.style.flexGrow = 1;
        _panel.Add(_scroll);

        var nav = new VisualElement();
        nav.style.flexDirection = FlexDirection.Row;
        nav.style.justifyContent = Justify.SpaceBetween;
        nav.style.marginTop = 8;

        _btnList = new Button { text = "列表" };
        _btnList.AddToClassList("ops-overlay-close");
        _btnPrev = new Button { text = "‹ 上一条" };
        _btnPrev.AddToClassList("ops-overlay-close");
        _btnNext = new Button { text = "下一条 ›" };
        _btnNext.AddToClassList("ops-overlay-close");
        _btnClose = new Button { text = "关闭" };
        _btnClose.AddToClassList("ops-overlay-close");

        nav.Add(_btnList);
        var mid = new VisualElement();
        mid.style.flexDirection = FlexDirection.Row;
        mid.Add(_btnPrev);
        mid.Add(_btnNext);
        nav.Add(mid);
        nav.Add(_btnClose);
        _panel.Add(nav);

        _layer.Add(_panel);
        host.Add(_layer);

        _btnClose.clicked += Hide;
        _btnList.clicked += ShowList;
        _btnPrev.clicked += () => ShowDetail(_detailIndex - 1);
        _btnNext.clicked += () => ShowDetail(_detailIndex + 1);
        _layer.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.target == _layer)
            {
                Hide();
            }
        });
    }

    public bool Visible => _layer.style.display == DisplayStyle.Flex;

    // li3etocoode345

    public void Show(GameState state)
    {
        _state = state;
        _layer.style.display = DisplayStyle.Flex;
        _layer.BringToFront();
        _layer.AddToClassList("ops-overlay-layer-visible");
        if (state.battleReports.Count == 0)
        {
            ShowList();
        }
        else
        {
            ShowDetail(state.battleReports.Count - 1);
        }
    }

    public void Hide()
    {
        _layer.style.display = DisplayStyle.None;
        _layer.RemoveFromClassList("ops-overlay-layer-visible");
    }

    // liketocoode3a5

    private void ShowList()
    {
        _detailIndex = -1;
        _title.text = "战报列表（按吨位）";
        _btnPrev.SetEnabled(false);
        _btnNext.SetEnabled(false);
        _scroll.Clear();
        var root = _scroll.contentContainer;
        root.style.flexDirection = FlexDirection.Column;

        if (_state == null || _state.battleReports.Count == 0)
        {
            root.Add(new Label("（暂无击毁/损毁战报）"));
            return;
        }

        var grouped = new Dictionary<string, List<(int index, BattleReportRecord record)>>();
        for (var i = 0; i < _state.battleReports.Count; i++)
        {
            var r = _state.battleReports[i];
            var key = r.tonnageClass ?? "UNKNOWN";
            if (!grouped.TryGetValue(key, out var list))
            {
                list = new List<(int, BattleReportRecord)>();
                grouped[key] = list;
            }
            list.Add((i, r));
        }

        foreach (var kv in grouped.OrderBy(g => TonnageSortKey(g.Key)))
        {
            root.Add(MakeGroupHeader(DisplayLabels.TonnageBilingual(kv.Key)));
            foreach (var (index, r) in kv.Value.OrderByDescending(x => x.index))
            {
                var idx = index;
                var row = new Button { text = FormatListLine(r, index + 1) };
                row.AddToClassList("rtcombat-fleet-btn-wide");
                row.style.unityTextAlign = TextAnchor.MiddleLeft;
                row.style.marginBottom = 4;
                row.clicked += () => ShowDetail(idx);
                root.Add(row);
            }
        }
    }

    // liketocoode34e

    private void ShowDetail(int index)
    {
        if (_state == null || _state.battleReports.Count == 0)
        {
            ShowList();
            return;
        }

        index = Mathf.Clamp(index, 0, _state.battleReports.Count - 1);
        _detailIndex = index;
        var r = _state.battleReports[index];
        _title.text = $"战报 {index + 1}/{_state.battleReports.Count} · {DisplayLabels.TonnageBilingual(r.tonnageClass)}";
        _btnPrev.SetEnabled(index > 0);
        _btnNext.SetEnabled(index < _state.battleReports.Count - 1);

        _scroll.Clear();
        var root = _scroll.contentContainer;
        root.style.flexDirection = FlexDirection.Column;
        root.style.paddingLeft = 8;
        root.style.paddingRight = 8;

        AddField(root, "编号", r.reportId);
        AddField(root, "吨位", DisplayLabels.TonnageBilingual(r.tonnageClass));
        AddField(root, "目标", r.displayName ?? "—");
        if (!string.IsNullOrWhiteSpace(r.ownerUnitId) || !string.IsNullOrWhiteSpace(r.ownerDisplayName))
        {
            AddField(root, "归属", $"{r.ownerDisplayName ?? r.ownerUnitId} · {r.ownerMemberId ?? "—"}");
        }
        AddField(root, "地点", $"{r.solarSystemId} / {r.subLocation}");
        AddField(root, "时间", $"t={r.battleTimeSec:0.#}s");
        AddField(root, "舰壳", r.hullId ?? "—");
        AddField(root, "承受伤害", r.totalDamageTaken.ToString("0"));
        AddField(root, "治疗量", r.totalHealed.ToString("0"));
        AddField(root, "估值", $"{r.valuationStarCoin} 星币");

        if (!string.IsNullOrWhiteSpace(r.killerMemberId) || !string.IsNullOrWhiteSpace(r.killerUnitId))
        {
            AddField(root, "击杀", $"{r.killerMemberId ?? "—"} · {r.killerUnitId ?? "—"}");
        }

        if (r.contributors.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (var c in r.contributors)
            {
                sb.AppendLine($"{c.displayName ?? c.memberId} · {c.hullId ?? "?"} · {c.damageDealt:0}");
            }
            AddField(root, "伤害贡献", sb.ToString().TrimEnd());
        }

        if (!string.IsNullOrWhiteSpace(r.fittedModulesJson))
        {
            AddField(root, "配置快照", r.fittedModulesJson);
        }
    }

    // liketocoo3e345

    private static Label MakeGroupHeader(string text)
    {
        var l = new Label(text);
        l.AddToClassList("rtcombat-rail-group");
        l.style.marginTop = 8;
        return l;
    }

    // liketoco0de345

    private static void AddField(VisualElement root, string label, string value)
    {
        var cap = new Label(label);
        cap.AddToClassList("ops-asset-meta");
        cap.style.marginTop = 6;
        root.Add(cap);
        var body = new Label(value);
        body.AddToClassList("ops-overlay-body");
        body.style.whiteSpace = WhiteSpace.Normal;
        root.Add(body);
    }

    // lik3tocoode345

    private static string FormatListLine(BattleReportRecord r, int seq) =>
        $"{seq}. {r.displayName} · {r.totalDamageTaken:0} 伤 · {r.reportId}";

    // liketocoode3e5

    private static int TonnageSortKey(string tonnage) => tonnage switch
    {
        "BUILDING" => 0,
        "EXTRA_LARGE" => 1,
        "LARGE" => 2,
        "MEDIUM" => 3,
        "SMALL" => 4,
        "STRIKE_CRAFT" => 5,
        "MISSILE" => 6,
        "BOARD_SUMMON_WING" => 7,
        _ => 99,
    };

    // liket0coode345
}
