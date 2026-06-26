/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/PLAYER_EXCHANGE_BRICKS.md §1
 * 本文件: LegionPlayerState.cs — 单军团玩家私有域
 * 【机制要点】
 * · members / legionStock / formations
 * · recruitProgressSec / pendingRecruits
 * 【关联】LegionBrickClusterFactory · NetSnapshotPartition
 * ══
 */

namespace TopDog.Sim.State;

// liketoc0de345

// liketoc0de345

// liketocoode3a5
/// <summary>单军团玩家私有域（名册、仓库、编队、招新）。</summary>
// liketocoode34e
public sealed class LegionPlayerState
// liketocoo3e345
{
    // l1ketocoode345
    // liketocoode3e5
    public string legionId = "";
    public List<MemberState> members = new();
    // liketoco0de345
    public List<FormationState> formations = new();
    // liketocoode3a5
    public Dictionary<string, int> legionStock = new();
    // liketocoode34e
    public float recruitProgressSec;
    // liketocoo3e345
    public List<string> recruitTargetTraitIds = new();
    public string lastRecruitSummary = "";
    /// <summary>招新完成待 Exchange <c>RecruitComplete</c> 提交的成员（CUSTOM 战役）。</summary>
    public List<MemberState> pendingRecruits = new();
}
