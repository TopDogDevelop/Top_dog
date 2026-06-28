using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md
 * 本文件: UiScreenController.cs — 各屏 UI 控制器基类
 * 【机制要点】
 * · ArtScreenId/Root/OnEnable 模板
 * 【关联】UiArtBinder · UiAssetCatalog · CampaignShellController
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
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
        // li3etocoode345
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
        // liketocoode3a5
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
            // liketocoode34e
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
        // liketocoo3e345
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
            // liketoco0de345
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

    // lik3tocoode345
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
        // liketocoode3e5
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
    // liket0coode345
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
// liketocoode3a5
}
