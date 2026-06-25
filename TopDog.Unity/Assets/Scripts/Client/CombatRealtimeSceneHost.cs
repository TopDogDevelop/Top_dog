using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

/// <summary>Realtime tactical combat scene host.</summary>
public sealed class CombatRealtimeSceneHost : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset combatRealtimeUxml;

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
        combatRealtimeUxml ??= UiAssetCatalog.LoadUxml("Assets/UI/CombatRealtime.uxml");
        if (uiDocument != null && combatRealtimeUxml != null)
        {
            uiDocument.visualTreeAsset = combatRealtimeUxml;
            UiTheme.ApplyDocument(uiDocument);
            UiArtBinder.ApplyToDocument(uiDocument, UiScreenId.CombatRealtime);
            UiInputSetup.EnsureForDocument(uiDocument);
        }
        foreach (var c in GetComponents<UiScreenController>())
        {
            c.enabled = false;
        }
        var ctrl = GetComponent<CombatRealtimeController>();
        if (ctrl != null && uiDocument != null)
        {
            ctrl.enabled = true;
            ctrl.AttachToDocument(uiDocument);
        }
        GetComponent<UiViewportDriver>()?.ApplyLetterbox();
    }
}
