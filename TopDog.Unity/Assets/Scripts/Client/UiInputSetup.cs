using System.Collections.Generic;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.UIElements;



namespace TopDog.Client;



/// <summary>

/// Single input path: one EventSystem + PanelRaycaster per UIDocument.

/// See docs/UI_TWO_LAYER.md — no duplicate PanelSettings world-space input; no viewport CapturePointer.

/// </summary>

public static class UiInputSetup

{

    private static bool _installed;

    private static readonly Dictionary<UIDocument, GameObject> WiredPanels = new();



    public static void Ensure()

    {

        if (_installed && EventSystem.current != null)

        {

            return;

        }



        if (Object.FindAnyObjectByType<EventSystem>() == null)

        {

            var go = new GameObject("EventSystem");

            go.AddComponent<EventSystem>();

            go.AddComponent<StandaloneInputModule>();

            Object.DontDestroyOnLoad(go);

            Debug.Log("TopDog: created EventSystem for UI Toolkit input");

        }



        _installed = true;

    }



    public static void EnsureForDocument(UIDocument document)

    {

        Ensure();

        if (document == null)

        {

            return;

        }



        WirePanelPickers(document);



        if (document.rootVisualElement != null)

        {

            document.rootVisualElement.RegisterCallback<AttachToPanelEvent>(_ => WirePanelPickers(document));

            document.rootVisualElement.schedule.Execute(() => WirePanelPickers(document)).ExecuteLater(1);

            document.rootVisualElement.schedule.Execute(() => WirePanelPickers(document)).ExecuteLater(50);

        }

    }



    private static void WirePanelPickers(UIDocument document)

    {

        var panelRoot = document.rootVisualElement;

        if (panelRoot?.panel is not IRuntimePanel runtimePanel)

        {

            return;

        }



        var panel = panelRoot.panel;

        if (IsPanelWired(runtimePanel, panel))

        {

            return;

        }



        var eventSystem = EventSystem.current;

        if (eventSystem == null)

        {

            return;

        }



        if (WiredPanels.TryGetValue(document, out var oldGo) && oldGo != null)

        {

            Object.Destroy(oldGo);

        }



        var go = new GameObject(document.name + " UI Panel");

        go.transform.SetParent(eventSystem.transform, false);



        var handler = go.AddComponent<PanelEventHandler>();

        var raycaster = go.AddComponent<PanelRaycaster>();

        handler.panel = panel;

        raycaster.panel = panel;

        runtimePanel.selectableGameObject = go;

        WiredPanels[document] = go;



        Debug.Log("TopDog: wired PanelRaycaster for " + document.name);

    }



    private static bool IsPanelWired(IRuntimePanel runtimePanel, IPanel panel)
    {
        if (runtimePanel.selectableGameObject == null)
        {
            return false;
        }

        var handler = runtimePanel.selectableGameObject.GetComponent<PanelEventHandler>();
        return handler != null && handler.panel == panel;
    }
}

