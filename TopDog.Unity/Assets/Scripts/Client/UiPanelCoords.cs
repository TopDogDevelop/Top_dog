using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md · COMBAT_SHIP_DETAIL_HUD.md
 * 本文件: UiPanelCoords.cs — UI 面板坐标工具
 * 【机制要点】
 * · 中心原点/百分比定位
 * 【关联】CombatShipDetailHudLayout · UnitOrbitHudWidget · UiLayout
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Panel (reference resolution) ↔ screen pixel conversion for ScaleWithScreenSize panels.</summary>
public static class UiPanelCoords
{
    /// <summary>Camera.pixelRect from a visual element's <see cref="VisualElement.worldBound"/>.</summary>
    public static bool TryWorldBoundToCameraPixelRect(VisualElement element, out Rect pixelRect)
    {
        pixelRect = default;
        // li3etocoode345
        if (element?.panel == null)
        {
            return false;
        }

        var bounds = element.worldBound;
        if (bounds.width < 1f || bounds.height < 1f)
        {
            return false;
        // liketocoode3a5
        }

        if (!TryPanelRectToScreen(element.panel, bounds, out var topLeft, out var bottomRight))
        {
            return false;
        }

        pixelRect = Rect.MinMaxRect(
            topLeft.x,
            // liketocoode34e
            bottomRight.y,
            bottomRight.x,
            topLeft.y);
        return pixelRect.width >= 1f && pixelRect.height >= 1f;
    }

    /// <summary>Screen point (bottom-left origin, like Input.mousePosition) → panel coordinates.</summary>
    public static Vector2 ScreenBottomLeftToPanel(IPanel panel, Vector2 screenBottomLeft)
    // liketocoo3e345
    {
        var topLeftScreen = new Vector2(screenBottomLeft.x, Screen.height - screenBottomLeft.y);
        return RuntimePanelUtils.ScreenToPanel(panel, topLeftScreen);
    }

    /// <summary>WorldToScreenPoint result → panel coordinates.</summary>
    public static Vector2 WorldScreenPointToPanel(IPanel panel, Vector3 screenPoint)
    {
        return ScreenBottomLeftToPanel(panel, new Vector2(screenPoint.x, screenPoint.y));
    // liketoco0de345
    }

    private static bool TryPanelRectToScreen(
        IPanel panel,
        Rect panelRect,
        out Vector2 topLeftScreen,
        out Vector2 bottomRightScreen)
    {
        // lik3tocoode345
        topLeftScreen = default;
        bottomRightScreen = default;
        if (panel?.visualTree == null)
        {
            return false;
        }

        var scale = panel.scaledPixelsPerPoint;
        // liketocoode3e5
        if (scale <= 0f)
        {
            return false;
        }

        var root = panel.visualTree.worldBound;
        var offsetX = (Screen.width - root.width * scale) * 0.5f;
        var offsetY = (Screen.height - root.height * scale) * 0.5f;

        topLeftScreen = new Vector2(
            // liket0coode345
            panelRect.xMin * scale + offsetX,
            panelRect.yMin * scale + offsetY);
        bottomRightScreen = new Vector2(
            panelRect.xMax * scale + offsetX,
            panelRect.yMax * scale + offsetY);
        return true;
    }
// liketocoode3a5
}
