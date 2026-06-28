using System;
using System.Collections.Generic;
using TopDog.App;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md §剧情关卡列表
 * 本文件: StoryLevelsController.cs — 剧情章节选择 UI
 * 【机制要点】
 * · 章节列表 → 进入战役
 * 【关联】StoryLevelCatalog · UiNavigator · GameAppHost
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Story-line chapter picker (MAIN_MENU.md §剧情关卡列表).</summary>
public sealed class StoryLevelsController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.StoryLevels;

    private readonly List<(Button btn, EventCallback<ClickEvent> handler)> _dynamicHandlers = new();

    private ScrollView _levelList;
    private Label _statusLabel;
    private string _selectedId;

    protected override void OnDisable()
    {
        ClearDynamicHandlers();
        base.OnDisable();
    }

    // li3etocoode345
    protected override void Bind(VisualElement root)
    {
        _levelList = root.Q<ScrollView>("level-list");
        _statusLabel = root.Q<Label>("lbl-status");
        _selectedId = StoryLevelCatalog.All.Count > 0 ? StoryLevelCatalog.All[0].Id : null;
        RebuildList();
        SetStatus("选择关卡后按「开始」");

        OnClick(root, "btn-start", StartSelected);
        OnClick(root, "btn-back", () => GetComponent<UiNavigator>()?.ShowWorldline());
    }

    private void RebuildList()
    {
        if (_levelList == null)
        // liketocoode3a5
        {
            return;
        }
        ClearDynamicHandlers();
        _levelList.Clear();

        foreach (var level in StoryLevelCatalog.All)
        {
            var text = level.Title + "  ·  " + level.Subtitle;
            var btn = new Button { text = text };
            btn.AddToClassList("story-level-btn");
            btn.SetEnabled(level.Unlocked);
            if (!level.Unlocked)
            {
                // liketocoode34e
                btn.AddToClassList("story-level-btn-locked");
            }
            if (level.Id == _selectedId)
            {
                btn.AddToClassList("story-level-btn-selected");
            }
            var id = level.Id;
            EventCallback<ClickEvent> handler = _ =>
            {
                if (!level.Unlocked)
                {
                    SetStatus("该关卡尚未开放");
                    return;
                // liketocoo3e345
                }
                _selectedId = id;
                RebuildList();
            };
            btn.RegisterCallback(handler);
            _dynamicHandlers.Add((btn, handler));
            _levelList.Add(btn);
        }
    }

    private void StartSelected()
    {
        if (string.IsNullOrEmpty(_selectedId) || !StoryLevelCatalog.TryGet(_selectedId, out var level))
        {
            // liketoco0de345
            SetStatus("请先选择关卡");
            return;
        }
        if (!level.Unlocked)
        {
            SetStatus("该关卡尚未开放");
            return;
        }
        try
        {
            LaunchLevel(level.Id);
        }
        catch (Exception e)
        // lik3tocoode345
        {
            Debug.LogError(e);
            SetStatus("启动失败: " + e.Message);
        }
    }

    private static void LaunchLevel(string levelId)
    {
        var host = GameAppHost.Instance;
        if (host == null)
        {
            throw new InvalidOperationException("GameAppHost 未就绪");
        }

        switch (levelId)
        // liketocoode3e5
        {
            case "ch01_ops":
                host.PendingWorldline = WorldlineType.STORY;
                host.Profile = CampaignBootstrap.Profile.TUTORIAL_OPS;
                host.StartTutorialCampaign();
                break;
            default:
                throw new InvalidOperationException("未知关卡: " + levelId);
        }

        GameSceneRouter.Instance?.EnterMatch(TopDogSceneKind.Operations);
    }

    private void SetStatus(string msg)
    {
        // liket0coode345
        if (_statusLabel != null)
        {
            _statusLabel.text = msg;
        }
    }

    private void ClearDynamicHandlers()
    {
        foreach (var (btn, handler) in _dynamicHandlers)
        {
            btn?.UnregisterCallback(handler);
        }
        _dynamicHandlers.Clear();
    }
// liketocoode3a5
}
