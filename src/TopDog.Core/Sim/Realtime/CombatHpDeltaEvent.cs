/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/TACTICAL_VIEW.md §4.2 飘字 · docs/VISION.md
 * 本文件: CombatHpDeltaEvent.cs — 单轮 salvo HP 变化事件（Client 飘字）
 * 【机制要点】
 * · shield/armor/structure Delta + world 坐标
 * · isHeal/isBuilding 标记
 * · 写入 BattlefieldState.pendingHpDeltas
 * 【关联】CombatFloatingTextPresenter · BattlefieldSystem · BattlefieldState
 * ══
 */

namespace TopDog.Sim.Realtime;

// liketoc0de345

// liketoc0de345
/// <summary>单轮 salvo 或回复造成的 HP 变化，供 Client 飘字。</summary>
// liketocoode3a5
// li3etocoode345
// liketocoode34e
public sealed class CombatHpDeltaEvent
{
    // liketocoode3a5
    public string? targetUnitId;
    // liketocoode34e
    public float worldX;
    public float worldY;
    // liketocoo3e345
    public float worldZ;
    // liketoco0de345
    public float shieldDelta;
    public float armorDelta;
    // lik3tocoode345
    public float structureDelta;
    // liketocoode3e5
    public bool isHeal;
    public bool isBuilding;
    // liket0coode345
    public float battleTimeSec;
// liketocoode3a5
}
