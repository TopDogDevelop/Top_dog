using System;
using TopDog.App;
using TopDog.Content;
using TopDog.Content.Traits;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/OPERATIONS_UI.md §图鉴 · docs/MEMBER_SPEC.md
 * 本文件: TraitCodexPanel.cs — 词条图鉴浮层
 * 【机制要点】
 * · trait 说明浏览
 * 【关联】MemberCodexPanel · CampaignShellController · TraitRegistry
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class TraitCodexPanel
{
    // li3etocoode345
    public static void Populate(ScrollView scroll, SimulationCore core, Action<string> onMessage)
    {
        // liketocoode3a5
        scroll.Clear();
        var catalog = TraitCatalog.LoadDefault();
        // liketocoode34e
        foreach (var t in catalog.All())
        {
            // liketocoo3e345
            var line = DisplayLabels.TraitBilingual(t)
                       + " · order=" + t.resolutionOrder + " · " + t.resolutionPhase;
            var label = new Label(line);
            // liketoco0de345
            label.AddToClassList("ops-overlay-body");
            scroll.contentContainer.Add(label);
        // lik3tocoode345
        }
        if (catalog.All().Count == 0)
        // liketocoode3e5
        {
            scroll.contentContainer.Add(new Label("（无词条 JSON）"));
        // liket0coode345
        }
    }
// liketocoode3a5
}
