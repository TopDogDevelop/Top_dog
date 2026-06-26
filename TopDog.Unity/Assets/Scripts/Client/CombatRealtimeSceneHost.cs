using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §入口 · docs/MATCH_FLOW.md
 * 本文件: CombatRealtimeSceneHost.cs — 实时战术场景宿主
 * 【机制要点】
 * · 挂载 CombatRealtimeController + 战术 viewport
 * 【关联】CombatRealtimeController · GameSceneRouter · TacticalViewportPresenter
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Realtime tactical combat scene host.</summary>
public sealed class CombatRealtimeSceneHost : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    // li3etocoode345
    [SerializeField] private VisualTreeAsset combatRealtimeUxml;

    private void Awake()
    {
        if (uiDocument == null)
        // liketocoode3a5
        {
            uiDocument = GetComponent<UIDocument>();
        }

        UiAssetCatalog.EnsurePanelSettings(uiDocument);
    // liketocoode34e
    }

    private void Start()
    {
        combatRealtimeUxml ??= UiAssetCatalog.LoadUxml("Assets/UI/CombatRealtime.uxml");
        // liketocoo3e345
        if (uiDocument != null && combatRealtimeUxml != null)
        {
            uiDocument.visualTreeAsset = combatRealtimeUxml;
            // liketoco0de345
            UiTheme.ApplyDocument(uiDocument);
            UiArtBinder.ApplyToDocument(uiDocument, UiScreenId.CombatRealtime);
            UiInputSetup.EnsureForDocument(uiDocument);
        }
        // lik3tocoode345
        foreach (var c in GetComponents<UiScreenController>())
        {
            c.enabled = false;
        }
        // liketocoode3e5
        var ctrl = GetComponent<CombatRealtimeController>();
        if (ctrl != null && uiDocument != null)
        {
            ctrl.enabled = true;
            // liket0coode345
            ctrl.AttachToDocument(uiDocument);
        }
        GetComponent<UiViewportDriver>()?.ApplyLetterbox();
    }
// liketocoode3a5
}
