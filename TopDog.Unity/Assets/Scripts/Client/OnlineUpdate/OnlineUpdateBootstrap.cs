using System.Collections;
using TopDog.Net.Lan;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/RELEASE_AND_HOTUPDATE.md · docs/ONLINE_UPDATE.md
 * 本文件: OnlineUpdateBootstrap.cs — Boot 检查更新 + 确认弹窗
 * 【机制要点】
 * · 版号不合须确认；失败不阻断进主菜单
 * · 弹窗挂 UI Toolkit；缺 PanelSettings 时不可见（见 RELEASE §2.3）
 * · 下载时进度条按字节总量显示
 * 【关联】OnlineUpdateClient · ContentRootBootstrap · GameAppBootstrap
 * ══
 */

namespace TopDog.Client.OnlineUpdate;

/// <summary>Runs content update check before content root is finalized.</summary>
public static class OnlineUpdateBootstrap
{
    public static IEnumerator Run(VisualElement? hostRoot = null)
    {
        OnlineUpdateClient.SyncGateFromDisk();
        var statusLabel = EnsureHud(hostRoot, out var yesBtn, out var noBtn, out var panel, out var bar, out var sizeLabel);
        SetHud(statusLabel, panel, yesBtn, noBtn, bar, sizeLabel, "检查更新…", showButtons: false, progress: null);

        var checkTask = OnlineUpdateClient.CheckAsync();
        while (!checkTask.IsCompleted)
        {
            yield return null;
        }

        var check = checkTask.Result;
        if (!check.Ok)
        {
            SetHud(statusLabel, panel, yesBtn, noBtn, bar, sizeLabel, check.Message, showButtons: false, progress: null);
            yield return new WaitForSecondsRealtime(1.2f);
            HideHud(panel);
            yield break;
        }

        if (!check.NeedsUpdate || check.Remote == null)
        {
            SetHud(statusLabel, panel, yesBtn, noBtn, bar, sizeLabel, check.Message, showButtons: false, progress: null);
            yield return new WaitForSecondsRealtime(0.6f);
            HideHud(panel);
            yield break;
        }

        var decided = false;
        var accepted = false;
        SetHud(statusLabel, panel, yesBtn, noBtn, bar, sizeLabel, check.Message, showButtons: true, progress: null);
        if (yesBtn != null)
        {
            yesBtn.clicked += () => { accepted = true; decided = true; };
        }

        if (noBtn != null)
        {
            noBtn.clicked += () => { accepted = false; decided = true; };
        }

        var timeout = 60f;
        while (!decided && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!decided || !accepted)
        {
            SetHud(
                statusLabel,
                panel,
                yesBtn,
                noBtn,
                bar,
                sizeLabel,
                "已跳过更新 · 本机 " + ContentVersionGate.Current,
                showButtons: false,
                progress: null);
            yield return new WaitForSecondsRealtime(0.8f);
            HideHud(panel);
            yield break;
        }

        SetHud(statusLabel, panel, yesBtn, noBtn, bar, sizeLabel, "正在更新…", showButtons: false, progress: null);
        string? lastStatus = null;
        OnlineUpdateClient.ProgressInfo? lastProgress = null;
        var applyTask = OnlineUpdateClient.ApplyRemoteAsync(
            check.Remote,
            s => lastStatus = s,
            p => lastProgress = p);
        while (!applyTask.IsCompleted)
        {
            if (lastProgress != null)
            {
                SetHud(
                    statusLabel,
                    panel,
                    yesBtn,
                    noBtn,
                    bar,
                    sizeLabel,
                    lastProgress.Status,
                    showButtons: false,
                    progress: lastProgress);
            }
            else if (!string.IsNullOrEmpty(lastStatus))
            {
                SetHud(statusLabel, panel, yesBtn, noBtn, bar, sizeLabel, lastStatus, showButtons: false, progress: null);
            }

            yield return null;
        }

        var apply = applyTask.Result;
        SetHud(statusLabel, panel, yesBtn, noBtn, bar, sizeLabel, apply.Message, showButtons: false, progress: null);
        yield return new WaitForSecondsRealtime(1.0f);
        HideHud(panel);
    }

