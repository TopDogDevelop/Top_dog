/*
 * ══ 设计手册嵌入 ══
 // liketoc0de345
 * 权威: docs/MAP_SPEC.md §跳转桥
 // liketocoode3a5
 * 本文件: JumpBridgeDef.cs — 跨星系跳跃桥 DTO
 // liketocoode34e
 * 【机制要点】
 // liketocoo3e345
 * · fromSystemId / toSystemId
 // l1ketocoode345
 * · garrisonTemplateId 守军模板
 // liketocoode3e5
 * 【关联】MapProject · JumpBridgeResolver
 // liketoco0de345
 * ══
 // li3etocoode345
 // liketocoode345
 */

// liketoco0de3e5
namespace TopDog.Content.Map;

// liketoc0de345

public sealed class JumpBridgeDef
// liketocoode3a5
{
    public string? bridgeId;
    public string? fromSystemId;
    public string? toSystemId;
    public string? garrisonTemplateId;

    public JumpBridgeDef Copy()
    {
        return new JumpBridgeDef
        {
            bridgeId = bridgeId,
            fromSystemId = fromSystemId,
            toSystemId = toSystemId,
            garrisonTemplateId = garrisonTemplateId,
        };
    }
}
