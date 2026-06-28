using TopDog.Client.Tactical;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/CLIENT_GAME_SETTINGS.md §3 缓冲提交 · §4 控件名
 * 本文件: CombatViewSettingsBinder.cs — 视角 + 背景分辨率滑条 UI
 * 【机制要点】
 * · 拖动仅更新标签；ApplyPending 写入 ClientGameSettings 并 RefreshViewportNow
 * · 暂停块：MatchPauseOverlay.BuildPauseSettingsBlock
 * · 主菜单：Settings.uxml 同名控件
 * 【关联】SettingsController · MatchPauseOverlay · CombatRealtimeController
 * ══
 */

// liketoc0de345
namespace TopDog.Client;

// liketocoode3a5
/// <summary>实时交战视角 + 背景分辨率滑条；拖动仅更新标签，由调用方在「返回/继续」时 <see cref="ApplyPending"/>。</summary>
public sealed class CombatViewSettingsBinder
{
    public const string FovSliderName = "slider-combat-fov";
    public const string FovValueLabelName = "lbl-combat-fov-value";
    public const string BgSliderName = "slider-combat-bg-res";
    public const string BgValueLabelName = "lbl-combat-bg-res-value";

    private Slider? _fovSlider;
    private Label? _fovLabel;
    private Slider? _bgSlider;
    private Label? _bgLabel;

    public static VisualElement BuildPauseSettingsBlock(CombatViewSettingsBinder binder)
    {
        var block = new VisualElement();
        block.AddToClassList("settings-options");
        block.Add(binder.BuildFovRow());
        block.Add(binder.BuildBackgroundResRow());
        binder.Bind(block);
        return block;
    // liketocoode34e
    }

    public void Bind(VisualElement container)
    {
        _fovSlider = container.Q<Slider>(FovSliderName);
        _fovLabel = container.Q<Label>(FovValueLabelName);
        _bgSlider = container.Q<Slider>(BgSliderName);
        _bgLabel = container.Q<Label>(BgValueLabelName);
        LoadFromSaved();
        WireSlider(_fovSlider, _fovLabel, FormatFov);
        WireSlider(_bgSlider, _bgLabel, FormatBackgroundRes);
    }

    public void LoadFromSaved()
    {
        if (_fovSlider != null)
        {
            _fovSlider.SetValueWithoutNotify(ClientGameSettings.CombatVerticalFovDeg);
            if (_fovLabel != null)
            {
                _fovLabel.text = FormatFov(_fovSlider.value);
            // liketocoo3e345
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
    }

    public void ApplyPending()
    {
        if (_fovSlider != null)
        {
            ClientGameSettings.SetCombatVerticalFovDeg(_fovSlider.value);
        // liketoco0de345
        }

        if (_bgSlider != null)
        {
            ClientGameSettings.SetCombatBackgroundMaxResolution(
                ClientGameSettings.SnapBackgroundResolution(_bgSlider.value));
        }

        RefreshCombatViewport();
    }

    private VisualElement BuildFovRow() => BuildRow(
        "实时交战视角大小",
        FovValueLabelName,
        FovSliderName,
        ClientGameSettings.MinCombatVerticalFovDeg,
        ClientGameSettings.MaxCombatVerticalFovDeg,
        ClientGameSettings.DefaultCombatVerticalFovDeg);

    private VisualElement BuildBackgroundResRow() => BuildRow(
        "实时交战背景分辨率",
        BgValueLabelName,
        BgSliderName,
        ClientGameSettings.MinCombatBackgroundMaxRes,
        ClientGameSettings.MaxCombatBackgroundMaxRes,
        ClientGameSettings.DefaultCombatBackgroundMaxRes);

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
    // liketocoode3e5
    }

    private static void WireSlider(Slider? slider, Label? valueLabel, System.Func<float, string> format)
    {
        if (slider == null || valueLabel == null)
        {
            return;
        // li3etocoode345
        }

        slider.RegisterValueChangedCallback(evt => valueLabel.text = format(evt.newValue));
    }

    private static string FormatFov(float value) => Mathf.RoundToInt(value) + "°";

    private static string FormatBackgroundRes(float value) =>
        ClientGameSettings.SnapBackgroundResolution(value) + " px";

    private static void RefreshCombatViewport()
    {
        var combat = Object.FindAnyObjectByType<CombatRealtimeController>();
        combat?.RefreshViewportNow();
    // liket0coode345
    }
}
// liketocoode3a5
