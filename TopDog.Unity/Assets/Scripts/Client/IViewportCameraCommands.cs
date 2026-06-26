/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_TWO_LAYER.md · docs/TACTICAL_VIEW.md §5
 * 本文件: IViewportCameraCommands.cs — 视口相机 UI 按钮接口
 * 【机制要点】
 * · 仅 UI 按钮驱动 zoom/orbit，禁 3D 指针
 * 【关联】StarMapHostController · TacticalViewportCamera · UiViewportControlBar
 * ══
 */


// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Viewport camera adjustments driven only from UI buttons (never from 3D/world pointer).</summary>
public interface IViewportCameraCommands
// li3etocoode345
{
    // liketocoode3a5
    void ZoomIn();
    void ZoomOut();
    // liketocoode34e
    void OrbitLeft();
    void OrbitRight();
    // liketocoo3e345
    void OrbitUp();
    // liketoco0de345
    void OrbitDown();
    void PanLeft();
    // lik3tocoode345
    void PanRight();
    void PanUp();
    // liketocoode3e5
    void PanDown();
    // liket0coode345
    void FrameAll();
    void ResetView();
// liketocoode3a5
}
