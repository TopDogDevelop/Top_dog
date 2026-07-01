using TopDog.Content;
using TopDog.Content.Banter;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>单行伴聊富文本：游戏内名 + #色 /表情 正文。</summary>
public static class BanterLogLineView
{
    public static VisualElement Build(GameState state, CompanionLogEntry entry)
    {
        var row = new VisualElement();
        row.AddToClassList("banter-log-line");

        var speaker = DisplayLabels.ResolveBanterSpeakerName(state, entry.memberId);
        var speakerLabel = new Label(speaker + "：");
        speakerLabel.AddToClassList("banter-speaker");
        speakerLabel.AddToClassList("ops-event-feed");
        row.Add(speakerLabel);

        var body = new VisualElement();
        body.AddToClassList("banter-log-body");
        body.style.flexDirection = FlexDirection.Row;
        body.style.flexWrap = Wrap.Wrap;
        row.Add(body);

        var parsed = BanterInlineMarkupParser.Parse(entry.text);
        var color = BanterStyleCatalog.ResolveColorHex(parsed.ColorId);
        foreach (var run in parsed.Runs)
        {
            if (run.Kind == BanterMarkupRunKind.Emote)
            {
                body.Add(BuildEmote(run.EmoteId));
                continue;
            }

            if (string.IsNullOrEmpty(run.Text))
            {
                continue;
            }

            var textLabel = new Label(run.Text);
            textLabel.AddToClassList("banter-text");
            textLabel.AddToClassList("ops-event-feed");
            if (!string.IsNullOrEmpty(color)
                && ColorUtility.TryParseHtmlString(color, out var unityColor))
            {
                textLabel.style.color = unityColor;
            }

            body.Add(textLabel);
        }

        return row;
    }

    private static VisualElement BuildEmote(int emoteId)
    {
        var box = new VisualElement();
        box.AddToClassList("banter-emote");
        box.style.width = 16;
        box.style.height = 16;
        box.style.marginLeft = 1;
        box.style.marginRight = 1;
        box.style.alignSelf = Align.Center;
        box.style.backgroundColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);

        var fallback = new Label(emoteId.ToString());
        fallback.style.fontSize = 9;
        fallback.style.unityTextAlign = TextAnchor.MiddleCenter;
        fallback.AddToClassList("banter-emote-fallback");
        box.Add(fallback);
        return box;
    }
}
