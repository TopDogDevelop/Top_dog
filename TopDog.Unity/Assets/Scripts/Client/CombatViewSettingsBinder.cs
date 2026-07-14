using System.Collections.Generic;
using TopDog.Client.Tactical;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ⚠️ 背景链（CombatBackground* / CombatSpaceBackground* / 本文件背景设置块）：勿动，除非用户明确要求。
 * ══ 设计手册嵌入 ══
 * 权威: docs/CLIENT_GAME_SETTINGS.md §3 缓冲提交 · §4 控件名
 * 本文件: CombatViewSettingsBinder.cs — 视角 + 背景分辨率滑条 UI
 * 【机制要点】
 * · 拖动仅更新标签；ApplyPending 写入 ClientGameSettings 并 RefreshViewportNow
 * · 暂停块：MatchPauseOverlay 设置子面板（BuildInMatchSettingsOptions）
 * · 主菜单：Settings.uxml 同名控件
 * 【关联】SettingsController · MatchPauseOverlay · CombatRealtimeController
 * ══
 */

namespace TopDog.Client;

/// <summary>实时交战视角 + 背景设置；拖动/下拉缓冲，由调用方在「返回/继续」时 <see cref="ApplyPending"/>。</summary>
public sealed class CombatViewSettingsBinder
{
    public const string FovSliderName = "slider-combat-fov";
    public const string FovValueLabelName = "lbl-combat-fov-value";
    public const string BgSliderName = "slider-combat-bg-res";
    public const string BgValueLabelName = "lbl-combat-bg-res-value";
    public const string BgSetDropdownName = "dropdown-combat-bg-set";
    public const string BreathingSliderName = "slider-combat-breathing";
    public const string BreathingValueLabelName = "lbl-combat-breathing-value";

    private Slider? _fovSlider;
    private Label? _fovLabel;
    private Slider? _bgSlider;
    private Label? _bgLabel;
    private Slider? _breathingSlider;
    private Label? _breathingLabel;
    private DropdownField? _bgSetDropdown;
    private readonly List<string> _bgSetIds = new();
    private readonly List<string> _bgSetLabels = new();

    public void AppendRowsTo(VisualElement container)
    {
        container.Add(BuildFovRow());
        container.Add(BuildBreathingRow());
        container.Add(BuildBackgroundSetRow());
        container.Add(BuildBackgroundResRow());
    }

    public void Bind(VisualElement container)
    {
        _fovSlider = container.Q<Slider>(FovSliderName);
        _fovLabel = container.Q<Label>(FovValueLabelName);
        _bgSlider = container.Q<Slider>(BgSliderName);
        _bgLabel = container.Q<Label>(BgValueLabelName);
        _breathingSlider = container.Q<Slider>(BreathingSliderName);
        _breathingLabel = container.Q<Label>(BreathingValueLabelName);
        _bgSetDropdown = container.Q<DropdownField>(BgSetDropdownName);
        EnsureBackgroundSetChoices();
        LoadFromSaved();
        WireSlider(_fovSlider, _fovLabel, FormatFov);
        WireSlider(_bgSlider, _bgLabel, FormatBackgroundRes);
        WireSlider(_breathingSlider, _breathingLabel, FormatBreathing);
    }

    public void LoadFromSaved()
    {
        if (_fovSlider != null)
        {
            _fovSlider.SetValueWithoutNotify(ClientGameSettings.CombatVerticalFovDeg);
            if (_fovLabel != null)
            {
                _fovLabel.text = FormatFov(_fovSlider.value);
            }
        }

        if (_bgSlider != null)
        {
            _bgSlider.SetValueWithoutNotify(ClientGameSettings.CombatBackgroundMaxResolution);
            if (_bgLabel != null)
            {
                _bgLabel.text = FormatBackgroundRes(_bgSlider.value);
            }
        }

        if (_bgSetDropdown != null)
        {
            var pref = ClientGameSettings.CombatBackgroundSetPreference;
            var idx = _bgSetIds.IndexOf(pref);
            _bgSetDropdown.index = idx >= 0 ? idx : 0;
        }

        if (_breathingSlider != null)
        {
            _breathingSlider.SetValueWithoutNotify(ClientGameSettings.CombatViewBreathingAmplitudePercent);
            if (_breathingLabel != null)
            {
                _breathingLabel.text = FormatBreathing(_breathingSlider.value);
            }
        }
    }

