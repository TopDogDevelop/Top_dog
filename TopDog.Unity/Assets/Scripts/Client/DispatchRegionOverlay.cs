using System;
using TopDog.App;
using TopDog.Sim.Map;
using TopDog.Sim.Member;
using UnityEngine.UIElements;

namespace TopDog.Client;

public static class DispatchRegionOverlay
{
    public static void Populate(
        ScrollView scroll,
        SimulationCore core,
        string systemId,
        string task,
        Action<string?, bool> onPick)
    {
        scroll.Clear();
        var root = scroll.contentContainer;
        var kind = EventRegionPicker.RequiredKindForTask(task);
        var isLegionAnchor = MemberDispatchService.TaskAnchor.Equals(task, StringComparison.Ordinal)
            || "锚定".Equals(task, StringComparison.Ordinal);
        var caption = new Label("目标星系: " + systemId + " · 任务: " + task);
        caption.AddToClassList("ops-overlay-body");
        root.Add(caption);

        if (!isLegionAnchor)
        {
            var noneBtn = new Button { text = "不指定区域（个人收益 / 不强制移动）" };
            noneBtn.AddToClassList("menu-button-wide");
            noneBtn.clicked += () => onPick(null, false);
            root.Add(noneBtn);
        }

        if (kind == null)
        {
            var ok = new Button { text = "确认（星系级）" };
            ok.AddToClassList("menu-button-wide");
            ok.clicked += () => onPick(null, false);
            root.Add(ok);
            return;
        }

        var regions = EventRegionPicker.ListOfKind(core.State, systemId, kind);
        if (regions.Count == 0)
        {
            if (isLegionAnchor)
            {
                var fallback = new Button { text = "该星系无行星，确认星系级军堡" };
                fallback.AddToClassList("menu-button-wide");
                fallback.clicked += () => onPick(null, false);
                root.Add(fallback);
            }
            else
            {
                root.Add(new Label("该星系无可用区域: " + kind));
            }
            return;
        }
        foreach (var er in regions)
        {
            var name = er.name ?? er.eventRegionId ?? "?";
            var id = er.eventRegionId;
            var btn = new Button { text = name + " (" + id + ")" };
            btn.AddToClassList("menu-button-wide");
            btn.clicked += () => onPick(id, true);
            root.Add(btn);
        }
    }
}
