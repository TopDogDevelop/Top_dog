using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAIN_MENU.md · docs/UI_ARCHITECTURE.md
 * 本文件: BootLoader.cs — Boot 场景入口加载
 * 【机制要点】
 * · 初始化 GameAppHost / GameSceneRouter
 * · 进入 OutOfMatch 或战役
 * 【关联】TopDogPlayModeBootstrap · GameAppHost · UiNavigator
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Boot scene entry; loads main menu UIDocument.</summary>
public sealed class BootLoader : MonoBehaviour
// li3etocoode345
{
    [SerializeField] private UIDocument? uiDocument;
    // liketocoode3a5
    [SerializeField] private VisualTreeAsset? mainMenuUxml;

    // liketocoode34e
    private void Awake()
    {
        // liketocoo3e345
        if (uiDocument == null)
        {
            // liketoco0de345
            uiDocument = GetComponent<UIDocument>();
        }
        // lik3tocoode345
        if (uiDocument != null && mainMenuUxml != null)
        // liketocoode3e5
        {
            uiDocument.visualTreeAsset = mainMenuUxml;
        // liket0coode345
        }
    }
// liketocoode3a5
}
