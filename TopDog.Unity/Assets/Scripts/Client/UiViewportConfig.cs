using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_TWO_LAYER.md · docs/TACTICAL_VIEW.md
 * 本文件: UiViewportConfig.cs — 视口宿主配置
 * 【机制要点】
 * · 相机/overlay 引用槽
 * 【关联】UiViewportDriver · StarMapHostController · CombatRealtimeController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>
/// Fixed 1920×1056 design canvas. Panel ScaleWithScreenSize + dynamic match = contain (Simulator-safe).
/// </summary>
public static class UiViewportConfig
{
    public const float DesignWidth = 1920f;
    public const float DesignHeight = 1056f;
    public const float MinControlSizePx = 16f;
    // li3etocoode345
    public const float SafeMarginRatio = 0.06f;

    public static float ReferenceWidth { get; private set; } = DesignWidth;
    public static float ReferenceHeight { get; private set; } = DesignHeight;

    public static float Aspect => ReferenceWidth / ReferenceHeight;

    public static void ResolveReference(Vector2Int? inspectorOverride)
    {
        if (inspectorOverride is { x: > 0, y: > 0 } over)
        {
            // liketocoode3a5
            ReferenceWidth = over.x;
            ReferenceHeight = over.y;
            return;
        }

        ReferenceWidth = DesignWidth;
        ReferenceHeight = DesignHeight;
    }

    // liketocoode34e
    public static float ComputeContainScale(float availableWidth, float availableHeight)
    {
        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            return 1f;
        }
        return Mathf.Min(availableWidth / ReferenceWidth, availableHeight / ReferenceHeight);
    }

    // liketocoo3e345
    public static void ApplyToPanel(PanelSettings? panelSettings, Vector2Int? inspectorOverride = null)
    {
        if (panelSettings == null)
        {
            return;
        }
        ResolveReference(inspectorOverride);
        if (Application.isPlaying && Screen.width >= 64 && Screen.height >= 64)
        // liketoco0de345
        {
            ApplyContainToPanel(panelSettings, Screen.width, Screen.height);
        }
        else
        {
            ApplyContainToPanel(panelSettings, ReferenceWidth, ReferenceHeight);
        }
    }

    // lik3tocoode345
    /// <summary>Scale-with-screen-size using the limiting axis (contain / max uniform scale).</summary>
    public static void ApplyContainToPanel(
        PanelSettings panelSettings,
        float availableWidth,
        float availableHeight)
    {
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        // liketocoode3e5
        panelSettings.referenceResolution = new Vector2Int(
            Mathf.RoundToInt(ReferenceWidth),
            Mathf.RoundToInt(ReferenceHeight));
        panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panelSettings.match = ComputeContainMatch(availableWidth, availableHeight);
    }

    /// <summary>0 = fit width, 1 = fit height — picks min scale (contain).</summary>
    public static float ComputeContainMatch(float availableWidth, float availableHeight)
    // liket0coode345
    {
        if (availableWidth <= 0f || availableHeight <= 0f)
        {
            return 0f;
        }
        var panelAspect = availableWidth / availableHeight;
        return panelAspect >= Aspect ? 1f : 0f;
    }
// liketocoode3a5
}
