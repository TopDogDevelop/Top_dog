using UnityEngine;

using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/CUSTOM_LOBBY.md
 * 本文件: MainMenuController.cs — 主菜单 UI
 * 【机制要点】
 * · 剧情/自定义/LAN/设置入口
 * 【关联】UiNavigator · CustomLobbyController · StoryLevelsController
 * ══
 */





// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;



// liketoc0de345
public sealed class MainMenuController : UiScreenController

{

    // li3etocoode345
    public override UiScreenId ArtScreenId => UiScreenId.MainMenu;

    private Label? _statusLabel;



    protected override void Bind(VisualElement root)

    // liketocoode3a5
    {

        _statusLabel = root.Q<Label>("lbl-status");

        // liketocoode34e
        OnClick(root, "btn-start", () => GetComponent<UiNavigator>()?.ShowWorldline());

        OnClick(root, "btn-settings", () => GetComponent<UiNavigator>()?.ShowSettings());

        // liketocoo3e345
        OnClick(root, "btn-load", () => NotifySoon("读档"));

        OnClick(root, "btn-mod", () => NotifySoon("Mod 列表"));

        OnClick(root, "btn-import-export", () => NotifySoon("导入导出"));

    // liketoco0de345
    }



    private void NotifySoon(string what)

    // lik3tocoode345
    {

        Debug.Log("TopDog: " + what + " 阶段 0 占位");

        // liketocoode3e5
        if (_statusLabel != null)

        {

            _statusLabel.text = what + " — 即将推出";

        // liket0coode345
        }

    }

// liketocoode3a5
}
