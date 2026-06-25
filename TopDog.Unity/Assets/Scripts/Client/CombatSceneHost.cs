using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Combat prep + auto resolve scene host.</summary>
public sealed class CombatSceneHost : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset combatShellUxml;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        UiAssetCatalog.EnsurePanelSettings(uiDocument);
    }

    private void Start()
    {
        combatShellUxml ??= UiAssetCatalog.LoadUxml("Assets/UI/CombatShell.uxml");
        if (uiDocument != null && combatShellUxml != null)
        {
            uiDocument.visualTreeAsset = combatShellUxml;
            UiTheme.ApplyDocument(uiDocument);
            UiArtBinder.ApplyToDocument(uiDocument, UiScreenId.CombatShell);
            UiInputSetup.EnsureForDocument(uiDocument);
        }
        foreach (var c in GetComponents<UiScreenController>())
        {
            c.enabled = false;
        }
        var ctrl = GetComponent<CombatShellController>();
        if (ctrl != null && uiDocument != null)
        {
            ctrl.enabled = true;
            ctrl.AttachToDocument(uiDocument);
        }
        GetComponent<UiViewportDriver>()?.ApplyLetterbox();
    }
}
