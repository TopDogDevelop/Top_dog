using System.Text;
using TopDog.Sim.Skirmish;
using TopDog.Sim.State;
using UnityEngine.UIElements;

namespace TopDog.Client;

public static class SkirmishResultPanel
{
    public static void AppendTo(VisualElement parent, GameState state)
    {
        if (state.worldline.type != WorldlineType.LEGION_SKIRMISH || state.skirmish == null)
        {
            return;
        }

        var block = new VisualElement();
        block.AddToClassList("skirmish-result-block");

        var title = new Label("军团约战结算");
        title.style.fontSize = 22;
        title.style.color = UnityEngine.Color.white;
        title.style.marginBottom = 8;
        block.Add(title);

        if (!string.IsNullOrWhiteSpace(state.skirmish.endReason))
        {
            var reason = new Label(state.skirmish.endReason);
            reason.style.fontSize = 16;
            reason.style.whiteSpace = WhiteSpace.Normal;
            reason.style.marginBottom = 8;
            block.Add(reason);
        }

        foreach (var legion in state.legions)
        {
            if (legion.legionId == null)
            {
                continue;
            }

            state.skirmish.scores.TryGetValue(legion.legionId, out var score);
            var line = new Label($"{legion.displayName ?? legion.legionId} · 击毁积分 {score}");
            line.style.fontSize = 16;
            line.style.marginBottom = 4;
            block.Add(line);
        }

        if (state.skirmish.scoreLedger.Count > 0)
        {
            var ledgerTitle = new Label("积分明细（可溯源）");
            ledgerTitle.style.fontSize = 18;
            ledgerTitle.style.marginTop = 12;
            ledgerTitle.style.marginBottom = 6;
            block.Add(ledgerTitle);

            foreach (var entry in state.skirmish.scoreLedger)
            {
                var sb = new StringBuilder();
                sb.Append($"+{entry.points} · {entry.tonnageClass ?? "?"} · {entry.targetHullId ?? "?"}");
                sb.Append($" · bf={entry.battlefieldId ?? "?"} · t={entry.timeSec:0}s");
                var row = new Label(sb.ToString());
                row.style.fontSize = 14;
                row.style.whiteSpace = WhiteSpace.Normal;
                row.style.marginBottom = 2;
                block.Add(row);
            }
        }

        parent.Add(block);
    }
}
