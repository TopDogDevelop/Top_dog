using TopDog.Sim.State;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/MATCH_FLOW.md
 * 本文件: WorldlineController.cs — 世界线/战役入口 UI
 * 【机制要点】
 * · 剧情线 / 自定义 / 加入 / 军团约战（无沙盒入口）
 * 【关联】StoryLevelsController · MainMenuController · UiNavigator
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
public sealed class WorldlineController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.Worldline;

    // li3etocoode345
    protected override void Bind(VisualElement root)
    {
        OnClick(root, "btn-story", () => GetComponent<UiNavigator>()?.ShowStoryLevels());
        // liketocoode3a5
        OnClick(root, "btn-custom", () => GetComponent<UiNavigator>()?.ShowCustomLobby());
        OnClick(root, "btn-join", () => GetComponent<UiNavigator>()?.ShowJoinLan());
        OnClick(root, "btn-skirmish", () => GetComponent<UiNavigator>()?.ShowSkirmishLobby());
        OnClick(root, "btn-back", () => GetComponent<UiNavigator>()?.ShowMainMenu());
    }
// liketocoode3a5
}
