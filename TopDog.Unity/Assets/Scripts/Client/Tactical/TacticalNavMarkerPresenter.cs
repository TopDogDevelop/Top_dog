using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/// <summary>导航目标标记 + 垂线（TACTICAL_NAVIGATION.md · nav_destination_32.png）。</summary>
public sealed class TacticalNavMarkerPresenter : VisualElement
{
    private const float IconSizePx = 32f;
    private const float IconHalfPx = IconSizePx * 0.5f;

    private readonly TacticalViewportCamera _camera;
    private readonly VisualElement _icon;
    private GameState _state;
    private BattlefieldState _bf;

    public TacticalNavMarkerPresenter(TacticalViewportCamera camera)
    {
        _camera = camera;
        name = "tactical-nav-marker";
        AddToClassList("rtcombat-nav-marker");
        pickingMode = PickingMode.Ignore;

        _icon = new VisualElement { name = "nav-destination-icon" };
        _icon.AddToClassList("rtcombat-nav-destination-icon");
        _icon.pickingMode = PickingMode.Ignore;
        var tex = TacticalIconCatalog.ResolveNavDestinationIcon();
        if (tex != null)
        {
            _icon.style.backgroundImage = new StyleBackground(tex);
        }

        Add(_icon);
        generateVisualContent += OnGenerateVisualContent;
    }

    public void Refresh(GameState state, BattlefieldState bf)
    {
        _state = state;
        _bf = bf;
        var visible = state != null && state.tacticalNavVisible;
        style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        _icon.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (!visible)
        {
            return;
        }

        UpdateIconPlacement();
        MarkDirtyRepaint();
    }

    private void UpdateIconPlacement()
    {
        if (_state == null || _bf == null || _camera == null)
        {
            return;
        }

        var w = contentRect.width;
        var h = contentRect.height;
        if (w < 1f || h < 1f)
        {
            return;
        }

        var focus = TopDog.Sim.Vision.VisionAnchorService.ResolveDefaultFocus(_state, _bf);
        var fx = focus?.x ?? 0f;
        var fy = focus?.y ?? 0f;
        var fz = focus?.z ?? 0f;
        var dx = _state.tacticalNavX - fx;
        var dy = _state.tacticalNavY - fy;
        var dz = _state.tacticalNavZ - fz;
        var proj = _camera.ProjectWorldOffset(dx, dy, dz, w, h);
        if (!proj.InFront)
        {
            _icon.style.display = DisplayStyle.None;
            return;
        }

        _icon.style.display = DisplayStyle.Flex;
        _icon.style.left = proj.CenterX - IconHalfPx;
        _icon.style.top = proj.CenterY - IconHalfPx;
        _icon.style.width = IconSizePx;
        _icon.style.height = IconSizePx;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        if (_state == null || _bf == null || _camera == null || !_state.tacticalNavVisible)
        {
            return;
        }

        var w = contentRect.width;
        var h = contentRect.height;
        if (w < 1f || h < 1f)
        {
            return;
        }

        var focus = TopDog.Sim.Vision.VisionAnchorService.ResolveDefaultFocus(_state, _bf);
        var fx = focus?.x ?? 0f;
        var fy = focus?.y ?? 0f;
        var fz = focus?.z ?? 0f;
        var dx = _state.tacticalNavX - fx;
        var dy = _state.tacticalNavY - fy;
        var dz = _state.tacticalNavZ - fz;
        var proj = _camera.ProjectWorldOffset(dx, dy, dz, w, h);
        if (!proj.InFront)
        {
            return;
        }

        var ground = _camera.ProjectWorldOffset(dx, dy, 0f, w, h);
        var painter = ctx.painter2D;
        painter.strokeColor = new Color(0.7f, 0.9f, 1f, 0.55f);
        painter.lineWidth = 1f;
        painter.BeginPath();
        painter.MoveTo(new Vector2(proj.CenterX, proj.CenterY));
        painter.LineTo(new Vector2(ground.CenterX, ground.CenterY));
        painter.Stroke();
    }
}
