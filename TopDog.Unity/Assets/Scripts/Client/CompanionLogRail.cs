using TopDog.App;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>左侧日志栏：系统事件 + companionLog 混排；滚动条默认贴底。</summary>
public static class CompanionLogRail
{
    private static readonly System.Collections.Generic.Dictionary<VisualElement, StickyBottomScrollTracker> Trackers = new();

    public static void BindScroll(ScrollView? scrollView, VisualElement? feedRoot)
    {
        if (scrollView == null || feedRoot == null || Trackers.ContainsKey(feedRoot))
        {
            return;
        }

        Trackers[feedRoot] = new StickyBottomScrollTracker(scrollView, feedRoot);
    }

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
        NotifyAppended(feedRoot);
    }

    public static void SyncCompanion(SimulationCore? core, VisualElement? feedRoot, ref int syncedCount)
    {
        if (core == null || feedRoot == null)
        {
            return;
        }

        var before = syncedCount;
        var log = core.State.companionLog;
        while (syncedCount < log.Count)
        {
            var entry = log[syncedCount];
            syncedCount++;
            feedRoot.Add(BanterLogLineView.Build(core.State, entry));
        }

        if (syncedCount > before)
        {
            NotifyAppended(feedRoot);
        }
    }

    private static void NotifyAppended(VisualElement feedRoot)
    {
        if (Trackers.TryGetValue(feedRoot, out var tracker))
        {
            tracker.OnContentAppended();
            return;
        }

        ScrollToEnd(feedRoot);
    }

    private static void ScrollToEnd(VisualElement feedRoot)
    {
        var scroll = feedRoot.parent as ScrollView;
        if (scroll == null)
        {
            return;
        }

        scroll.schedule.Execute(() =>
        {
            scroll.scrollOffset = new Vector2(0, scroll.contentContainer.layout.height);
        });
    }

    private sealed class StickyBottomScrollTracker
    {
        private const float BottomEpsilon = 4f;

        private readonly ScrollView _scroll;
        private readonly VisualElement _feedRoot;
        private bool _stickToBottom = true;
        private bool _suppressScrollEvent;

        public StickyBottomScrollTracker(ScrollView scroll, VisualElement feedRoot)
        {
            _scroll = scroll;
            _feedRoot = feedRoot;
            _scroll.verticalScroller.valueChanged += OnScrollerChanged;
            _scroll.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => OnContentGeometryChanged());
            SetCompact(true);
        }

        public void OnContentAppended()
        {
            if (!_stickToBottom)
            {
                return;
            }

            ScrollToBottom();
        }

        private void OnContentGeometryChanged()
        {
            if (_stickToBottom)
            {
                ScrollToBottom();
            }
        }

        private void OnScrollerChanged(float _)
        {
            if (_suppressScrollEvent)
            {
                return;
            }

            _stickToBottom = IsAtBottom();
            SetCompact(_stickToBottom);
        }

        private void SetCompact(bool compact)
        {
            if (compact)
            {
                _feedRoot.AddToClassList("companion-feed-compact");
                _scroll.AddToClassList("companion-scroll-compact");
            }
            else
            {
                _feedRoot.RemoveFromClassList("companion-feed-compact");
                _scroll.RemoveFromClassList("companion-scroll-compact");
            }
        }

        private bool IsAtBottom()
        {
            var scroller = _scroll.verticalScroller;
            return scroller.highValue <= BottomEpsilon
                || scroller.value >= scroller.highValue - BottomEpsilon;
        }

        private void ScrollToBottom()
        {
            _scroll.schedule.Execute(() =>
            {
                _suppressScrollEvent = true;
                var scroller = _scroll.verticalScroller;
                scroller.value = scroller.highValue;
                _stickToBottom = true;
                SetCompact(true);
                _scroll.schedule.Execute(() => _suppressScrollEvent = false);
            });
        }
    }
}
