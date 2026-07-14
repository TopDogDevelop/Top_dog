using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/VERSION.md · docs/CUSTOM_LOBBY.md
 * 本文件: MainMenuController.cs — 主菜单 UI
 * 【机制要点】
 * · 剧情/自定义/LAN/设置入口
 * · 副标题 lbl-version 不在 UXML 写死；运行时「软件版本」+ Application.version（YYYYMM.D.N，见 VERSION.md）
 * · Windows（Player/Editor）在菜单列表最底动态加「退出游戏」；移动端不加
 * 【关联】UiNavigator · CustomLobbyController · ContentVersionGate（内容号同形）
 * ══
 */

namespace TopDog.Client;

public sealed class MainMenuController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.MainMenu;

    private Label? _statusLabel;

    protected override void Bind(VisualElement root)
    {
        _statusLabel = root.Q<Label>("lbl-status");
        var versionLabel = root.Q<Label>("lbl-version");
        if (versionLabel != null)
        {
            versionLabel.text = FormatSoftwareVersionSubtitle();
        }

        OnClick(root, "btn-start", () => GetComponent<UiNavigator>()?.ShowWorldline());
        OnClick(root, "btn-settings", () => GetComponent<UiNavigator>()?.ShowSettings());
        OnClick(root, "btn-load", () => NotifySoon("读档"));
        OnClick(root, "btn-mod", () => NotifySoon("Mod 列表"));
        OnClick(root, "btn-import-export", () => NotifySoon("导入导出"));
        EnsureWindowsQuitButton(root);
    }

    /// <summary>
    /// 副标题：软件版本（PlayerSettings.bundleVersion → Application.version）。
    /// 例：软件版本 202607.14.1（格式见 docs/VERSION.md）；非单独拼接热更号。
    /// </summary>
    private static string FormatSoftwareVersionSubtitle()
    {
        var ver = Application.version;
        if (string.IsNullOrWhiteSpace(ver))
        {
            return "软件版本";
        }

        return "软件版本 " + ver.Trim();
    }

    private static bool IsWindowsDesktopRuntime() =>
        Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor;

    private void EnsureWindowsQuitButton(VisualElement root)
    {
        if (!IsWindowsDesktopRuntime())
        {
            return;
        }

        if (root.Q<Button>("btn-quit") != null)
        {
            return;
        }

        var stack = root.Q(className: "menu-stack")
                     ?? FindMenuStack(root);
        if (stack == null)
        {
            Debug.LogWarning("TopDog: menu-stack missing — skip Windows quit button");
            return;
        }

        var quit = new Button(QuitGame) { name = "btn-quit", text = "退出游戏" };
        quit.AddToClassList("menu-button");

        var status = _statusLabel ?? stack.Q<Label>("lbl-status");
        if (status != null && status.parent == stack)
        {
            stack.Insert(stack.IndexOf(status), quit);
        }
        else
        {
            var importExport = stack.Q<Button>("btn-import-export");
            if (importExport != null)
            {
                stack.Insert(stack.IndexOf(importExport) + 1, quit);
            }
            else
            {
                stack.Add(quit);
            }
        }
    }

    private static VisualElement? FindMenuStack(VisualElement root)
    {
        var importExport = root.Q<Button>("btn-import-export");
        return importExport?.parent;
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        // Avoid asmdef UnityEditor reference: stop Play Mode via reflection.
        var editorApp = System.Type.GetType("UnityEditor.EditorApplication,UnityEditor");
        editorApp?.GetProperty("isPlaying")?.SetValue(null, false);
#else
        Application.Quit();
#endif
    }

    private void NotifySoon(string what)
    {
        Debug.Log("TopDog: " + what + " 阶段 0 占位");
        if (_statusLabel != null)
        {
            _statusLabel.text = what + " — 即将推出";
        }
    }
}
