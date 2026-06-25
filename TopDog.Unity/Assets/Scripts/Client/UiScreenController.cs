using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

public abstract class UiScreenController : MonoBehaviour
{
    protected UIDocument? Doc;
    protected VisualElement? Root;
    private bool _boundThisAttach;
    private readonly List<(Button btn, Action handler)> _clickHandlers = new();

    public abstract UiScreenId ArtScreenId { get; }

    protected virtual bool UseSafeAreaInsets => true;

    public void Detach()
    {
        ClearBindings();
        Doc = null;
        Root = null;
        _boundThisAttach = false;
    }

    public void AttachToDocument(UIDocument document)
    {
        Doc = document;
        _boundThisAttach = false;
        ClearBindings();
        if (document.rootVisualElement == null)
        {
            Debug.LogWarning($"{GetType().Name}: rootVisualElement is null");
            return;
        }

        UiLayout.ApplyDocumentLayout(document.rootVisualElement);
        UiInputSetup.EnsureForDocument(document);
        document.rootVisualElement.schedule.Execute(TryBind).ExecuteLater(0);
        document.rootVisualElement.schedule.Execute(TryBind).ExecuteLater(16);
        document.rootVisualElement.schedule.Execute(TryBind).ExecuteLater(50);
    }

    private void TryBind()
    {
        if (Doc == null)
        {
            return;
        }

        var panelRoot = Doc.rootVisualElement;
        if (panelRoot == null)
        {
            return;
        }

        UiLayout.ApplyDocumentLayout(panelRoot);

        Root = panelRoot.Q("root") ?? panelRoot.Q(className: "screen-root") ?? panelRoot;
        if (Root.childCount == 0)
        {
            return;
        }

        if (_boundThisAttach)
        {
            return;
        }

        ClearBindings();
        if (UseSafeAreaInsets)
        {
            UiTheme.ApplyRoot(Root);
        }
        else
        {
            UiTheme.ApplyOperationsRoot(Root);
        }

        UiArtBinder.ApplyScreen(Root, ArtScreenId);
        Bind(Root);
        GetComponent<UiViewportDriver>()?.ApplyLetterbox();
        _boundThisAttach = true;
        Debug.Log($"{GetType().Name}: bound UI on '{Root.name}'");
    }

    protected abstract void Bind(VisualElement root);

    protected virtual void OnDisable()
    {
    }

    protected void OnClick(VisualElement root, string name, Action action)
    {
        var btn = root.Q<Button>(name);
        if (btn == null)
        {
            Debug.LogWarning($"{GetType().Name}: button '{name}' not found");
            return;
        }

        btn.focusable = true;
        btn.pickingMode = PickingMode.Position;

        Action handler = () =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        };

        btn.clicked += handler;
        _clickHandlers.Add((btn, handler));
    }

    private void ClearBindings()
    {
        foreach (var (btn, handler) in _clickHandlers)
        {
            if (btn != null)
            {
                btn.clicked -= handler;
            }
        }

        _clickHandlers.Clear();
    }
}
