using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

public static class UiLayout
{
    public static void ApplyDocumentLayout(VisualElement panelRoot)
    {
        if (panelRoot == null)
        {
            return;
        }

        FillPanel(panelRoot);

        var content = panelRoot.Q("root") ?? panelRoot.Q(className: "screen-root");
        var isOpsShell = content != null && content.ClassListContains("campaign-shell-root");

        var shell = panelRoot.Q(className: "display-shell");
        if (shell != null)
        {
            FillColumn(shell);
            shell.style.justifyContent = isOpsShell ? Justify.FlexStart : Justify.Center;
            if (isOpsShell)
            {
                shell.style.minHeight = 0;
            }
        }

        var frame = panelRoot.Q(className: "viewport-frame");
        if (frame != null)
        {
            FillColumn(frame);
            frame.style.justifyContent = isOpsShell ? Justify.FlexStart : Justify.Center;
            if (isOpsShell)
            {
                frame.style.minHeight = 0;
            }
        }

        if (content != null)
        {
            FillColumn(content);
            var isMenuScreen = content.Q(className: "menu-stack") != null
                || content.ClassListContains("main-menu-root");
            if (isMenuScreen)
            {
                content.style.alignItems = Align.Center;
                content.style.justifyContent = Justify.Center;
            }
            else
            {
                content.style.alignItems = Align.Stretch;
                content.style.justifyContent = Justify.FlexStart;
                if (isOpsShell)
                {
                    content.style.minHeight = 0;
                }
            }
        }

        var stack = panelRoot.Q(className: "menu-stack");
        if (stack != null)
        {
            FillColumn(stack);
            stack.style.alignItems = Align.Center;
            stack.style.justifyContent = Justify.Center;
        }

        if (isOpsShell && content != null)
        {
            ApplyOperationsGrid(content);
        }
    }

