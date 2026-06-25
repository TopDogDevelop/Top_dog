using UnityEngine;

using UnityEngine.UIElements;



namespace TopDog.Client;



public sealed class MainMenuController : UiScreenController

{

    public override UiScreenId ArtScreenId => UiScreenId.MainMenu;

    private Label? _statusLabel;



    protected override void Bind(VisualElement root)

    {

        _statusLabel = root.Q<Label>("lbl-status");

        OnClick(root, "btn-start", () => GetComponent<UiNavigator>()?.ShowWorldline());

        OnClick(root, "btn-settings", () => GetComponent<UiNavigator>()?.ShowSettings());

        OnClick(root, "btn-load", () => NotifySoon("读档"));

        OnClick(root, "btn-mod", () => NotifySoon("Mod 列表"));

        OnClick(root, "btn-import-export", () => NotifySoon("导入导出"));

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

