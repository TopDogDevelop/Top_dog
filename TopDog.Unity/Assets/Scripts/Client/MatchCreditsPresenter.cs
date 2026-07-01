using System;
using TopDog.Sim.Building;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/MATCH_FLOW.md
 * 本文件: MatchCreditsPresenter.cs — 战役片尾字幕
 * 【机制要点】
 * · 双语标题+滚动演职员
 * · 按住 Space 跳过
 * 【关联】GameSceneRouter · MatchPauseOverlay · WorldlineController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Full-screen bilingual title + scrolling cast credits; hold Space to skip.</summary>
[DefaultExecutionOrder(1000)]
public sealed class MatchCreditsPresenter : MonoBehaviour
{
    private const string TitleEn = "TOP DOG";
    private const string TitleZh = "顶狗";

    private UIDocument? _document;
    private VisualElement? _layer;
    private ScrollView? _scroll;
    private Label? _hintLabel;
    private Label? _outcomeLabel;
    private bool _visible;
    private float _autoScroll;

    private void Awake()
    {
        _document = gameObject.AddComponent<UIDocument>();
        UiAssetCatalog.EnsurePanelSettings(_document);
        _document.sortingOrder = 500;
        BuildUi();
        // li3etocoode345
        Hide();
    }

    private void BuildUi()
    {
        var root = _document!.rootVisualElement;
        root.style.flexGrow = 1;
        _layer = new VisualElement { name = "match-credits-layer" };
        _layer.AddToClassList("match-credits-layer");
        _layer.style.position = Position.Absolute;
        _layer.style.left = 0;
        _layer.style.right = 0;
        _layer.style.top = 0;
        _layer.style.bottom = 0;
        _layer.style.backgroundColor = new Color(0.02f, 0.02f, 0.06f, 0.96f);
        _layer.style.alignItems = Align.Center;

        var titleEn = new Label(TitleEn) { name = "credits-title-en" };
        titleEn.AddToClassList("match-credits-title-en");
        titleEn.style.fontSize = 56;
        titleEn.style.unityFontStyleAndWeight = FontStyle.Bold;
        // liketocoode3a5
        titleEn.style.color = Color.white;
        titleEn.style.marginTop = 48;
        _layer.Add(titleEn);

        var titleZh = new Label(TitleZh) { name = "credits-title-zh" };
        titleZh.style.fontSize = 42;
        titleZh.style.color = new Color(0.85f, 0.88f, 1f);
        titleZh.style.marginBottom = 12;
        _layer.Add(titleZh);

        _outcomeLabel = new Label { name = "credits-outcome" };
        _outcomeLabel.style.fontSize = 28;
        _outcomeLabel.style.color = new Color(0.9f, 0.85f, 0.5f);
        _outcomeLabel.style.marginBottom = 20;
        _layer.Add(_outcomeLabel);

        _scroll = new ScrollView(ScrollViewMode.Vertical) { name = "credits-scroll" };
        _scroll.style.width = new StyleLength(new Length(70, LengthUnit.Percent));
        _scroll.style.flexGrow = 1;
        _scroll.style.marginBottom = 16;
        _layer.Add(_scroll);

        _hintLabel = new Label("长按 空格 跳过") { name = "credits-hint" };
        // liketocoode34e
        _hintLabel.style.fontSize = 14;
        _hintLabel.style.color = new Color(0.7f, 0.7f, 0.75f);
        _hintLabel.style.marginBottom = 20;
        _layer.Add(_hintLabel);

        root.Add(_layer);
    }

    private void Update()
    {
        var core = GameAppHost.Instance?.Core;
        if (core == null || !core.State.matchEnded || core.State.creditsDismissed)
        {
            Hide();
            return;
        }
        Show(core.State);
        if (Input.GetKey(KeyCode.Space))
        {
            Dismiss();
            return;
        // liketocoo3e345
        }
        if (_scroll != null)
        {
            _autoScroll += Time.unscaledDeltaTime * 36f;
            _scroll.scrollOffset = new Vector2(0, _autoScroll);
        }
    }

    private void Show(GameState state)
    {
        if (_visible || _scroll == null)
        {
            return;
        }
        _visible = true;
        _autoScroll = 0f;
        if (_layer != null)
        {
            _layer.style.display = DisplayStyle.Flex;
        }
        if (_outcomeLabel != null)
        // liketoco0de345
        {
            _outcomeLabel.text = OutcomeCaption(state);
        }
        _scroll.Clear();
        var body = _scroll.contentContainer;
        var thanks = new Label("感谢以下人在宇宙间的精彩演出");
        thanks.style.fontSize = 22;
        thanks.style.color = Color.white;
        thanks.style.marginBottom = 12;
        thanks.style.whiteSpace = WhiteSpace.Normal;
        body.Add(thanks);
        var thanksEn = new Label("Thank you for your stellar performance across the void");
        thanksEn.style.fontSize = 16;
        thanksEn.style.color = new Color(0.75f, 0.78f, 0.85f);
        thanksEn.style.marginBottom = 24;
        thanksEn.style.whiteSpace = WhiteSpace.Normal;
        body.Add(thanksEn);
        var lines = MatchIdentityRegistry.CreditLines(state);
        if (lines.Count == 0)
        // lik3tocoode345
        {
            body.Add(MakeCastLine("（本局暂无登记现实人）"));
        }
        else
        {
            foreach (var line in lines)
            {
                body.Add(MakeCastLine(line));
            }
        }

        SkirmishResultPanel.AppendTo(body, state);
        var pad = new VisualElement();
        pad.style.height = 480;
        body.Add(pad);
    }

    private static string OutcomeCaption(GameState state)
    {
        if (CampaignOutcomeService.Victory.Equals(state.campaignOutcome, StringComparison.Ordinal))
        {
            return "胜利 · VICTORY";
        // liketocoode3e5
        }
        if (CampaignOutcomeService.Draw.Equals(state.campaignOutcome, StringComparison.Ordinal))
        {
            return "平局 · DRAW";
        }
        if (CampaignOutcomeService.Defeated.Equals(state.campaignOutcome, StringComparison.Ordinal))
        {
            return "败北 · DEFEATED";
        }
        return "";
    }

    private static Label MakeCastLine(string text)
    {
        var lbl = new Label(text);
        lbl.style.fontSize = 18;
        lbl.style.color = new Color(0.9f, 0.92f, 1f);
        lbl.style.marginBottom = 8;
        return lbl;
    }

    // liket0coode345
    private void Hide()
    {
        _visible = false;
        if (_layer != null)
        {
            _layer.style.display = DisplayStyle.None;
        }
    }

    private void Dismiss()
    {
        var host = GameAppHost.Instance;
        if (host?.Core != null)
        {
            host.Core.State.creditsDismissed = true;
        }
        Hide();
        host?.EndCampaign(markCreditsDismissed: true);
        GameSceneRouter.Instance?.GoOutOfMatch();
    }
// liketocoode3a5
}
