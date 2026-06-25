using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

public static class UiTheme
{
    public static void ApplyRoot(VisualElement root)
    {
        ApplyOperationsRoot(root);
        var pad = UiViewportConfig.SafeMarginRatio * 100f;
        root.style.paddingLeft = Length.Percent(pad);
        root.style.paddingRight = Length.Percent(pad);
        root.style.paddingTop = Length.Percent(pad);
        root.style.paddingBottom = Length.Percent(pad);
    }

    /// <summary>Full-bleed shell (operations grid); no art safe-margin padding.</summary>
    public static void ApplyOperationsRoot(VisualElement root)
    {
        root.style.flexGrow = 1;
        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);
        root.style.minHeight = Length.Percent(100);
        root.style.alignItems = Align.Stretch;
        root.style.justifyContent = Justify.FlexStart;
        root.style.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
        root.style.color = Color.white;
    }

    public static void ApplyDocument(UIDocument doc)
    {
        UiViewportConfig.ApplyToPanel(doc.panelSettings);
        if (doc.rootVisualElement != null)
        {
            var opsRoot = doc.rootVisualElement.Q("root");
            if (opsRoot != null && opsRoot.ClassListContains("campaign-shell-root"))
            {
                UiAssetCatalog.EnsureOperationsStyleSheets(doc.rootVisualElement);
            }

            ApplyShell(doc.rootVisualElement);
            UiLayout.ApplyDocumentLayout(doc.rootVisualElement);
        }
    }

    public static void ApplyShell(VisualElement panelRoot)
    {
        panelRoot.style.flexGrow = 1;
        panelRoot.style.flexShrink = 0;
        panelRoot.style.flexDirection = FlexDirection.Column;
        panelRoot.style.width = Length.Percent(100);
        panelRoot.style.height = Length.Percent(100);
        panelRoot.style.minHeight = Length.Percent(100);
        panelRoot.style.backgroundColor = Color.black;
    }
}
