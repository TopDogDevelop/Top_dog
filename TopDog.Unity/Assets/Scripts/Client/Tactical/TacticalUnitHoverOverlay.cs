using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/// <summary>悬停单位框选高亮 + 待维修轮次（TACTICAL_NAVIGATION.md §5）。</summary>
public sealed class TacticalUnitHoverOverlay : VisualElement
{
    private const float BoxHalfPx = 24f;

    private readonly TacticalViewportPresenter _presenter;
    private readonly VisualElement _box;
    private readonly Label _repairLabel;

    public TacticalUnitHoverOverlay(TacticalViewportPresenter presenter)
    {
        _presenter = presenter;
        name = "tactical-unit-hover";
        AddToClassList("rtcombat-unit-hover");
        pickingMode = PickingMode.Ignore;

        _box = new VisualElement { name = "hover-box" };
        _box.AddToClassList("rtcombat-hover-box");
        _box.pickingMode = PickingMode.Ignore;
        Add(_box);

        _repairLabel = new Label { name = "hover-repair-rounds" };
        _repairLabel.AddToClassList("rtcombat-hover-repair");
        _repairLabel.pickingMode = PickingMode.Ignore;
        Add(_repairLabel);
    }

    public void Refresh(GameState? state, BattlefieldState? bf)
    {
        var hoverId = TacticalSelectionState.HoveredUnitId;
        if (state == null || bf == null || hoverId == null
            || !_presenter.TryGetUnitScreenCenter(hoverId, out var center))
        {
            style.display = DisplayStyle.None;
            return;
        }

        var unit = FindUnit(bf, hoverId);
        if (unit == null || unit.IsDestroyed() || !unit.Arrived(bf.timeSec)
            || BattlefieldSceneProxyService.IsSceneProxy(unit))
        {
            style.display = DisplayStyle.None;
            return;
        }

        style.display = DisplayStyle.Flex;
        _box.EnableInClassList("rtcombat-hover-friendly", unit.side == UnitSide.FRIENDLY);
        _box.EnableInClassList("rtcombat-hover-hostile", unit.side != UnitSide.FRIENDLY);

        _box.style.left = center.x - BoxHalfPx;
        _box.style.top = center.y - BoxHalfPx;
        _box.style.width = BoxHalfPx * 2f;
        _box.style.height = BoxHalfPx * 2f;

        _repairLabel.style.display = DisplayStyle.None;
    }

    private static BattlefieldUnit? FindUnit(BattlefieldState bf, string unitId)
    {
        foreach (var u in bf.units)
        {
            if (unitId.Equals(u.unitId, System.StringComparison.Ordinal))
            {
                return u;
            }
        }

        return null;
    }
}
