using UnityEngine.UIElements;

namespace TopDog.Client;

public sealed class SettingsController : UiScreenController
{
    public override UiScreenId ArtScreenId => UiScreenId.Settings;

    protected override void Bind(VisualElement root)
    {
        OnClick(root, "btn-back", () => GetComponent<UiNavigator>()?.ShowMainMenu());
    }
}
