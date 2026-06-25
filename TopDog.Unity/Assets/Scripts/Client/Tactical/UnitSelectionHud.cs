using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/// <summary>选中单位 HUD：盾/甲/结构条 + 名称（TACTICAL_VIEW.md §4.2）。</summary>
public sealed class UnitSelectionHud
{
    private readonly VisualElement _root;
    private readonly Label _title;
    private readonly ProgressBar _shieldBar;
    private readonly ProgressBar _armorBar;
    private readonly ProgressBar _structureBar;
    private readonly Label _detail;

    public UnitSelectionHud(VisualElement host)
    {
        _root = new VisualElement();
        _root.name = "selection-hud";
        _root.AddToClassList("rtcombat-selection-hud");
        _root.style.display = DisplayStyle.None;

        _title = new Label();
        _title.AddToClassList("rtcombat-hud-title");
        _root.Add(_title);

        _shieldBar = MakeBar("rtcombat-hud-bar-shield");
        _armorBar = MakeBar("rtcombat-hud-bar-armor");
        _structureBar = MakeBar("rtcombat-hud-bar-structure");
        _root.Add(_shieldBar);
        _root.Add(_armorBar);
        _root.Add(_structureBar);

        _detail = new Label();
        _detail.AddToClassList("rtcombat-hud-bottom");
        _root.Add(_detail);

        host?.Add(_root);
    }

    public void Refresh(BattlefieldState bf, (float left, float top)? anchor = null)
    {
        var id = TacticalSelectionState.SelectedTargetUnitId;
        if (bf == null || id == null)
        {
            _root.style.display = DisplayStyle.None;
            return;
        }
        BattlefieldUnit unit = null;
        foreach (var u in bf.units)
        {
            if (id.Equals(u.unitId, System.StringComparison.Ordinal))
            {
                unit = u;
                break;
            }
        }
        if (unit == null || unit.IsDestroyed())
        {
            _root.style.display = DisplayStyle.None;
            return;
        }

        _root.style.display = DisplayStyle.Flex;
        if (anchor != null)
        {
            _root.style.left = anchor.Value.left + 20f;
            _root.style.top = anchor.Value.top - 8f;
            _root.style.position = Position.Absolute;
        }
        else
        {
            _root.style.left = 8f;
            _root.style.top = 8f;
        }

        if (unit.isBuilding)
        {
            _title.text = unit.displayName ?? "建筑";
            _shieldBar.style.display = DisplayStyle.None;
            _armorBar.style.display = DisplayStyle.None;
            _structureBar.style.display = DisplayStyle.Flex;
            SetBar(_structureBar, unit.structureHp, unit.structureMax, "结构");
            _detail.text = "";
        }
        else
        {
            _title.text = unit.displayName ?? unit.unitId ?? "?";
            _shieldBar.style.display = DisplayStyle.Flex;
            _armorBar.style.display = DisplayStyle.Flex;
            _structureBar.style.display = DisplayStyle.Flex;
            SetBar(_shieldBar, unit.shieldHp, unit.shieldMax, "护盾");
            SetBar(_armorBar, unit.armorHp, unit.armorMax, "装甲");
            SetBar(_structureBar, unit.structureHp, unit.structureMax, "结构");
            _detail.text = $"{TacticalIconCatalog.GroupLabel(unit.tonnageClass)} · {unit.SpeedMps():0} m/s · Z={unit.z:0}m";
        }
    }

    private static ProgressBar MakeBar(string className)
    {
        var bar = new ProgressBar { lowValue = 0f, highValue = 1f };
        bar.AddToClassList("rtcombat-hud-bar");
        bar.AddToClassList(className);
        return bar;
    }

    private static void SetBar(ProgressBar bar, float hp, float max, string label)
    {
        bar.title = label + " " + hp.ToString("0") + "/" + max.ToString("0");
        bar.value = max > 0f ? Mathf.Clamp01(hp / max) : 0f;
    }
}
