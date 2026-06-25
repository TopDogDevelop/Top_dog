using TopDog.Client;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client.Tactical;

/// <summary>加载 CombatShipDetailHud.template.uxml（COMBAT_SHIP_DETAIL_HUD.md）。</summary>
public static class CombatShipDetailHudTemplate
{
    public const string UxmlPath = "Assets/UI/CombatShipDetailHud.template.uxml";

    private static VisualTreeAsset? _cached;

    public static VisualElement? InstantiateRoot()
    {
        _cached ??= UiAssetCatalog.LoadUxml(UxmlPath);
        if (_cached == null)
        {
            Debug.LogWarning("TopDog: missing " + UxmlPath);
            return null;
        }

        var tree = _cached.Instantiate();
        var root = tree.Q<VisualElement>("ship-detail-hud-root") ?? tree;
        root.AddToClassList("rtcombat-orbit-hud");
        root.AddToClassList("rtcombat-ship-detail-hud");
        return root;
    }
}
