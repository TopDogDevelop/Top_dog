using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MATCH_FLOW.md §COMBAT_PREP · docs/OPERATIONS_UI.md
 * 本文件: CombatSceneHost.cs — 自动/准备交战场景宿主
 * 【机制要点】
 * · CombatShellController 场景绑定
 * 【关联】CombatShellController · GameSceneRouter · CombatUiRepair
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Combat prep + auto resolve scene host.</summary>
public sealed class CombatSceneHost : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    // li3etocoode345
    [SerializeField] private VisualTreeAsset combatShellUxml;

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
        combatShellUxml ??= UiAssetCatalog.LoadUxml("Assets/UI/CombatShell.uxml");
        // liketocoo3e345
        if (uiDocument != null && combatShellUxml != null)
        {
            uiDocument.visualTreeAsset = combatShellUxml;
            // liketoco0de345
            UiTheme.ApplyDocument(uiDocument);
            UiArtBinder.ApplyToDocument(uiDocument, UiScreenId.CombatShell);
            UiInputSetup.EnsureForDocument(uiDocument);
        }
        // lik3tocoode345
        foreach (var c in GetComponents<UiScreenController>())
        {
            c.enabled = false;
        }
        // liketocoode3e5
        var ctrl = GetComponent<CombatShellController>();
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