    public void ApplyPending()
    {
        if (_fovSlider != null)
        {
            ClientGameSettings.SetCombatVerticalFovDeg(_fovSlider.value);
        }

        if (_bgSlider != null)
        {
            ClientGameSettings.SetCombatBackgroundMaxResolution(
                ClientGameSettings.SnapBackgroundResolution(_bgSlider.value));
        }

        if (_bgSetDropdown != null)
        {
            var idx = _bgSetDropdown.index;
            if (idx < 0 || idx >= _bgSetIds.Count)
            {
                idx = ResolveDropdownIndex(ClientGameSettings.CombatBackgroundSetPreference);
            }

            if (idx >= 0 && idx < _bgSetIds.Count)
            {
                ClientGameSettings.SetCombatBackgroundSetPreference(_bgSetIds[idx]);
            }
        }

        if (_breathingSlider != null)
        {
            ClientGameSettings.SetCombatViewBreathingAmplitudePercent(Mathf.RoundToInt(_breathingSlider.value));
        }

        RefreshCombatViewport();
    }

    private int ResolveDropdownIndex(string preference)
    {
        var idx = _bgSetIds.IndexOf(preference);
        return idx >= 0 ? idx : 0;
    }

    private VisualElement BuildFovRow() => BuildRow(
        "实时交战视角大小",
        FovValueLabelName,
        FovSliderName,
        ClientGameSettings.MinCombatVerticalFovDeg,
        ClientGameSettings.MaxCombatVerticalFovDeg,
        ClientGameSettings.DefaultCombatVerticalFovDeg);

    private VisualElement BuildBreathingRow() => BuildRow(
        "视角呼吸微动",
        BreathingValueLabelName,
        BreathingSliderName,
        ClientGameSettings.MinCombatViewBreathingPercent,
        ClientGameSettings.MaxCombatViewBreathingPercent,
        ClientGameSettings.DefaultCombatViewBreathingPercent);

    private VisualElement BuildBackgroundSetRow()
    {
        EnsureBackgroundSetChoices();

        var row = new VisualElement();
        row.AddToClassList("settings-row");

        var titleLabel = new Label("实时交战背景");
        titleLabel.AddToClassList("settings-label");
        row.Add(titleLabel);

        var dropdown = new DropdownField
        {
            name = BgSetDropdownName,
            choices = _bgSetLabels,
        };
        dropdown.index = 0;
        dropdown.AddToClassList("settings-dropdown");
        row.Add(dropdown);
        return row;
    }

    private VisualElement BuildBackgroundResRow() => BuildRow(
        "实时交战背景分辨率",
        BgValueLabelName,
        BgSliderName,
        ClientGameSettings.MinCombatBackgroundMaxRes,
        ClientGameSettings.MaxCombatBackgroundMaxRes,
        ClientGameSettings.DefaultCombatBackgroundMaxRes);

    private void EnsureBackgroundSetChoices()
    {
        if (_bgSetIds.Count == 0)
        {
            _bgSetIds.Add(ClientGameSettings.CombatBackgroundSetRandom);
            _bgSetLabels.Add("随机");
            for (var i = 0; i < CombatBackgroundCatalog.MainSetIds.Length; i++)
            {
                _bgSetIds.Add(CombatBackgroundCatalog.MainSetIds[i]);
                _bgSetLabels.Add($"{i + 1}号背景");
            }
        }

        if (_bgSetDropdown != null)
        {
            _bgSetDropdown.choices = _bgSetLabels;
        }
    }

    private static VisualElement BuildRow(
        string title,
        string valueLabelName,
        string sliderName,
        float low,
        float high,
        float defaultValue)
    {
        var row = new VisualElement();
        row.AddToClassList("settings-row");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("settings-label");
        row.Add(titleLabel);

        var valueLabel = new Label { name = valueLabelName };
        valueLabel.AddToClassList("settings-value");
        row.Add(valueLabel);

        var slider = new Slider(low, high)
        {
            name = sliderName,
            showInputField = false,
        };
        slider.SetValueWithoutNotify(defaultValue);
        slider.AddToClassList("settings-slider");
        row.Add(slider);
        return row;
    }

    private static void WireSlider(Slider? slider, Label? valueLabel, System.Func<float, string> format)
    {
        if (slider == null || valueLabel == null)
        {
            return;
        }

        slider.RegisterValueChangedCallback(evt => valueLabel.text = format(evt.newValue));
    }

    private static string FormatFov(float value) => Mathf.RoundToInt(value) + "°";

    private static string FormatBackgroundRes(float value) =>
        ClientGameSettings.SnapBackgroundResolution(value) + " px";

    private static string FormatBreathing(float value) => Mathf.RoundToInt(value) + "%";

    private static void RefreshCombatViewport()
    {
        var combat = Object.FindAnyObjectByType<CombatRealtimeController>();
        combat?.RefreshViewportNow();
    }
}
