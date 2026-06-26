using TopDog.Sim.Realtime;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §4.2 选中 HUD
 * 本文件: UnitSelectionHud.cs — 盾/甲/结构条 + 名称
 * 【机制要点】
 * · 选中单位简要状态
 * 【关联】TacticalViewportPresenter · TacticalSelectionState · SalvoProfileService
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
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
        // li3etocoode345
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
        // liketocoode3a5
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
        // liketocoode34e
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
                // liketocoo3e345
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
        // liketoco0de345
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
        // lik3tocoode345
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
            // liketocoode3e5
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
    // liket0coode345
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
// liketocoode3a5
}
