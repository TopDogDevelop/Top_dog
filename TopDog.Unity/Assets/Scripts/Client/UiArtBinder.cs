using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>
/// Applies <see cref="IUiArtSkin"/> to UXML trees and enforces two-layer viewport input rules.
/// Elements: class {@code art-slot-{slotId}} · viewport hosts: {@code art-viewport-host}.
/// </summary>
public static class UiArtBinder
{
    private const string ArtSlotPrefix = "art-slot-";

    private static readonly Dictionary<UiScreenId, (string ussClass, string slotId)[]> ConventionMap =
        new()
        {
            [UiScreenId.MainMenu] = new[]
            {
                ("menu-button", UiArtSlots.BtnMenu),
                ("menu-button-wide", UiArtSlots.BtnMenuWide),
                ("screen-title", UiArtSlots.Logo),
            },
            [UiScreenId.Worldline] = new[]
            {
                ("menu-button", UiArtSlots.BtnMenu),
                ("menu-button-wide", UiArtSlots.BtnMenuWide),
            },
            [UiScreenId.Settings] = new[]
            {
                ("menu-button", UiArtSlots.BtnMenu),
            },
            [UiScreenId.JoinLan] = new[]
            {
                ("menu-button", UiArtSlots.BtnMenu),
                ("menu-button-wide", UiArtSlots.BtnMenuWide),
            },
            [UiScreenId.StoryLevels] = new[]
            {
                ("menu-button", UiArtSlots.BtnMenu),
                ("menu-button-wide", UiArtSlots.BtnMenuWide),
            },
            [UiScreenId.CustomLobby] = new[]
            {
                ("menu-button-wide", UiArtSlots.BtnMenuWide),
                ("lobby-secondary-btn", UiArtSlots.BtnPrimary),
            },
            [UiScreenId.CampaignShell] = new[]
            {
                ("ops-top-btn", UiArtSlots.BtnPrimary),
                ("ops-small-btn", UiArtSlots.BtnMenu),
                ("ops-recruit-btn", UiArtSlots.BtnMenuWide),
            },
            [UiScreenId.CombatShell] = new[]
            {
                ("menu-button", UiArtSlots.BtnMenu),
                ("menu-button-wide", UiArtSlots.BtnMenuWide),
            },
            [UiScreenId.CombatRealtime] = new[]
            {
                ("menu-button-wide", UiArtSlots.BtnMenuWide),
            },
        };

    public static void ApplyScreen(VisualElement root, UiScreenId screen)
    {
        if (root == null)
        {
            return;
        }

        var skin = UiArtCatalog.Active;
        ApplyStyleOverrides(root, screen, skin);
        ApplyExplicitArtSlots(root, screen, skin);
        ApplyConventionSlots(root, screen, skin);
        EnforceTwoLayerInput(root);
    }

    public static void ApplyToDocument(UIDocument document, UiScreenId screen)
    {
        if (document?.rootVisualElement == null)
        {
            return;
        }

        var skin = UiArtCatalog.Active;
        var global = skin.GetGlobalStyleOverride();
        if (global != null && !document.rootVisualElement.styleSheets.Contains(global))
        {
            document.rootVisualElement.styleSheets.Add(global);
        }

        ApplyScreen(document.rootVisualElement, screen);
    }

    public static void SetBackgroundFromSkin(
        VisualElement element,
        UiScreenId screen,
        string slotId,
        IUiArtSkin? skin = null)
    {
        skin ??= UiArtCatalog.Active;
        var sprite = skin.GetSprite(screen, slotId);
        if (sprite != null)
        {
            element.style.backgroundImage = new StyleBackground(sprite);
            return;
        }

        var tex = skin.GetTexture(screen, slotId);
        if (tex != null)
        {
            element.style.backgroundImage = new StyleBackground(tex);
        }
    }

    private static void ApplyStyleOverrides(VisualElement root, UiScreenId screen, IUiArtSkin skin)
    {
        var screenSheet = skin.GetScreenStyleOverride(screen);
        if (screenSheet != null && !root.styleSheets.Contains(screenSheet))
        {
            root.styleSheets.Add(screenSheet);
        }
    }

    private static void ApplyExplicitArtSlots(VisualElement root, UiScreenId screen, IUiArtSkin skin)
    {
        var queue = new Queue<VisualElement>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var el = queue.Dequeue();
            foreach (var child in el.Children())
            {
                queue.Enqueue(child);
            }

            foreach (var className in el.GetClasses())
            {
                if (!className.StartsWith(ArtSlotPrefix))
                {
                    continue;
                }

                var slotId = className.Substring(ArtSlotPrefix.Length);
                if (string.IsNullOrEmpty(slotId) || slotId == "layer")
                {
                    continue;
                }

                SetBackgroundFromSkin(el, screen, slotId, skin);
            }
        }
    }

    private static void ApplyConventionSlots(VisualElement root, UiScreenId screen, IUiArtSkin skin)
    {
        if (!ConventionMap.TryGetValue(screen, out var rules))
        {
            return;
        }

        foreach (var (ussClass, slotId) in rules)
        {
            root.Query(className: ussClass).ForEach(el =>
            {
                if (skin.GetTexture(screen, slotId) == null && skin.GetSprite(screen, slotId) == null)
                {
                    return;
                }

                SetBackgroundFromSkin(el, screen, slotId, skin);
            });
        }
    }

    /// <summary>Viewport compute hosts must not capture pointer; only child Buttons receive clicks.</summary>
    public static void EnforceTwoLayerInput(VisualElement root)
    {
        root.Query(className: "art-viewport-host").ForEach(el =>
        {
            el.pickingMode = PickingMode.Ignore;
        });

        foreach (var name in new[] { "star-map-host", "tactical-viewport-host", "rtcombat-viewport" })
        {
            var host = root.Q(name);
            if (host != null)
            {
                host.pickingMode = PickingMode.Ignore;
                if (!host.ClassListContains("art-viewport-host"))
                {
                    host.AddToClassList("art-viewport-host");
                }
            }
        }

        root.Query(className: "art-layer").ForEach(el => el.pickingMode = PickingMode.Ignore);
    }
}
