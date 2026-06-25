using TopDog.App;
using TopDog.Sim.State;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDog.Client;

public sealed class WorldlineController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.Worldline;

    protected override void Bind(VisualElement root)
    {
        OnClick(root, "btn-story", () => GetComponent<UiNavigator>()?.ShowStoryLevels());
        OnClick(root, "btn-custom", () => GetComponent<UiNavigator>()?.ShowCustomLobby());
        OnClick(root, "btn-join", () => GetComponent<UiNavigator>()?.ShowJoinLan());
        OnClick(root, "btn-sandbox", () =>
        {
            try
            {
                var host = GameAppHost.Instance;
                if (host != null)
                {
                    host.PendingWorldline = WorldlineType.SANDBOX;
                    host.Profile = CampaignBootstrap.Profile.SHIPS_AND_MAP;
                    host.StartSandboxCampaign();
                }
                GameSceneRouter.Instance?.EnterMatch(TopDogSceneKind.Operations);
            }
            catch (System.Exception e)
            {
                Debug.LogError("沙盒启动失败: " + e.Message);
            }
        });
        OnClick(root, "btn-back", () => GetComponent<UiNavigator>()?.ShowMainMenu());
    }
}