    private static Label? EnsureHud(
        VisualElement? hostRoot,
        out Button? yesBtn,
        out Button? noBtn,
        out VisualElement? panel,
        out ProgressBar? bar,
        out Label? sizeLabel)
    {
        yesBtn = null;
        noBtn = null;
        panel = null;
        bar = null;
        sizeLabel = null;
        if (hostRoot == null)
        {
            return null;
        }

        panel = hostRoot.Q("online-update-panel");
        if (panel == null)
        {
            panel = new VisualElement { name = "online-update-panel" };
            panel.style.position = Position.Absolute;
            panel.style.left = 0;
            panel.style.right = 0;
            panel.style.top = 0;
            panel.style.bottom = 0;
            panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
            panel.style.justifyContent = Justify.Center;
            panel.style.alignItems = Align.Center;
            panel.style.display = DisplayStyle.Flex;

            var box = new VisualElement();
            box.style.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.96f);
            box.style.paddingLeft = 24;
            box.style.paddingRight = 24;
            box.style.paddingTop = 20;
            box.style.paddingBottom = 20;
            box.style.minWidth = 320;
            box.style.maxWidth = 520;

            var label = new Label { name = "online-update-status", text = "…" };
            label.style.color = Color.white;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 12;
            box.Add(label);

            bar = new ProgressBar { name = "online-update-bar", title = "" };
            bar.lowValue = 0;
            bar.highValue = 1;
            bar.value = 0;
            bar.style.height = 18;
            bar.style.marginBottom = 6;
            bar.style.display = DisplayStyle.None;
            box.Add(bar);

            sizeLabel = new Label { name = "online-update-size", text = "" };
            sizeLabel.style.color = new Color(0.75f, 0.8f, 0.85f);
            sizeLabel.style.fontSize = 12;
            sizeLabel.style.marginBottom = 12;
            sizeLabel.style.display = DisplayStyle.None;
            box.Add(sizeLabel);

            var row = new VisualElement { name = "online-update-buttons" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.FlexEnd;
            yesBtn = new Button { name = "online-update-yes", text = "更新" };
            noBtn = new Button { name = "online-update-no", text = "跳过" };
            yesBtn.style.marginRight = 8;
            row.Add(yesBtn);
            row.Add(noBtn);
            box.Add(row);
            panel.Add(box);
            hostRoot.Add(panel);
        }
        else
        {
            yesBtn = panel.Q<Button>("online-update-yes");
            noBtn = panel.Q<Button>("online-update-no");
            bar = panel.Q<ProgressBar>("online-update-bar");
            sizeLabel = panel.Q<Label>("online-update-size");
        }

        return panel.Q<Label>("online-update-status");
    }

    private static void SetHud(
        Label? status,
        VisualElement? panel,
        Button? yesBtn,
        Button? noBtn,
        ProgressBar? bar,
        Label? sizeLabel,
        string text,
        bool showButtons,
        OnlineUpdateClient.ProgressInfo? progress)
    {
        if (panel != null)
        {
            panel.style.display = DisplayStyle.Flex;
        }

        if (status != null)
        {
            status.text = text;
        }

        var display = showButtons ? DisplayStyle.Flex : DisplayStyle.None;
        if (yesBtn != null)
        {
            yesBtn.style.display = display;
        }

        if (noBtn != null)
        {
            noBtn.style.display = display;
        }

        var showBar = progress != null && progress.BytesTotal > 0;
        if (bar != null)
        {
            bar.style.display = showBar ? DisplayStyle.Flex : DisplayStyle.None;
            if (showBar)
            {
                bar.value = progress!.Fraction;
            }
        }

        if (sizeLabel != null)
        {
            sizeLabel.style.display = showBar ? DisplayStyle.Flex : DisplayStyle.None;
            if (showBar)
            {
                sizeLabel.text = OnlineUpdateClient.FormatBytes(progress!.BytesDone)
                    + " / "
                    + OnlineUpdateClient.FormatBytes(progress.BytesTotal);
            }
        }
    }

    private static void HideHud(VisualElement? panel)
    {
        if (panel != null)
        {
            panel.style.display = DisplayStyle.None;
        }
    }
}
