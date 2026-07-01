using TopDog.App;
using TopDog.Sim.State;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>左侧日志栏：系统事件 + companionLog 混排。</summary>
public static class CompanionLogRail
{
    public static void AppendSystemLine(VisualElement? feedRoot, string message)
    {
        if (feedRoot == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var label = new Label(message);
        label.AddToClassList("ops-event-feed");
        label.AddToClassList("banter-system-line");
        feedRoot.Add(label);
        ScrollToEnd(feedRoot);
    }

    public static void SyncCompanion(SimulationCore? core, VisualElement? feedRoot, ref int syncedCount)
    {
        if (core == null || feedRoot == null)
        {
            return;
        }

        var log = core.State.companionLog;
        while (syncedCount < log.Count)
        {
            var entry = log[syncedCount];
            syncedCount++;
            feedRoot.Add(BanterLogLineView.Build(core.State, entry));
        }

        if (syncedCount < log.Count)
        {
            ScrollToEnd(feedRoot);
        }
    }

    private static void ScrollToEnd(VisualElement feedRoot)
    {
        var scroll = feedRoot.parent as ScrollView;
        if (scroll == null)
        {
            return;
        }

        scroll.schedule.Execute(() => scroll.scrollOffset = new UnityEngine.Vector2(0, scroll.contentContainer.layout.height));
    }
}
