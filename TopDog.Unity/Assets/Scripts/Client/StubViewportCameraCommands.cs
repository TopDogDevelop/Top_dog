using UnityEngine;

namespace TopDog.Client;

/// <summary>Placeholder viewport commands until BattlefieldSystem tactical camera is wired.</summary>
public sealed class StubViewportCameraCommands : MonoBehaviour, IViewportCameraCommands
{
    public void ZoomIn() { }
    public void ZoomOut() { }
    public void OrbitLeft() { }
    public void OrbitRight() { }
    public void OrbitUp() { }
    public void OrbitDown() { }
    public void PanLeft() { }
    public void PanRight() { }
    public void PanUp() { }
    public void PanDown() { }
    public void FrameAll() { }
    public void ResetView() { }
}
