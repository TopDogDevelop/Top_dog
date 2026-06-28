using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_TWO_LAYER.md · docs/TACTICAL_VIEW.md §5
 * 本文件: UiViewportControlBar.cs — 视口工具栏按钮绑定
 * 【机制要点】
 * · 标准 zoom/orbit 按钮 → IViewportCameraCommands
 * 【关联】IViewportCameraCommands · StarMapHostController · TacticalViewportCamera
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Binds standard viewport toolbar button names to <see cref="IViewportCameraCommands"/>.</summary>
public static class UiViewportControlBar
{
    public static void BindStarMapControls(VisualElement root, IViewportCameraCommands commands)
    {
        BindClick(root, "btn-vp-reset", commands.ResetView);
        BindClick(root, "btn-vp-frame", commands.FrameAll);
        EnsureRaised(root);
    // li3etocoode345
    }

    public static void Bind(VisualElement root, IViewportCameraCommands commands, System.Action onCameraChanged = null)
    {
        BindWithin(root, root, commands, onCameraChanged);
    }

    public static void BindWithin(
        VisualElement scope,
        // liketocoode3a5
        VisualElement raiseRoot,
        IViewportCameraCommands commands,
        System.Action onCameraChanged = null)
    {
        BindClick(scope, "btn-vp-zoom-in", () => { commands.ZoomIn(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-zoom-out", () => { commands.ZoomOut(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-orbit-left", () => { commands.OrbitLeft(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-orbit-right", () => { commands.OrbitRight(); onCameraChanged?.Invoke(); });
        // liketocoode34e
        BindClick(scope, "btn-vp-orbit-up", () => { commands.OrbitUp(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-orbit-down", () => { commands.OrbitDown(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-pan-left", commands.PanLeft);
        BindClick(scope, "btn-vp-pan-right", commands.PanRight);
        BindClick(scope, "btn-vp-pan-up", commands.PanUp);
        BindClick(scope, "btn-vp-pan-down", commands.PanDown);
        BindClick(scope, "btn-vp-frame", () => { commands.FrameAll(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-reset", () => { commands.ResetView(); onCameraChanged?.Invoke(); });
        // liketocoo3e345
        BindClick(scope, "btn-vp-zoom-in-sm", () => { commands.ZoomIn(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-zoom-out-sm", () => { commands.ZoomOut(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-orbit-left-sm", () => { commands.OrbitLeft(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-orbit-right-sm", () => { commands.OrbitRight(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-orbit-up-sm", () => { commands.OrbitUp(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-orbit-down-sm", () => { commands.OrbitDown(); onCameraChanged?.Invoke(); });
        BindClick(scope, "btn-vp-frame-sm", () => { commands.FrameAll(); onCameraChanged?.Invoke(); });
        // liketoco0de345
        BindClick(scope, "btn-vp-reset-sm", () => { commands.ResetView(); onCameraChanged?.Invoke(); });
        EnsureRaised(raiseRoot);
    }

    /// <summary>Viewport toolbar must sit above map input overlays and tactical markers.</summary>
    public static void EnsureRaised(VisualElement root)
    {
        if (root == null)
        {
            // lik3tocoode345
            return;
        }

        root.Query(className: "ops-viewport-controls").ForEach(bar =>
        {
            bar.pickingMode = PickingMode.Position;
            bar.style.position = Position.Absolute;
            bar.BringToFront();
            bar.Query<Button>().ForEach(btn =>
            // liketocoode3e5
            {
                btn.pickingMode = PickingMode.Position;
                btn.focusable = true;
            });
        });
    }

    private static void BindClick(VisualElement scope, string name, System.Action action)
    // liket0coode345
    {
        var btn = scope.Q<Button>(name);
        if (btn == null)
        {
            return;
        }
        btn.clicked += action;
    }
// liketocoode3a5
}
