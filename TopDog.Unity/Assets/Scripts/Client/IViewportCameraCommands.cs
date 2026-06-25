namespace TopDog.Client;

/// <summary>Viewport camera adjustments driven only from UI buttons (never from 3D/world pointer).</summary>
public interface IViewportCameraCommands
{
    void ZoomIn();
    void ZoomOut();
    void OrbitLeft();
    void OrbitRight();
    void OrbitUp();
    void OrbitDown();
    void PanLeft();
    void PanRight();
    void PanUp();
    void PanDown();
    void FrameAll();
    void ResetView();
}