    /// <summary>
    /// Inline flex axes for operations shell — survives missing/broken USS at runtime.
    /// </summary>
    public static void ApplyOperationsGrid(VisualElement opsRoot)
    {
        opsRoot.style.overflow = Overflow.Hidden;

        var topBar = opsRoot.Q(className: "ops-top-bar");
        if (topBar != null)
        {
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.flexShrink = 0;
            topBar.style.height = 60;
            topBar.style.minHeight = 60;
            topBar.style.alignItems = Align.Center;
        }

        var navScroll = opsRoot.Q(className: "ops-top-nav-scroll");
        if (navScroll != null)
        {
            navScroll.style.flexDirection = FlexDirection.Row;
            navScroll.style.flexWrap = Wrap.Wrap;
            navScroll.style.flexGrow = 1;
            navScroll.style.alignItems = Align.Center;
        }

        var bodyRow = opsRoot.Q(className: "ops-body-row");
        if (bodyRow != null)
        {
            bodyRow.style.flexDirection = FlexDirection.Row;
            bodyRow.style.flexGrow = 1;
            bodyRow.style.flexShrink = 1;
            bodyRow.style.minHeight = 0;
            bodyRow.style.overflow = Overflow.Hidden;
        }

        SetColumn(opsRoot.Q(className: "ops-left-events"));
        var left = opsRoot.Q(className: "ops-left-events");
        if (left != null)
        {
            left.style.flexShrink = 0;
            left.style.width = 240;
            left.style.minWidth = 240;
        }

        var center = opsRoot.Q(className: "ops-center-column");
        if (center != null)
        {
            SetColumn(center);
            center.style.flexGrow = 1;
            center.style.flexShrink = 1;
            center.style.minWidth = 200;
            center.style.minHeight = 0;
        }

        var detail = opsRoot.Q(className: "ops-member-detail");
        if (detail != null)
        {
            SetColumn(detail);
            detail.style.flexShrink = 0;
            detail.style.width = 260;
            detail.style.minWidth = 260;
        }

        var rail = opsRoot.Q(className: "ops-right-rail");
        if (rail != null)
        {
            SetColumn(rail);
            rail.style.flexShrink = 0;
            rail.style.width = 268;
            rail.style.minWidth = 268;
            rail.style.minHeight = 0;
            rail.style.display = DisplayStyle.Flex;
        }

        var memberScroll = opsRoot.Q<ScrollView>("member-scroll");
        if (memberScroll != null)
        {
            memberScroll.style.flexGrow = 1;
            memberScroll.style.flexShrink = 1;
            memberScroll.style.minHeight = 160;
        }

        var starMap = opsRoot.Q(className: "ops-star-map");
        if (starMap != null)
        {
            starMap.style.flexGrow = 1;
            starMap.style.flexShrink = 1;
            starMap.style.minHeight = 0;
        }

        var bottomBar = opsRoot.Q(className: "ops-bottom-bar");
        if (bottomBar != null)
        {
            bottomBar.style.flexDirection = FlexDirection.Row;
            bottomBar.style.flexShrink = 0;
            bottomBar.style.height = 88;
            bottomBar.style.minHeight = 88;
            bottomBar.style.alignItems = Align.Center;
        }

        var legionHeader = opsRoot.Q(className: "ops-legion-header");
        if (legionHeader != null)
        {
            legionHeader.style.flexDirection = FlexDirection.Row;
            legionHeader.style.flexWrap = Wrap.Wrap;
            legionHeader.style.alignItems = Align.Center;
        }

        opsRoot.Query(className: "ops-action-row").ForEach(el =>
        {
            el.style.flexDirection = FlexDirection.Row;
            el.style.flexWrap = Wrap.Wrap;
        });

        opsRoot.Query(className: "ops-star-map-mode-bar").ForEach(el =>
        {
            el.style.flexDirection = FlexDirection.Row;
        });

        var vpControls = opsRoot.Q(className: "ops-viewport-controls")
            ?? opsRoot.Q(className: "ops-viewport-controls-minimal");
        if (vpControls != null)
        {
            vpControls.style.flexDirection = FlexDirection.Column;
            vpControls.pickingMode = PickingMode.Position;
        }

        var memberList = opsRoot.Q<VisualElement>("member-list");
        if (memberList != null)
        {
            memberList.style.flexGrow = 1;
            memberList.style.width = Length.Percent(100);
            memberList.style.minWidth = 0;
        }

        if (memberScroll != null)
        {
            memberScroll.contentContainer.style.flexGrow = 1;
            memberScroll.contentContainer.style.width = Length.Percent(100);
            memberScroll.contentContainer.style.minWidth = 0;
        }

        opsRoot.Query(className: "ops-top-btn").ForEach(el =>
        {
            el.style.flexGrow = 0;
            el.style.flexShrink = 0;
            el.style.width = StyleKeyword.Auto;
            el.style.alignSelf = Align.Center;
        });

        opsRoot.Query(className: "ops-small-btn").ForEach(el =>
        {
            el.style.flexGrow = 0;
            el.style.flexShrink = 0;
            el.style.width = StyleKeyword.Auto;
        });
    }

    private static void SetColumn(VisualElement? element)
    {
        if (element == null)
        {
            return;
        }

        element.style.flexDirection = FlexDirection.Column;
    }

    public static void FillPanel(VisualElement element)
    {
        element.style.flexGrow = 1;
        element.style.flexShrink = 0;
        element.style.flexDirection = FlexDirection.Column;
        element.style.width = Length.Percent(100);
        element.style.height = Length.Percent(100);
        element.style.minHeight = Length.Percent(100);
    }

    public static void FillColumn(VisualElement element)
    {
        element.style.flexGrow = 1;
        element.style.flexShrink = 1;
        element.style.flexDirection = FlexDirection.Column;
        element.style.width = Length.Percent(100);
        element.style.height = Length.Percent(100);
        element.style.minHeight = Length.Percent(100);
        element.style.minWidth = Length.Percent(100);
    }

    /// <summary>Legacy alias; prefer <see cref="FillColumn"/>.</summary>
    public static void Stretch(VisualElement element) => FillColumn(element);
}
