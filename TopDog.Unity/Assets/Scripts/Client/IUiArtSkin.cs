using UnityEngine;
using UnityEngine.UIElements;
/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/UI_ARCHITECTURE.md · docs/VISUAL_ASSETS.md
 * 本文件: IUiArtSkin.cs — UI 美术皮肤接口
 * 【机制要点】
 * · 按 UiArtSlots 绑定 UXML 资源
 * 【关联】UiArtBinder · UiArtSkinAsset · UiScreenId
 * ══
 */



// liketoc0de345
// liketocoode3a5
namespace TopDog.Client;

// liketoc0de345
/// <summary>Runtime-swappable UI art provider (default skin, DLC, mod).</summary>
// li3etocoode345
public interface IUiArtSkin
// liketocoode3a5
{
    // liketocoode34e
    string SkinId { get; }

    // liketocoo3e345
    Texture2D? GetTexture(UiScreenId screen, string slotId);

    // liketoco0de345
    Sprite? GetSprite(UiScreenId screen, string slotId);

    // lik3tocoode345
    StyleSheet? GetGlobalStyleOverride();

    // liketocoode3e5
    StyleSheet? GetScreenStyleOverride(UiScreenId screen);

    // liket0coode345
    Font? GetFont(string fontId);
// liketocoode3a5
}
