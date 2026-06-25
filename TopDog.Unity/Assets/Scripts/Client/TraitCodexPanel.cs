using System;
using TopDog.App;
using TopDog.Content;
using TopDog.Content.Traits;
using UnityEngine.UIElements;

namespace TopDog.Client;

public static class TraitCodexPanel
{
    public static void Populate(ScrollView scroll, SimulationCore core, Action<string> onMessage)
    {
        scroll.Clear();
        var catalog = TraitCatalog.LoadDefault();
        foreach (var t in catalog.All())
        {
            var line = DisplayLabels.TraitBilingual(t)
                       + " · order=" + t.resolutionOrder + " · " + t.resolutionPhase;
            var label = new Label(line);
            label.AddToClassList("ops-overlay-body");
            scroll.contentContainer.Add(label);
        }
        if (catalog.All().Count == 0)
        {
            scroll.contentContainer.Add(new Label("（无词条 JSON）"));
        }
    }
}
