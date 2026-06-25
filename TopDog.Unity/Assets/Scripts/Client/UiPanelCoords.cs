using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Panel (reference resolution) ↔ screen pixel conversion for ScaleWithScreenSize panels.</summary>
public static class UiPanelCoords
{
    /// <summary>Camera.pixelRect from a visual element's <see cref="VisualElement.worldBound"/>.</summary>
    public static bool TryWorldBoundToCameraPixelRect(VisualElement element, out Rect pixelRect)
    {
        pixelRect = default;
        if (element?.panel == null)
        {
            return false;
        }

        var bounds = element.worldBound;
        if (bounds.width < 1f || bounds.height < 1f)
        {
            return false;
        }

        if (!TryPanelRectToScreen(element.panel, bounds, out var topLeft, out var bottomRight))
        {
            return false;
        }

        pixelRect = Rect.MinMaxRect(
            topLeft.x,
            bottomRight.y,
            bottomRight.x,
            topLeft.y);
        return pixelRect.width >= 1f && pixelRect.height >= 1f;
    }

    /// <summary>Screen point (bottom-left origin, like Input.mousePosition) → panel coordinates.</summary>
    public static Vector2 ScreenBottomLeftToPanel(IPanel panel, Vector2 screenBottomLeft)
    {
        var topLeftScreen = new Vector2(screenBottomLeft.x, Screen.height - screenBottomLeft.y);
        return RuntimePanelUtils.ScreenToPanel(panel, topLeftScreen);
    }

    /// <summary>WorldToScreenPoint result → panel coordinates.</summary>
    public static Vector2 WorldScreenPointToPanel(IPanel panel, Vector3 screenPoint)
    {
        return ScreenBottomLeftToPanel(panel, new Vector2(screenPoint.x, screenPoint.y));
    }

    private static bool TryPanelRectToScreen(
        IPanel panel,
        Rect panelRect,
        out Vector2 topLeftScreen,
        out Vector2 bottomRightScreen)
    {
        topLeftScreen = default;
        bottomRightScreen = default;
        if (panel?.visualTree == null)
        {
            return false;
        }

        var scale = panel.scaledPixelsPerPoint;
        if (scale <= 0f)
        {
            return false;
        }

        var root = panel.visualTree.worldBound;
        var offsetX = (Screen.width - root.width * scale) * 0.5f;
        var offsetY = (Screen.height - root.height * scale) * 0.5f;

        topLeftScreen = new Vector2(
            panelRect.xMin * scale + offsetX,
            panelRect.yMin * scale + offsetY);
        bottomRightScreen = new Vector2(
            panelRect.xMax * scale + offsetX,
            panelRect.yMax * scale + offsetY);
        return true;
    }
}
