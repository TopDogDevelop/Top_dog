using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>
/// Fixed 1920×1056 design canvas. Panel ScaleWithScreenSize + dynamic match = contain (Simulator-safe).
/// </summary>
public static class UiViewportConfig
{
    public const float DesignWidth = 1920f;
    public const float DesignHeight = 1056f;
    public const float MinControlSizePx = 16f;
    public const float SafeMarginRatio = 0.06f;

    public static float ReferenceWidth { get; private set; } = DesignWidth;
    public static float ReferenceHeight { get; private set; } = DesignHeight;

    public static float Aspect => ReferenceWidth / ReferenceHeight;

    public static void ResolveReference(Vector2Int? inspectorOverride)
    {
        if (inspectorOverride is { x: > 0, y: > 0 } over)
        {
            ReferenceWidth = over.x;
            ReferenceHeight = over.y;
            return;
        }

        ReferenceWidth = DesignWidth;
        ReferenceHeight = DesignHeight;
    }

    public static float ComputeContainScale(float availableWidth, float availableHeight)
    {
        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            return 1f;
        }
        return Mathf.Min(availableWidth / ReferenceWidth, availableHeight / ReferenceHeight);
    }

    public static void ApplyToPanel(PanelSettings? panelSettings, Vector2Int? inspectorOverride = null)
    {
        if (panelSettings == null)
        {
            return;
        }
        ResolveReference(inspectorOverride);
        if (Application.isPlaying && Screen.width >= 64 && Screen.height >= 64)
        {
            ApplyContainToPanel(panelSettings, Screen.width, Screen.height);
        }
        else
        {
            ApplyContainToPanel(panelSettings, ReferenceWidth, ReferenceHeight);
        }
    }

    /// <summary>Scale-with-screen-size using the limiting axis (contain / max uniform scale).</summary>
    public static void ApplyContainToPanel(
        PanelSettings panelSettings,
        float availableWidth,
        float availableHeight)
    {
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panelSettings.referenceResolution = new Vector2Int(
            Mathf.RoundToInt(ReferenceWidth),
            Mathf.RoundToInt(ReferenceHeight));
        panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panelSettings.match = ComputeContainMatch(availableWidth, availableHeight);
    }

    /// <summary>0 = fit width, 1 = fit height — picks min scale (contain).</summary>
    public static float ComputeContainMatch(float availableWidth, float availableHeight)
    {
        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            return 0f;
        }
        var panelAspect = availableWidth / availableHeight;
        return panelAspect >= Aspect ? 1f : 0f;
    }
}
