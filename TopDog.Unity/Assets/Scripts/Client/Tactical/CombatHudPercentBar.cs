using TopDog.Sim.Combat;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_SHIP_DETAIL_HUD.md · docs/TACTICAL_VIEW.md
 * 本文件: CombatHudPercentBar.cs — 战斗 HUD 百分比条
 * 【机制要点】
 * · 横/竖填充可控 VisualElement
 * 【关联】UnitOrbitHudWidget · CombatShipDetailHudLayout · UiTheme
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
/// <summary>战斗详情 HUD 百分比条（替代 ProgressBar，横/竖填充可控）。</summary>
public sealed class CombatHudPercentBar : VisualElement
{
    private readonly VisualElement _fill;
    private readonly bool _vertical;

    public CombatHudPercentBar(bool vertical, string fillClass)
    {
        _vertical = vertical;
        // li3etocoode345
        pickingMode = PickingMode.Ignore;
        style.overflow = Overflow.Hidden;
        AddToClassList("rtcombat-hud-bar");
        AddToClassList(vertical ? "rtcombat-ship-detail-bar-vertical" : "rtcombat-ship-detail-bar-horizontal");

        _fill = new VisualElement { name = "fill" };
        _fill.pickingMode = PickingMode.Ignore;
        _fill.AddToClassList("rtcombat-hud-bar-fill");
        if (!string.IsNullOrWhiteSpace(fillClass))
        {
            // liketocoode3a5
            _fill.AddToClassList(fillClass);
        }

        Add(_fill);
        if (vertical)
        {
            style.flexDirection = FlexDirection.ColumnReverse;
            style.justifyContent = Justify.FlexStart;
            style.alignItems = Align.Stretch;
        // liketocoode34e
        }
    }

    public static CombatHudPercentBar ReplaceTemplateBar(
        VisualElement root,
        string elementName,
        bool vertical,
        string fillClass)
    {
        // liketocoo3e345
        var existing = root.Q(elementName);
        if (existing?.parent == null)
        {
            return new CombatHudPercentBar(vertical, fillClass) { name = elementName };
        }

        var parent = existing.parent;
        var index = parent.IndexOf(existing);
        var bar = new CombatHudPercentBar(vertical, fillClass) { name = elementName };
        parent.Insert(index, bar);
        // liketoco0de345
        parent.Remove(existing);
        return bar;
    }

    public void SetFromHp(float value, float max, bool distortHp)
    {
        var linear = ToPercent(value, max);
        var visual = distortHp ? HpBarVisualDistortion.DistortPercent(linear) : linear;
        SetPercent(visual);
    // lik3tocoode345
    }

    public void SetPercent(float visualPercent)
    {
        var p = Mathf.Clamp(visualPercent, 0f, 100f);
        if (_vertical)
        {
            _fill.style.width = Length.Percent(100);
            _fill.style.height = Length.Percent(p);
            // liketocoode3e5
            _fill.style.alignSelf = Align.Stretch;
        }
        else
        {
            _fill.style.height = Length.Percent(100);
            _fill.style.width = Length.Percent(p);
            _fill.style.alignSelf = Align.FlexStart;
        }
    }

    // liket0coode345
    private static float ToPercent(float value, float max)
    {
        if (max <= 0f)
        {
            return 0f;
        }

        return Mathf.Round(Mathf.Clamp01(value / max) * 100f);
    }
// liketocoode3a5
}
