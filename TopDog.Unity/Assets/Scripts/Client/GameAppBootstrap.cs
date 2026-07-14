using System;
using System.Collections;
using TopDog.Client.OnlineUpdate;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md · docs/MATCH_FLOW.md · docs/ONLINE_UPDATE.md
 * 本文件: GameAppBootstrap.cs — 应用级一次性初始化（含热更检查）
 * 【机制要点】
 * · 先 OnlineUpdateBootstrap，再 ContentRootBootstrap，再进 OutOfMatch
 * 【关联】GameAppHost · ContentRootBootstrap · OnlineUpdateBootstrap
 * ══
 */

namespace TopDog.Client;

/// <summary>First-frame setup: online update → content path → load OutOfMatch scene.</summary>
public sealed class GameAppBootstrap : MonoBehaviour
{
    private void Awake()
    {
        UiInputSetup.Ensure();
    }

    private IEnumerator Start()
    {
        // Boot.unity has no UIDocument; without one the update dialog is invisible / unusable.
        var doc = EnsureBootUpdateDocument();
        VisualElement? root = doc != null ? doc.rootVisualElement : null;

        yield return OnlineUpdateBootstrap.Run(root);

        try
        {
            ContentRootBootstrap.Apply();
            TopDog.Content.Members.MemberPortraitCatalog.Refresh();
            MemberPortraitView.InvalidateCache();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        HybridClrHotLoader.LoadAfterContentReady();

        // Scene load destroys Boot camera; keep a listener on the persistent host.
        EnsureAudioListenerOnPersistentHost();

        if (SceneManager.GetActiveScene().name == SceneCatalog.Name(TopDogSceneKind.OutOfMatch))
        {
            OutOfMatchUiRepair.Ensure();
        }

        yield return null;

        try
        {
            var router = GameSceneRouter.Instance ?? FindAnyObjectByType<GameSceneRouter>();
            if (router != null)
            {
                router.GoOutOfMatch();
            }
            else
            {
                Debug.LogError("TopDog: GameSceneRouter missing after online update — cannot enter OutOfMatch");
                OutOfMatchRuntimeBootstrap.Ensure();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            OutOfMatchRuntimeBootstrap.Ensure();
        }
    }

    private static UIDocument? EnsureBootUpdateDocument()
    {
        var existing = FindAnyObjectByType<UIDocument>();
        if (existing != null)
        {
            UiAssetCatalog.EnsurePanelSettings(existing);
            UiInputSetup.EnsureForDocument(existing);
            return existing;
        }

        var go = new GameObject("BootUpdateUI");
        var doc = go.AddComponent<UIDocument>();
        UiAssetCatalog.EnsurePanelSettings(doc);
        if (doc.panelSettings == null)
        {
            Debug.LogError("TopDog: Boot update UI missing DefaultPanelSettings");
        }

        UiInputSetup.EnsureForDocument(doc);
        return doc;
    }

    private static void EnsureAudioListenerOnPersistentHost()
    {
        if (FindAnyObjectByType<AudioListener>() != null)
        {
            return;
        }

        var host = GameAppHost.Instance != null ? GameAppHost.Instance.gameObject : null;
        if (host == null)
        {
            host = GameObject.Find("TopDogPersistent");
        }

        if (host != null)
        {
            host.AddComponent<AudioListener>();
        }
    }
}
