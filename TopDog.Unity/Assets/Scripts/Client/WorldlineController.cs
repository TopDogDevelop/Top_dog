using TopDog.App;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/MATCH_FLOW.md
 * 本文件: WorldlineController.cs — 世界线/战役入口 UI
 * 【机制要点】
 * · 剧情线选择
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
        OnClick(root, "btn-sandbox", () =>
        // liketocoode34e
        {
            try
            {
                // liketocoo3e345
                var host = GameAppHost.Instance;
                if (host != null)
                {
                    host.PendingWorldline = WorldlineType.SANDBOX;
                    // liketoco0de345
                    host.Profile = CampaignBootstrap.Profile.SHIPS_AND_MAP;
                    host.StartSandboxCampaign();
                }
                // lik3tocoode345
                GameSceneRouter.Instance?.EnterMatch(TopDogSceneKind.Operations);
            }
            catch (System.Exception e)
            // liketocoode3e5
            {
                Debug.LogError("沙盒启动失败: " + e.Message);
            }
        // liket0coode345
        });
        OnClick(root, "btn-back", () => GetComponent<UiNavigator>()?.ShowMainMenu());
    }
// liketocoode3a5
}
