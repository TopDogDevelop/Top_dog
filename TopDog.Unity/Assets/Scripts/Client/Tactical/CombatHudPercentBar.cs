using TopDog.Sim.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/// <summary>战斗详情 HUD 百分比条（替代 ProgressBar，横/竖填充可控）。</summary>
public sealed class CombatHudPercentBar : VisualElement
{
    private readonly VisualElement _fill;
    private readonly bool _vertical;

    public CombatHudPercentBar(bool vertical, string fillClass)
    {
        _vertical = vertical;
        pickingMode = PickingMode.Ignore;
        style.overflow = Overflow.Hidden;
        AddToClassList("rtcombat-hud-bar");
        AddToClassList(vertical ? "rtcombat-ship-detail-bar-vertical" : "rtcombat-ship-detail-bar-horizontal");

        _fill = new VisualElement { name = "fill" };
        _fill.pickingMode = PickingMode.Ignore;
        _fill.AddToClassList("rtcombat-hud-bar-fill");
        if (!string.IsNullOrWhiteSpace(fillClass))
        {
            _fill.AddToClassList(fillClass);
        }

        Add(_fill);
        if (vertical)
        {
            style.flexDirection = FlexDirection.ColumnReverse;
            style.justifyContent = Justify.FlexStart;
            style.alignItems = Align.Stretch;
        }
    }

    public static CombatHudPercentBar ReplaceTemplateBar(
        VisualElement root,
        string elementName,
        bool vertical,
        string fillClass)
    {
        var existing = root.Q(elementName);
        if (existing?.parent == null)
        {
            return new CombatHudPercentBar(vertical, fillClass) { name = elementName };
        }

        var parent = existing.parent;
        var index = parent.IndexOf(existing);
        var bar = new CombatHudPercentBar(vertical, fillClass) { name = elementName };
        parent.Insert(index, bar);
        parent.Remove(existing);
        return bar;
    }

    public void SetFromHp(float value, float max, bool distortHp)
    {
        var linear = ToPercent(value, max);
        var visual = distortHp ? HpBarVisualDistortion.DistortPercent(linear) : linear;
        SetPercent(visual);
    }

    public void SetPercent(float visualPercent)
    {
        var p = Mathf.Clamp(visualPercent, 0f, 100f);
        if (_vertical)
        {
            _fill.style.width = Length.Percent(100);
            _fill.style.height = Length.Percent(p);
            _fill.style.alignSelf = Align.Stretch;
        }
        else
        {
            _fill.style.height = Length.Percent(100);
            _fill.style.width = Length.Percent(p);
            _fill.style.alignSelf = Align.FlexStart;
        }
    }

    private static float ToPercent(float value, float max)
    {
        if (max <= 0f)
        {
            return 0f;
        }

        return Mathf.Round(Mathf.Clamp01(value / max) * 100f);
    }
}
