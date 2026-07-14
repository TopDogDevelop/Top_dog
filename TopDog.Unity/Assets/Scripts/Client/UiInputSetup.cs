using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_TWO_LAYER.md · docs/UI_ARCHITECTURE.md · docs/INPUT_PC_TOUCH_MAP.md
 * 本文件: UiInputSetup.cs — EventSystem + PanelRaycaster；移动端横屏
 * 【机制要点】
 * · 单输入路径；StandaloneInputModule 同时服务鼠标与触摸
 * · 移动端锁定横屏
 * 【关联】UiArtBinder · StarMapHostController · TacticalViewportInputOverlay
 * ══
 */

namespace TopDog.Client;

/// <summary>
/// Single input path: one EventSystem + PanelRaycaster per UIDocument.
/// </summary>
public static class UiInputSetup
{
    private static bool _installed;
    private static readonly System.Collections.Generic.Dictionary<UIDocument, GameObject> WiredPanels = new();

    public static void Ensure()
    {
        if (Application.isPlaying
            && (Application.isMobilePlatform
                || Application.platform == RuntimePlatform.Android
                || Application.platform == RuntimePlatform.IPhonePlayer))
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
        }

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

        UiAudioHost.Ensure();
        if (UiAudioHost.Instance != null)
        {
            UiAudioHost.Instance.RegisterDocument(document);
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
