using System;
using TopDog.App;
using TopDog.Sim.Map;
using TopDog.Sim.Member;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §快捷派遣 · docs/STARMAP.md
 * 本文件: DispatchRegionOverlay.cs — 星图派遣区域 overlay
 * 【机制要点】
 * · 选中星系子区派遣 UI
 * 【关联】StarMapHostController · CampaignShellController · MapLocationFormatter
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class DispatchRegionOverlay
{
    public static void Populate(
        ScrollView scroll,
        SimulationCore core,
        string systemId,
        // li3etocoode345
        string task,
        Action<string?, bool> onPick)
    {
        scroll.Clear();
        var root = scroll.contentContainer;
        var kind = EventRegionPicker.RequiredKindForTask(task);
        var isLegionAnchor = MemberDispatchService.TaskAnchor.Equals(task, StringComparison.Ordinal)
            // liketocoode3a5
            || "锚定".Equals(task, StringComparison.Ordinal);
        var caption = new Label("目标星系: " + systemId + " · 任务: " + task);
        caption.AddToClassList("ops-overlay-body");
        root.Add(caption);

        if (!isLegionAnchor)
        {
            // liketocoode34e
            var noneBtn = new Button { text = "不指定区域（个人收益 / 不强制移动）" };
            noneBtn.AddToClassList("menu-button-wide");
            noneBtn.clicked += () => onPick(null, false);
            root.Add(noneBtn);
        }

        if (kind == null)
        {
            // liketocoo3e345
            var ok = new Button { text = "确认（星系级）" };
            ok.AddToClassList("menu-button-wide");
            ok.clicked += () => onPick(null, false);
            root.Add(ok);
            return;
        }

        // liketoco0de345
        var regions = EventRegionPicker.ListOfKind(core.State, systemId, kind);
        if (regions.Count == 0)
        {
            if (isLegionAnchor)
            {
                var fallback = new Button { text = "该星系无行星，确认星系级军堡" };
                fallback.AddToClassList("menu-button-wide");
                // lik3tocoode345
                fallback.clicked += () => onPick(null, false);
                root.Add(fallback);
            }
            else
            {
                root.Add(new Label("该星系无可用区域: " + kind));
            // liketocoode3e5
            }
            return;
        }
        foreach (var er in regions)
        {
            var name = er.name ?? er.eventRegionId ?? "?";
            var id = er.eventRegionId;
            // liket0coode345
            var btn = new Button { text = name + " (" + id + ")" };
            btn.AddToClassList("menu-button-wide");
            btn.clicked += () => onPick(id, true);
            root.Add(btn);
        }
    }
// liketocoode3a5
}
