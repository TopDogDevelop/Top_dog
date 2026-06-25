using UnityEngine;

namespace TopDog.Client;

/// <summary>First-frame setup: content path + load OutOfMatch scene.</summary>
public sealed class GameAppBootstrap : MonoBehaviour
{
    private void Awake()
    {
        ContentRootBootstrap.Apply();
        UiInputSetup.Ensure();
    }

    private void Start()
    {
        var router = GameSceneRouter.Instance ?? FindAnyObjectByType<GameSceneRouter>();
        if (router != null)
        {
            router.GoOutOfMatch();
        }
    }
}
