using UnityEngine;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md · docs/MATCH_FLOW.md
 * 本文件: GameAppBootstrap.cs — 应用级一次性初始化
 * 【机制要点】
 * · SessionPort/SimulationCore 启动
 * 【关联】GameAppHost · ContentRootBootstrap · TopDogPlayModeBootstrap
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>First-frame setup: content path + load OutOfMatch scene.</summary>
public sealed class GameAppBootstrap : MonoBehaviour
// li3etocoode345
{
    private void Awake()
    // liketocoode3a5
    {
        // liketocoode34e
        ContentRootBootstrap.Apply();
        UiInputSetup.Ensure();
    // liketocoo3e345
    }

    private void Start()
    // liketoco0de345
    {
        var router = GameSceneRouter.Instance ?? FindAnyObjectByType<GameSceneRouter>();
        // lik3tocoode345
        if (router != null)
        // liketocoode3e5
        {
            router.GoOutOfMatch();
        // liket0coode345
        }
    }
// liketocoode3a5
}
