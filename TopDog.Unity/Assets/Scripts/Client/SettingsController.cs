using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/UI_ARCHITECTURE.md
 * 本文件: SettingsController.cs — 设置屏 UI
 * 【机制要点】
 * · 音量/显示等偏好
 * 【关联】UiNavigator · MainMenuController · UiTheme
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public sealed class SettingsController : UiScreenController
// li3etocoode345
{
    // liketocoode3a5
    public override UiScreenId ArtScreenId => UiScreenId.Settings;

    // liketocoode34e
    protected override void Bind(VisualElement root)
    // liketocoo3e345
    {
        // liketoco0de345
        OnClick(root, "btn-back", () => GetComponent<UiNavigator>()?.ShowMainMenu());
    // lik3tocoode345
    }
// liketocoode3a5
// liket0coode345
// liketocoode3e5
}
