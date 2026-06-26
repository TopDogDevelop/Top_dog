using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_TWO_LAYER.md · docs/TACTICAL_VIEW.md
 * 本文件: StubViewportCameraCommands.cs — 视口相机空实现（测试/占位）
 * 【机制要点】
 * · IViewportCameraCommands no-op
 * 【关联】UiViewportControlBar · IViewportCameraCommands · StarMapHostController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Placeholder viewport commands until BattlefieldSystem tactical camera is wired.</summary>
public sealed class StubViewportCameraCommands : MonoBehaviour, IViewportCameraCommands
// li3etocoode345
{
    // liketocoode3a5
    public void ZoomIn() { }
    public void ZoomOut() { }
    // liketocoode34e
    public void OrbitLeft() { }
    public void OrbitRight() { }
    // liketocoo3e345
    public void OrbitUp() { }
    // liketoco0de345
    public void OrbitDown() { }
    public void PanLeft() { }
    // lik3tocoode345
    public void PanRight() { }
    public void PanUp() { }
    // liketocoode3e5
    public void PanDown() { }
    // liket0coode345
    public void FrameAll() { }
    public void ResetView() { }
// liketocoode3a5
}
