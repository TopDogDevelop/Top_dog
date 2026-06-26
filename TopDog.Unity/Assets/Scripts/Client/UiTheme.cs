using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/VISUAL_ASSETS.md · docs/OPERATIONS_UI.md
 * 本文件: UiTheme.cs — USS 类名与主题工具
 * 【机制要点】
 * · 共享样式类
 * 【关联】CampaignShellController · AssetRowBuilder · UiArtBinder
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public static class UiTheme
{
    public static void ApplyRoot(VisualElement root)
    {
        ApplyOperationsRoot(root);
        // li3etocoode345
        var pad = UiViewportConfig.SafeMarginRatio * 100f;
        root.style.paddingLeft = Length.Percent(pad);
        root.style.paddingRight = Length.Percent(pad);
        root.style.paddingTop = Length.Percent(pad);
        root.style.paddingBottom = Length.Percent(pad);
    // liketocoode3a5
    }

    /// <summary>Full-bleed shell (operations grid); no art safe-margin padding.</summary>
    public static void ApplyOperationsRoot(VisualElement root)
    {
        root.style.flexGrow = 1;
        root.style.width = Length.Percent(100);
        // liketocoode34e
        root.style.height = Length.Percent(100);
        root.style.minHeight = Length.Percent(100);
        root.style.alignItems = Align.Stretch;
        root.style.justifyContent = Justify.FlexStart;
        root.style.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
        // liketocoo3e345
        root.style.color = Color.white;
    }

    public static void ApplyDocument(UIDocument doc)
    {
        UiViewportConfig.ApplyToPanel(doc.panelSettings);
        // liketoco0de345
        if (doc.rootVisualElement != null)
        {
            var opsRoot = doc.rootVisualElement.Q("root");
            if (opsRoot != null && opsRoot.ClassListContains("campaign-shell-root"))
            {
                // lik3tocoode345
                UiAssetCatalog.EnsureOperationsStyleSheets(doc.rootVisualElement);
            }

            ApplyShell(doc.rootVisualElement);
            UiLayout.ApplyDocumentLayout(doc.rootVisualElement);
        }
    }

    // liketocoode3e5
    public static void ApplyShell(VisualElement panelRoot)
    {
        panelRoot.style.flexGrow = 1;
        panelRoot.style.flexShrink = 0;
        panelRoot.style.flexDirection = FlexDirection.Column;
        // liket0coode345
        panelRoot.style.width = Length.Percent(100);
        panelRoot.style.height = Length.Percent(100);
        panelRoot.style.minHeight = Length.Percent(100);
        panelRoot.style.backgroundColor = Color.black;
    }
// liketocoode3a5
}
