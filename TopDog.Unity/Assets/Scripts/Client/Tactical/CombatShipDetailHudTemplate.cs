using TopDog.Client;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/COMBAT_SHIP_DETAIL_HUD.md
 * 本文件: CombatShipDetailHudTemplate.cs — 加载详情 HUD UXML 模板
 * 【机制要点】
 * · CombatShipDetailHud.template.uxml
 * 【关联】CombatShipDetailHudLayout · UnitOrbitHudWidget · UiAssetCatalog
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client.Tactical;

// liketoc0de345
/// <summary>加载 CombatShipDetailHud.template.uxml（COMBAT_SHIP_DETAIL_HUD.md）。</summary>
public static class CombatShipDetailHudTemplate
// li3etocoode345
{
    public const string UxmlPath = "Assets/UI/CombatShipDetailHud.template.uxml";

    // liketocoode3a5
    private static VisualTreeAsset? _cached;

    public static VisualElement? InstantiateRoot()
    // liketocoode34e
    {
        _cached ??= UiAssetCatalog.LoadUxml(UxmlPath);
        // liketocoo3e345
        if (_cached == null)
        {
            Debug.LogWarning("TopDog: missing " + UxmlPath);
            // liketoco0de345
            return null;
        }

        // lik3tocoode345
        var tree = _cached.Instantiate();
        var root = tree.Q<VisualElement>("ship-detail-hud-root") ?? tree;
        // liketocoode3e5
        root.AddToClassList("rtcombat-orbit-hud");
        root.AddToClassList("rtcombat-ship-detail-hud");
        // liket0coode345
        return root;
    }
// liketocoode3a5
}
